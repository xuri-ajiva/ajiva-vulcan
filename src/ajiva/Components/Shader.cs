﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ajiva.Ecs;
using ajiva.Ecs.Component;
using ajiva.Models;
using ajiva.Models.Buffer;
using ajiva.Systems.Assets;
using ajiva.Systems.Assets.Contracts;
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

        private static uint[] LoadShaderData(AssetManager assetManager, string assetName, out int codeSize)
        {
            var fileBytes = assetManager.GetAsset(AssetType.Shader, assetName);
            var shaderData = new uint[(int)MathF.Ceiling(fileBytes.Length / 4f)];

            System.Buffer.BlockCopy(fileBytes, 0, shaderData, 0, fileBytes.Length);

            codeSize = fileBytes.Length;

            return shaderData;
        }

        private ShaderModule? CreateShader(AssetManager assetManager, string assetName)
        {
            var shaderData = LoadShaderData(assetManager, assetName, out var codeSize);

            return system.Device!.CreateShaderModule(codeSize, shaderData);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CreateShaderModules(AssetManager assetManager, string assetDir)
        {
            if (Created) return;
            VertShader = CreateShader(assetManager, AssetHelper.Combine(assetDir, Const.Default.VertexShaderName));

            FragShader = CreateShader(assetManager, AssetHelper.Combine(assetDir, Const.Default.FragmentShaderName));
            Created = true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CreateShaderModules(AssetManager assetManager, string vertexShaderName, string fragmentShaderName)
        {
            if (Created) return;
            VertShader = CreateShader(assetManager, vertexShaderName);

            FragShader = CreateShader(assetManager, fragmentShaderName);
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

        public static Shader CreateShaderFrom(AssetManager assetManager, string dir, DeviceSystem system, string name)
        {
            var sh = new Shader(system, name);
            sh.CreateShaderModules(assetManager, dir);
            return sh;
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

        public PipelineShaderStageCreateInfo VertShaderPipelineStageCreateInfo => new() { Stage = ShaderStageFlags.Vertex, Module = VertShader, Name = Name, };

        public PipelineShaderStageCreateInfo FragShaderPipelineStageCreateInfo => new() { Stage = ShaderStageFlags.Fragment, Module = FragShader, Name = Name, };
    }
}
