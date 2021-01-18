using ajiva.Engine;
using ajiva.Models;

namespace ajiva.EngineManagers
{
    public class ShaderComponent : RenderEngineComponent
    {
        public Shader? Main { get; set; }

        public readonly UniformBuffer<UniformViewProj> ViewProj;
        public readonly UniformBuffer<UniformModel> UniformModels;

        public ShaderComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
            ViewProj = new(renderEngine.DeviceComponent, 1);
            UniformModels = new(renderEngine.DeviceComponent, 200000);

            //Uniform = new(renderEngine.DeviceComponent);
        }

        public void EnsureShaderModulesExists()
        {
            if (Main != null) return;
            
            Main = new(RenderEngine.DeviceComponent);
            Main.CreateShaderModules("./Shaders");
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
            Main?.Dispose();
            UniformModels.Dispose();
            ViewProj.Dispose();
        }

        public void EnsureCreateUniformBufferExists()
        {
            //ProjView = new(renderEngine.DeviceComponent,1);
            // UniformModels = new(renderEngine.DeviceComponent,1000);

            ViewProj.EnsureExists();
            UniformModels.EnsureExists();

            //Main.EnsureCreateUniformBufferExists();
        }
    }
}
