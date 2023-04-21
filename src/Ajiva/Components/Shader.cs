using System.Runtime.CompilerServices;
using Ajiva.Systems.Assets;
using Ajiva.Systems.Assets.Contracts;
using Ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;
using SharpVk.Shanq;
using SharpVk.Shanq.Numerics;
using Buffer = System.Buffer;

namespace Ajiva.Components;

public class Shader : ThreadSaveCreatable
{
    private readonly IDeviceSystem system;

    public Shader(IDeviceSystem system, string name)
    {
        this.system = system;
        Name = name;
    }

    public ShaderModule? FragShader { get; private set; }
    public ShaderModule? VertShader { get; private set; }

    public string Name { get; }

    public PipelineShaderStageCreateInfo VertShaderPipelineStageCreateInfo =>
        new PipelineShaderStageCreateInfo
            { Stage = ShaderStageFlags.Vertex, Module = VertShader, Name = Name };

    public PipelineShaderStageCreateInfo FragShaderPipelineStageCreateInfo =>
        new PipelineShaderStageCreateInfo
            { Stage = ShaderStageFlags.Fragment, Module = FragShader, Name = Name };

    private static uint[] LoadShaderData(IAssetManager assetManager, string assetName, out int codeSize)
    {
        var fileBytes = assetManager.GetAsset(AssetType.Shader, assetName);
        var shaderData = new uint[(int)MathF.Ceiling(fileBytes.Length / 4f)];

        Buffer.BlockCopy(fileBytes, 0, shaderData, 0, fileBytes.Length);

        codeSize = fileBytes.Length;

        return shaderData;
    }

    private ShaderModule? CreateShader(IAssetManager assetManager, string assetName)
    {
        var shaderData = LoadShaderData(assetManager, assetName, out var codeSize);

        var shader = system.Device!.CreateShaderModule(codeSize, shaderData);
        system.WatchObject(shader);
        return shader;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void CreateShaderModules(IAssetManager assetManager, string assetDir)
    {
        if (Created) return;
        VertShader = CreateShader(assetManager, AssetHelper.Combine(assetDir, Const.Default.VertexShaderName));

        FragShader = CreateShader(assetManager, AssetHelper.Combine(assetDir, Const.Default.FragmentShaderName));
        Created = true;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void CreateShaderModules(IAssetManager assetManager, string vertexShaderName, string fragmentShaderName)
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

    public static Shader CreateShaderFrom(IAssetManager assetManager, string dir, IDeviceSystem system, string name)
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
}
