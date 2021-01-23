using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.Component;
using ajiva.Ecs.Entity;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Engine;
using SharpVk;
using SharpVk.Multivendor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ajiva.Entities;

// ReSharper disable once CheckNamespace
namespace ajiva.Systems.VulcanEngine
{
    public partial class AjivaRenderEngine : ComponentSystemBase<ARenderAble>, IRenderEngine
    {
        /// <inheritdoc />
        public Cameras.Camera MainCamara
        {
            get => mainCamara;
            set
            {
                mainCamara?.Dispose();
                mainCamara = value;
            }
        }

        /// <inheritdoc />
        public AjivaEcs Ecs { get; set; }

        #region Public

        #region Vars

        private static readonly DebugReportCallbackDelegate DebugReportDelegate = DebugReport;

        public long InitialTimestamp;

        #endregion

        private static Bool32 DebugReport(DebugReportFlags flags, DebugReportObjectType objectType, ulong @object, HostSize location, int messageCode, string layerPrefix, string message, IntPtr userData)
        {
            Console.WriteLine($"[{flags}] ({objectType}) {layerPrefix}:\n{message}");

            return false;
        }

        #endregion

        public static (Instance instance, DebugReportCallback debugReportCallback) CreateInstance(IEnumerable<string> enabledExtensionNames)
        {
            //if (Instance != null) return;

            List<string> enabledLayers = new();

            var props = Instance.EnumerateLayerProperties();

            void AddAvailableLayer(string layerName)
            {
                if (props.Any(x => x.LayerName == layerName))
                    enabledLayers.Add(layerName);
            }

            AddAvailableLayer("VK_LAYER_LUNARG_standard_validation");
            AddAvailableLayer("VK_LAYER_KHRONOS_validation");
            AddAvailableLayer("VK_LAYER_GOOGLE_unique_objects");
            //AddAvailableLayer("VK_LAYER_LUNARG_api_dump");
            AddAvailableLayer("VK_LAYER_LUNARG_core_validation");
            AddAvailableLayer("VK_LAYER_LUNARG_image");
            AddAvailableLayer("VK_LAYER_LUNARG_object_tracker");
            AddAvailableLayer("VK_LAYER_LUNARG_parameter_validation");
            AddAvailableLayer("VK_LAYER_LUNARG_swapchain");
            AddAvailableLayer("VK_LAYER_GOOGLE_threading");

            var instance = Instance.Create(
                enabledLayers.ToArray(),
                enabledExtensionNames.Append(ExtExtensions.DebugReport).ToArray(),
                applicationInfo: new ApplicationInfo
                {
                    ApplicationName = "ajiva",
                    ApplicationVersion = new(0, 0, 1),
                    EngineName = "ajiva-engine",
                    EngineVersion = new(0, 0, 1),
                    ApiVersion = new(1, 0, 0)
                });

            var debugReportCallback = instance.CreateDebugReportCallback(DebugReportDelegate, DebugReportFlags.Error | DebugReportFlags.Warning | DebugReportFlags.PerformanceWarning);

            return (instance, debugReportCallback);
        }

        private Cameras.Camera? mainCamara;

        private void UpdateCamaraProjView()
        {
            ShaderComponent.ViewProj.UpdateExpresion(delegate(int index, ref UniformViewProj value)
            {
                if (index != 0) return;

                value.View = MainCamara.View;
                value.Proj = MainCamara.Projection;
                value.Proj[1, 1] *= -1;
            });
            ShaderComponent.ViewProj.Copy();
        }

        private void DrawFrame(TimeSpan delta)
        {
            OnFrame?.Invoke(delta);
            GraphicsComponent.Current?.DrawFrame();
        }

        /// <inheritdoc />
        public override void Update(TimeSpan delta)
        {
            foreach (var (renderAble, entity) in ComponentEntityMap)
            {
                ShaderComponent.UniformModels.Staging.Value[renderAble!.Id] = new()
                {
                    Model =  entity.GetComponent<Transform3d>()?.ModelMat ?? mat4.Identity, TextureSamplerId = renderAble!.Id
                };
            }

            ShaderComponent.UniformModels.Staging.CopyValueToBuffer();
            lock (RenderLock)
                ShaderComponent.UniformModels.Copy();

            Window.PollEvents();
            //MainCamara.Update((float)delta.TotalMilliseconds);

            lock (RenderLock)
                UpdateCamaraProjView();
            lock (RenderLock)
                DrawFrame(delta);
            //Console.WriteLine(mainCamara.GetComponent<Transform3d>());

            if (!Window.WindowReady)
                Ecs.IssueClose();
        }

        /// <inheritdoc />
        public override async Task Init(AjivaEcs ecs)
        {
            await InitWindow(ecs.GetPara<int>("SurfaceWidth"), ecs.GetPara<int>("SurfaceHeight"));

            InitVulkan();
            Ecs = ecs;
        }

        /// <inheritdoc />
        public override ARenderAble CreateComponent(IEntity entity)
        {
            var rnd = new ARenderAble();
            ComponentEntityMap.Add(rnd, entity);
            return rnd;
        }

        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            entity.AddComponent(CreateComponent(entity));
        }
    }
}
