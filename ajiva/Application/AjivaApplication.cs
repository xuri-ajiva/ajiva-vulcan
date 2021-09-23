using System;
using System.Threading;
using System.Threading.Tasks;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Example;
using ajiva.Entities;
using ajiva.Factories;
using ajiva.Generators.Texture;
using ajiva.Systems;
using ajiva.Systems.VulcanEngine;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layer2d;
using ajiva.Systems.VulcanEngine.Layer3d;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using ajiva.Worker;
using Ajiva.Wrapper.Logger;
using GlmSharp;
using SharpVk;
using SharpVk.Glfw;
using SharpVk.Multivendor;

namespace ajiva.Application
{
    public class AjivaApplication : DisposingLogger
    {
        private bool Running { get; set; }

        private readonly AjivaEcs entityComponentSystem = new(false);

        public void Run()
        {
            Running = true;

            ConsoleBlock block = new(1);

            RunHelper.RunDelta(delegate(UpdateInfo info)
            {
                if (info.Iteration % 100 == 0)
                    block.WriteAt($"iteration: {info.Iteration}, delta: {info.Delta}, FPS: {1000.0f / info.Delta.TotalMilliseconds:F4}, PendingWorkItemCount: {ThreadPool.PendingWorkItemCount}, Entities.Count: {entityComponentSystem.Entities.Count}", 0);

                entityComponentSystem.Update(info);
                return entityComponentSystem.Available;
            }, TimeSpan.MaxValue);

            Running = false;
        }

        private Instance vulcanInstance;
        private DebugReportCallback debugReportCallback;

        private const int SurfaceWidth = 800;
        private const int SurfaceHeight = 600;

