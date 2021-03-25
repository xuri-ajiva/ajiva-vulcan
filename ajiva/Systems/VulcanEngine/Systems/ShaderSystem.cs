using System.Collections.Generic;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Models.Buffer;
using ajiva.Systems.VulcanEngine.Unions;
using ajiva.Utils;

namespace ajiva.Systems.VulcanEngine.Systems
{
    [Dependent(typeof(DeviceSystem))]
    public class ShaderSystem : SystemBase, IInit
    {
        public Dictionary<AjivaEngineLayer, ShaderUnion> ShaderUnions { get; } = new();

        /// <inheritdoc />
        protected override void Setup()
        {
            Ecs.RegisterInit(this);
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs)
        {
            var ds = ecs.GetSystem<DeviceSystem>();

            if (!ShaderUnions.ContainsKey(AjivaEngineLayer.Layer2d))
            {
                ShaderUnions.Add(AjivaEngineLayer.Layer2d, ShaderUnion.InitCreate("./Shaders/2d", ds, 10000));
            }
            if (!ShaderUnions.ContainsKey(AjivaEngineLayer.Layer3d))
            {
                ShaderUnions.Add(AjivaEngineLayer.Layer3d, ShaderUnion.InitCreate("./Shaders/3d", ds, 25000));
            }
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            foreach (var (_, value) in ShaderUnions)
            {
                value.Dispose();
            }
        }
    }
    public class ShaderUnion : DisposingLogger
    {
        public Shader Main { get; init; }

        public UniformBuffer<UniformViewProj> ViewProj { get; init; }
        public UniformBuffer<UniformModel> UniformModels { get; init; }

        public ShaderUnion(Shader main, UniformBuffer<UniformViewProj> viewProj, UniformBuffer<UniformModel> uniformModels)
        {
            Main = main;
            ViewProj = viewProj;
            UniformModels = uniformModels;
        }

        public static ShaderUnion InitCreate(string path, DeviceSystem ds, int count)
        {
            var su = new ShaderUnion(new(ds), new(ds, 1), new(ds, count));
            su.Main.CreateShaderModules(path);
            su.UniformModels.EnsureExists();
            su.ViewProj.EnsureExists();
            return su;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Main?.Dispose();
            ViewProj?.Dispose();
            UniformModels?.Dispose();
        }
    }
}
