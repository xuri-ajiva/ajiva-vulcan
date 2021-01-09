using System;
using System.IO;
using System.Linq;
using ajiva.Models;
using GlmSharp;
using SharpVk;
using SharpVk.Shanq;
using SharpVk.Shanq.GlmSharp;

namespace ajiva.EngineManagers
{
    public class Shader : IDisposable
    {
        private readonly DeviceManager manager;
        public bool Created { get; private set; }
        private object creatingLock = new();
        public ShaderModule? FragShader { get; private set; }
        public ShaderModule? VertShader { get; private set; }

        //public UniformBuffer<UniformBufferData> Uniform;

        public Shader(DeviceManager manager)
        {
            this.manager = manager;
        }

        //public void CreateUniformBuffer()
        //{
        //    Uniform = new(manager, 1);
        //}

        private static uint[] LoadShaderData(string filePath, out int codeSize)
        {
            var fileBytes = File.ReadAllBytes(filePath);
            var shaderData = new uint[(int)MathF.Ceiling(fileBytes.Length / 4f)];

            System.Buffer.BlockCopy(fileBytes, 0, shaderData, 0, fileBytes.Length);

            codeSize = fileBytes.Length;

            return shaderData;
        }

        private ShaderModule? CreateShader(string path)
        {
            var shaderData = LoadShaderData(path, out var codeSize);

            return manager.Device.CreateShaderModule(codeSize, shaderData);
        }

        public const string DefaultVertexShaderName = "vert.spv";
        public const string DefaultFragmentShaderName = "frag.spv";

        public void CreateShaderModules(string dir)
        {
            lock (creatingLock)
            {
                if (Created) return;
                VertShader = CreateShader($"{dir}/{DefaultVertexShaderName}");

                FragShader = CreateShader($"{dir}/{DefaultFragmentShaderName}");
                Created = true;
            }
        }

        public void CreateShaderModules(string vertexShaderName, string fragmentShaderName)
        {
            lock (creatingLock)
            {
                if (Created) return;
                VertShader = CreateShader(vertexShaderName);

                FragShader = CreateShader(fragmentShaderName);
                Created = true;
            }
        }

        public void CreateShaderModules<TV, TF>(Func<IShanqFactory, IQueryable<TV>> vertexShaderFunc, Func<IShanqFactory, IQueryable<TF>> fragmentShaderFunc)
        {
            lock (creatingLock)
            {
                if (Created) return;
                VertShader = manager.Device.CreateVertexModule(vertexShaderFunc);

                FragShader = manager.Device.CreateFragmentModule(fragmentShaderFunc);
                Created = true;
            }
        }

        public static Shader CreateShaderFrom(string dir, DeviceManager manager)
        {
            var sh = new Shader(manager);
            sh.CreateShaderModules(dir);
            //sh.CreateUniformBuffer();

            return sh;
        }

        public static Shader CreateShaderFrom<TV, TF>(Func<IShanqFactory, IQueryable<TV>> vertexShaderFunc, Func<IShanqFactory, IQueryable<TF>> fragmentShaderFunc, DeviceManager manager)
        {
            var sh = new Shader(manager);
            sh.CreateShaderModules(vertexShaderFunc, fragmentShaderFunc);
            //sh.CreateUniformBuffer();

            return sh;
        }

        public static Shader DefaultShader(DeviceManager manager)
        {
            return CreateShaderFrom(shank => from input in shank.GetInput<Vertex>()
                from ubo in shank.GetBinding<UniformBufferData>(0)
                let transform = ubo.Proj * ubo.View * ubo.Model
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
                }, manager);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            //Uniform?.Dispose();
            FragShader?.Dispose();
            VertShader?.Dispose();
        }
    }
}
