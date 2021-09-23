﻿using System;
using System.Collections.Generic;
using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Models;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer2d;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using GlmSharp;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layer2d
{
    [Dependent(typeof(WindowSystem))]
    public class Ajiva2dLayerSystem : SystemBase, IInit, IUpdate, IAjivaLayer<UniformLayer2d>
    {
        private WindowSystem window;

        private object MainLock { get; } = new object();

        /// <inheritdoc />
        public Ajiva2dLayerSystem(AjivaEcs ecs) : base(ecs)
        {
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs)
        {
            window = Ecs.GetSystem<WindowSystem>();

            window.OnMouseMove += delegate(object? sender, AjivaMouseMotionCallbackEventArgs args)
            {
                if (args.ActiveLayer == AjivaEngineLayer.Layer2d)
                {
                    MouseMoved(args.Pos);
                }
            };
            var canvas = window.Canvas;
            window.OnResize += delegate
            {
                BuildLayerUniform(window.Canvas);
            };

            var deviceSystem = Ecs.GetSystem<DeviceSystem>();

            /*MainShader = Shader.CreateShaderFrom("./Shaders/2d", deviceSystem, "main");
            PipelineDescriptorInfos = ajiva.Systems.VulcanEngine.Unions.PipelineDescriptorInfos.CreateFrom(
                LayerUniform.Uniform.Buffer!, (uint)LayerUniform.SizeOfT,
                Models.Uniform.Buffer!, (uint)Models.SizeOfT,
                Ecs.GetComponentSystem<TextureSystem, ATexture>().TextureSamplerImageViews
            );*/

            LayerUniform = new AChangeAwareBackupBufferOfT<UniformLayer2d>(1, deviceSystem);
            const int fac = 50;
            /*LayerUniform.SetAndCommit(0, new UniformLayer2d
                { View = mat4.Translate(-1, -1, 0) * mat4.Scale(2) });*/
            LayerUniform[0] = new UniformLayer2d
                //{ MousePos = new vec2(.5f, .5f), View = mat4.Ortho(-1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f)});           
                {
                    Vec2 = new vec2(1337, 421337), MousePos = new vec2(.5f, .5f)
                };
            BuildLayerUniform(window.Canvas);
        }

        private void BuildLayerUniform(Canvas canvas) => BuildLayerUniform(canvas.WidthF, canvas.HeightF);
        private void BuildLayerUniform(float height, float width)
        {
            var byRef = LayerUniform.GetForChange(0);

            byRef.Value.View = mat4.Translate(-width / 2, -height / 2, 0) * mat4.Scale(width * 2, height * 2, 0);
            byRef.Value.Proj = mat4.Ortho(-width, width, -height, height, -1, 1);
            LayerUniform.Commit(0);
        }

        private void MouseMoved(vec2 pos)
        {
            var posNew = new vec2(pos.x / window.Canvas.WidthF, pos.y / window.Canvas.HeightF);

            lock (MainLock)
            {
                LayerUniform[0].Value.MousePos = posNew;
                LayerUniform.Commit(0);
                //LayerUniform.SetAndCommit(0, new UniformLayer2d
                //  { MousePos = posNew });
            }
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
        }

        /// <inheritdoc />
        List<IAjivaLayerRenderSystem> IAjivaLayer.LayerRenderComponentSystems => new List<IAjivaLayerRenderSystem>(LayerRenderComponentSystems);

        /// <inheritdoc />
        public IAChangeAwareBackupBufferOfT<UniformLayer2d> LayerUniform { get; private set; }

        /// <inheritdoc />
        public void AddLayer(IAjivaLayerRenderSystem<UniformLayer2d> layer)
        {
            layer.AjivaLayer = this;
            LayerRenderComponentSystems.Add(layer);
        }

        /// <inheritdoc />
        public List<IAjivaLayerRenderSystem<UniformLayer2d>> LayerRenderComponentSystems { get; } = new List<IAjivaLayerRenderSystem<UniformLayer2d>>();

        /// <inheritdoc />
        public AjivaVulkanPipeline PipelineLayer { get; } = AjivaVulkanPipeline.Pipeline2d;

        /// <inheritdoc />
        public ClearValue[] ClearValues { get; } = Array.Empty<ClearValue>();

        /// <inheritdoc />
        public RenderPassLayer CreateRenderPassLayer(SwapChainLayer swapChainLayer)
        {
            return RenderPassLayerCreator.NoDepth(swapChainLayer, Ecs.GetSystem<DeviceSystem>(), Ecs.GetComponentSystem<ImageSystem, AImage>());
        }
    }
}
