using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ajiva.Ecs.Component;
using ajiva.Models;
using ajiva.Models.Buffer;
using ajiva.Systems.VulcanEngine;
using ajiva.Systems.VulcanEngine.Systems;
using GlmSharp;
using SharpVk;
using SharpVk.Shanq;
using SharpVk.Shanq.GlmSharp;

namespace ajiva.Components
{
    public class Shader : ThreadSaveCreatable, IComponent
    {
        private readonly DeviceSystem system;
        public ShaderModule? FragShader { get; private set; }
        public ShaderModule? VertShader { get; private set; }

        public Shader(DeviceSystem system, string name)
        {
            this.system = system;
            Name = name;
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
            var shaderData = LoadShaderData(path, out var codeSize);

            return system.Device!.CreateShaderModule(codeSize, shaderData);
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
            VertShader = system.Device.CreateVertexModule(vertexShaderFunc);

            FragShader = system.Device.CreateFragmentModule(fragmentShaderFunc);
            Created = true;
        }

        public static Shader CreateShaderFrom(string dir, DeviceSystem system, string name)
        {
            var sh = new Shader(system, name);
            sh.CreateShaderModules(dir);
            return sh;
        }

        public static Shader CreateShaderFrom<TV, TF>(Func<IShanqFactory, IQueryable<TV>> vertexShaderFunc, Func<IShanqFactory, IQueryable<TF>> fragmentShaderFunc, DeviceSystem system, string name)
        {
            var sh = new Shader(system, name);
            sh.CreateShaderModules(vertexShaderFunc, fragmentShaderFunc);
            return sh;
        }

        public static Shader DefaultShader(DeviceSystem system, string name)
        {
            return CreateShaderFrom(shank => from input in shank.GetInput<Vertex3D>()
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
                }, system, name);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            FragShader?.Dispose();
            VertShader?.Dispose();
        }

        /// <inheritdoc />
        protected override void Create()
        {
            throw new NotSupportedException("Create with Specific Arguments");
        }

        public string Name { get; }

        /// <inheritdoc />
        public bool Dirty { get; set; }

        public PipelineShaderStageCreateInfo VertShaderPipelineStageCreateInfo => new() {Stage = ShaderStageFlags.Vertex, Module = VertShader, Name = Name,};

        public PipelineShaderStageCreateInfo FragShaderPipelineStageCreateInfo => new() {Stage = ShaderStageFlags.Fragment, Module = FragShader, Name = Name,};
    }
}
