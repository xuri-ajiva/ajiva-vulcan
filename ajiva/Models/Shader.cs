using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ajiva.Helpers;
using ajiva.Systems.RenderEngine;
using ajiva.Systems.RenderEngine.EngineManagers;
using GlmSharp;
using SharpVk;
using SharpVk.Shanq;
using SharpVk.Shanq.GlmSharp;

namespace ajiva.Models
{
    public class Shader : DisposingLogger, IThreadSaveCreatable
    {
        private readonly DeviceComponent component;
        public ShaderModule? FragShader { get; private set; }
        public ShaderModule? VertShader { get; private set; }

        public Shader(DeviceComponent component)
        {
            this.component = component;
        }

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
            component.EnsureDevicesExist();
            
            var shaderData = LoadShaderData(path, out var codeSize);

            return component.Device!.CreateShaderModule(codeSize, shaderData);
        }

        public const string DefaultVertexShaderName = "vert.spv";
        public const string DefaultFragmentShaderName = "frag.spv";

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CreateShaderModules(string dir)
        {
            if (Created) return;
            VertShader = CreateShader($"{dir}/{DefaultVertexShaderName}");

            FragShader = CreateShader($"{dir}/{DefaultFragmentShaderName}");
            Created = true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CreateShaderModules(string vertexShaderName, string fragmentShaderName)
        {
            if (Created) return;
            VertShader = CreateShader(vertexShaderName);

            FragShader = CreateShader(fragmentShaderName);
            Created = true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CreateShaderModules<TV, TF>(Func<IShanqFactory, IQueryable<TV>> vertexShaderFunc, Func<IShanqFactory, IQueryable<TF>> fragmentShaderFunc)
        {
            if (Created) return;
            VertShader = component.Device.CreateVertexModule(vertexShaderFunc);

            FragShader = component.Device.CreateFragmentModule(fragmentShaderFunc);
            Created = true;
        }

        public static Shader CreateShaderFrom(string dir, DeviceComponent component)
        {
            var sh = new Shader(component);
            sh.CreateShaderModules(dir);
            //sh.EnsureCreateUniformBufferExists();

            return sh;
        }

        public static Shader CreateShaderFrom<TV, TF>(Func<IShanqFactory, IQueryable<TV>> vertexShaderFunc, Func<IShanqFactory, IQueryable<TF>> fragmentShaderFunc, DeviceComponent component)
        {
            var sh = new Shader(component);
            sh.CreateShaderModules(vertexShaderFunc, fragmentShaderFunc);
            //sh.EnsureCreateUniformBufferExists();

            return sh;
        }

        public static Shader DefaultShader(DeviceComponent component)
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
                }, component);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            FragShader?.Dispose();
            VertShader?.Dispose();
        }

        /// <inheritdoc />
        public bool Created { get; private set; }

        /// <inheritdoc />
        public void EnsureExists()
        {
            throw new NotSupportedException("Create with Specific Arguments");
        }
    }
}