        public void Init()
        {
            (vulcanInstance, debugReportCallback) = Statics.CreateInstance(Glfw3.GetRequiredInstanceExtensions());
            var deviceSystem = entityComponentSystem.CreateSystemOrComponentSystem<DeviceSystem>();

            var meshPool = new MeshPool(deviceSystem);
            entityComponentSystem.AddInstance(meshPool);

            entityComponentSystem.AddInstance(vulcanInstance);

            entityComponentSystem.AddSystem(new WorkerPool(Environment.ProcessorCount / 2, "AjivaWorkerPool", entityComponentSystem) { Enabled = true });
            var window = entityComponentSystem.CreateSystemOrComponentSystem<WindowSystem>();
            entityComponentSystem.CreateSystemOrComponentSystem<BoxTextureGenerator>();

            //var layerSystem = entityComponentSystem.CreateSystemOrComponentSystem<LayerSystem>();

            entityComponentSystem.CreateSystemOrComponentSystem<TextureSystem>();
            entityComponentSystem.CreateSystemOrComponentSystem<ImageSystem>();
            entityComponentSystem.CreateSystemOrComponentSystem<TransformComponentSystem>();
            entityComponentSystem.CreateSystemOrComponentSystem<Transform2dComponentSystem>();

            var graphicsSystem = entityComponentSystem.CreateSystemOrComponentSystem<GraphicsSystem>();
            entityComponentSystem.AddEntityFactory(new SomeEntityFactory());

            entityComponentSystem.AddEntityFactory(new CubeFactory());
            entityComponentSystem.AddEntityFactory(new RectFactory());
            entityComponentSystem.AddEntityFactory(new Cameras.FpsCamaraFactory());

            entityComponentSystem.AddParam(nameof(SurfaceHeight), SurfaceHeight);
            entityComponentSystem.AddParam(nameof(SurfaceWidth), SurfaceWidth);

            window.OnKeyEvent += WindowOnOnKeyEvent;
            var ajiva3dLayerSystem = entityComponentSystem.CreateSystemOrComponentSystem<Ajiva3dLayerSystem>();
            var ajiva2dLayerSystem = entityComponentSystem.CreateSystemOrComponentSystem<Ajiva2dLayerSystem>();
            var solidMeshRenderLayer = entityComponentSystem.CreateSystemOrComponentSystem<SolidMeshRenderLayer>();
            var rectRender = entityComponentSystem.CreateSystemOrComponentSystem<Mesh2dRenderLayer>();

            graphicsSystem.AddUpdateLayer(ajiva3dLayerSystem);
            graphicsSystem.AddUpdateLayer(ajiva2dLayerSystem);

            ajiva3dLayerSystem.AddLayer(solidMeshRenderLayer);
            ajiva2dLayerSystem.AddLayer(rectRender);

            var meshPref = MeshPrefab.Cube;
            var r = new Random();

            entityComponentSystem.InitSystems();

            meshPool.AddMesh(MeshPrefab.Cube);
            meshPool.AddMesh(MeshPrefab.Rect);

            var rect = entityComponentSystem.CreateEntity<Rect>();

            var rectMesh = rect.GetComponent<RenderMesh2D>();
            rectMesh.SetMesh(MeshPrefab.Rect);
            rectMesh.Render = true;
            var rectTrans = rect.GetComponent<Transform2d>();
            rectTrans.Scale = new vec2(.1f);
            

            for (var i = 0; i < 10; i++)
            {
                var cube = entityComponentSystem.CreateEntity<Cube>();

                var render = cube.GetComponent<RenderMesh3D>();
                render.SetMesh(meshPref);
                render.Render = true;

                var trans = cube.GetComponent<Transform3d>();
                trans.Position = new(r.Next(-posRange, posRange), r.Next(-posRange, posRange), r.Next(-posRange, posRange));
                trans.Rotation = new(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
            }
        }

        const int size = 100;
        const int posRange = 100;
        const float scale = 0.7f;
        Random r = new();

        private void WindowOnOnKeyEvent(object? sender, Key key, int scancode, InputAction inputaction, Modifier modifiers)
        {
            if (inputaction != InputAction.Press) return;
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            var meshPref = MeshPrefab.Cube;

            if (key is > Key.Num0 and <= Key.Num9)
            {
                Task.Run(() =>
                {
                    var index = key - Key.Num0 + 1;
                    var rep = 1 << index;
                    const float sz = .5f;
                    for (var i = 0; i < rep; i++)
                    {
                        for (int j = 0; j < rep; j++)
                        {
                            var cube = entityComponentSystem.CreateEntity<Cube>();

                            var render = cube.GetComponent<RenderMesh3D>();
                            render.SetMesh(meshPref);
                            render.Render = true;

                            var trans = cube.GetComponent<Transform3d>();
                            trans.Position = new(i * sz, -index * 2, j * sz);
                            trans.Rotation = new(i * 90, j * 90, 0);
                            trans.Scale = new(sz / 2);
                        }
                    }
                });
            }

            switch (key)
            {
                case Key.B:
                    for (var i = 0; i < 100; i++)
                    {
                        var cube = entityComponentSystem.CreateEntity<Cube>();

                        var render = cube.GetComponent<RenderMesh3D>();
                        render.SetMesh(meshPref);
                        render.Render = true;

                        var trans = cube.GetComponent<Transform3d>();
                        trans.Position = new(r.Next(-posRange, posRange), r.Next(-posRange, posRange), r.Next(-posRange, posRange));
                        trans.Rotation = new(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
                    }
                    break;

                case Key.R:
                    var rect = entityComponentSystem.CreateEntity<Rect>();

                    var renderRect = rect.GetComponent<RenderMesh2D>();
                    renderRect.SetMesh(MeshPrefab.Rect);
                    renderRect.Render = true;
                    break;

                case Key.T:
                    /*foreach (var keyValuePair in entityComponentSystem.Entities)
                    {keyValuePair.Value
                    }*/
                    break;
            }
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            entityComponentSystem.Dispose();

            debugReportCallback.Dispose();
            vulcanInstance.Dispose();
            debugReportCallback = null!;
            vulcanInstance = null!;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
