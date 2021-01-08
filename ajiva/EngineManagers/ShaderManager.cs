using System;
using System.IO;
using ajiva.Engine;
using SharpVk;

namespace ajiva.EngineManagers
{
    public class ShaderManager : IEngineManager
    {
        private readonly IEngine engine;

        public ShaderModule? FragShader { get; private set; }
        public ShaderModule? VertShader { get; private set; }

        public ShaderManager(IEngine engine)
        {
            this.engine = engine;
        }

        private static uint[] LoadShaderData(string filePath, out int codeSize)
        {
            var fileBytes = File.ReadAllBytes(filePath);
            var shaderData = new uint[(int)MathF.Ceiling(fileBytes.Length / 4f)];

            System.Buffer.BlockCopy(fileBytes, 0, shaderData, 0, fileBytes.Length);

            codeSize = fileBytes.Length;

            return shaderData;
        }

        ShaderModule? CreateShader(string path)
        {
            var shaderData = LoadShaderData(path, out var codeSize);

            return engine.DeviceManager.Device.CreateShaderModule(codeSize, shaderData);
        }

        public void CreateShaderModules()
        {
            VertShader = CreateShader(@".\Shaders\vert.spv");

            FragShader = CreateShader(@".\Shaders\frag.spv");
            /*        
              vertShader = device.CreateVertexModule(shank => from input in shank.GetInput<Vertex>()
                  from ubo in shank.GetBinding<UniformBufferObject>(0)
                  let transform = ubo.Proj * ubo.View * ubo.Model
                  select new VertexOutput
                  {
                      Position = transform * new vec4(input.Position, 1),
                      Colour = input.Colour
                  });
  
              fragShader = device.CreateFragmentModule(shank => from input in shank.GetInput<FragmentInput>()
                  from sampler in shank.GetSampler2d<vec4,vec2>(1,1) 
                  let colour = sampler.Sample(input.Position.xy)//new vec4(input.Color,1)
                  select new FragmentOutput
                  {
                      Colour = colour
                  });*/
        }

        /// <inheritdoc />
        public void Dispose()
        {
            FragShader?.Dispose();
            VertShader?.Dispose();
        }
    }
}
