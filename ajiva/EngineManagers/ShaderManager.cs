using System.Linq;
using ajiva.Engine;
using ajiva.Models;
using GlmSharp;

namespace ajiva.EngineManagers
{
    public class ShaderManager : IEngineManager
    {
        private readonly IEngine engine;

        public Shader Main { get; set; }

        //public UniformBuffer<UniformViewProj> ViewProj; 
        //public UniformBuffer<UniformModel> UniformModels; 
        public UniformBuffer<UniformBufferData> Uniform; 

        public ShaderManager(IEngine engine)
        {
            this.engine = engine;
            Main = new(engine.DeviceManager);
            Uniform = new(engine.DeviceManager);
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
        public void Dispose()
        {
            Main.Dispose();
        }

        public void CreateUniformBuffer()
        {
            //ViewProj = new(engine.DeviceManager,1);
           // UniformModels = new(engine.DeviceManager,1000);
            
             Uniform.Create();
            
            //Main.CreateUniformBuffer();
        }
    }
}
