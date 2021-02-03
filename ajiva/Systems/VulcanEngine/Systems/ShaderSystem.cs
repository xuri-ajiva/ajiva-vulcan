using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Models;

namespace ajiva.Systems.VulcanEngine.Systems
{
    public class ShaderSystem : SystemBase, IInit
    {
        public Shader? Main3D { get; set; }
        public Shader? Main2D { get; set; }

        public readonly UniformBuffer<UniformViewProj> ViewProj;
        public readonly UniformBuffer<UniformModel> UniformModels;

        public ShaderSystem(DeviceSystem deviceSystem)
        {
            ViewProj = new(deviceSystem, 1);
            UniformModels = new(deviceSystem, 2000);
        }

        /// <inheritdoc />
        protected override void Setup()
        {
            Ecs.RegisterInit(this, InitPhase.PreMain);
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs, InitPhase phase)
        {
            if (Main3D == null)
            {
                Main3D = new(ecs.GetSystem<DeviceSystem>());
                Main3D.CreateShaderModules("./Shaders/3d");
            }
            if (Main2D == null)
            {
                Main2D = new(ecs.GetSystem<DeviceSystem>());
                Main2D.CreateShaderModules("./Shaders/2d");
            }

            //ProjView = new(renderEngine.DeviceComponent,1);
            // UniformModels = new(renderEngine.DeviceComponent,1000);

            ViewProj.EnsureExists();
            UniformModels.EnsureExists();

            //Main.EnsureCreateUniformBufferExists();

            /* Main.EnsureShaderModulesExists(shank => from input in shank.GetInput<Vertex>()
                 from viewProj in shank.GetBinding<UniformViewProj>(0)
                 from model in shank.GetBinding<UniformModel>(1)
                 let transform = viewProj.Proj * viewProj.View * model.Model
                 select new VertexOutput
                 {
                     Position = transform * new vec4(input.Position, 1),
                     Colour = input.Colour
                 }, shank => from input in shank.GetInput<FragmentInput>()
                 from sampler in shank.GetSampler2d<vec4, vec2>(1, 1)
                 let colour = sampler.Sample(input.Position.xy) //new vec4(input.Color,1)
                 select new FragmentOutput
                 {
                     Colour = colour
                 });             */
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Main3D?.Dispose();
            Main2D?.Dispose();
            UniformModels.Dispose();
            ViewProj.Dispose();
        }
    }
}
