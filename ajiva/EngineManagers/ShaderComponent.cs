using ajiva.Engine;
using ajiva.Models;

namespace ajiva.EngineManagers
{
    public class ShaderComponent : RenderEngineComponent
    {

        public Shader Main { get; set; }

        public UniformBuffer<UniformViewProj> ViewProj;
        public UniformBuffer<UniformModel> UniformModels;

        public ShaderComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
            ViewProj = new(renderEngine.DeviceComponent);
            UniformModels = new(renderEngine.DeviceComponent);
            Main = new(renderEngine.DeviceComponent);
            //Uniform = new(renderEngine.DeviceComponent);
        }

        public void CreateShaderModules()
        {
            Main.CreateShaderModules("./Shaders");
            /* Main.CreateShaderModules(shank => from input in shank.GetInput<Vertex>()
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

        public void UpdateViewProj(UniformViewProj data)
        {
            //ViewProj.Update(new []{data});
            //ViewProj.Copy();
        }

        public void UpdateModel(UniformModel data, uint id)
        {
            // UniformModels.UpdateCopyOne(data, id);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Main.Dispose();
            UniformModels.Dispose();
            ViewProj.Dispose();
        }

        public void CreateUniformBuffer()
        {
            //ViewProj = new(renderEngine.DeviceComponent,1);
            // UniformModels = new(renderEngine.DeviceComponent,1000);

            ViewProj.Create();
            UniformModels.Create();

            //Main.CreateUniformBuffer();
        }
    }
}
