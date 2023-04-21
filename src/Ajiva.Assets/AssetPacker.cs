using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Ajiva.Application;
using Ajiva.Systems.Assets.Contracts;
using CliWrap;
using ProtoBuf;
using Serilog;

namespace Ajiva.Systems.Assets;

public class AssetPacker
{
    public static async Task Pack(string assetOutput, AssetSpecification assetSpecification, ShaderConfig config, bool @override = false)
    {
        var assetPack = new AssetPack();

        var shaderRoot = assetSpecification.Get(AssetType.Shader);

        Task.WaitAll(shaderRoot.EnumerateDirectories("", SearchOption.AllDirectories).Select(directory => CheckAndAddShadersInDir(assetPack, shaderRoot, directory, config)).ToArray());

        var textureRoot = assetSpecification.Get(AssetType.Texture);

        Task.WaitAll(textureRoot.EnumerateDirectories("", SearchOption.AllDirectories).Select(directory => AddTexturesInDir(assetPack, textureRoot, directory)).ToArray());

        await AddTexturesInDir(assetPack, textureRoot, textureRoot);

        WriteAsset(assetOutput, @override, assetPack);
    }

    private static void WriteAsset(string assetOutput, bool @override, AssetPack assetPack)
    {
        var hashFilePath = assetOutput + ".sha1.txt";
        try
        {
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, assetPack);
            var serializedAsset = ms.ToArray();

            var assetHash = SHA1.HashData(serializedAsset);
            if (File.Exists(assetOutput))
            {
                if (@override)
                {
                    if (File.Exists(hashFilePath))
                    {
                        var diskHash = File.ReadAllBytes(hashFilePath);
                        if (assetHash.SequenceEqual(diskHash))
                        {
                            Log.Warning("Asset Already Build, Content Identical: {Hash}", assetHash.Select(x => x.ToString("X")).Aggregate((x, y) => x + y));
                            return;
                        }
                    }
                }
                else
                {
                    throw new Exception("File Already Exists");
                }
            }
            File.WriteAllBytes(assetOutput, serializedAsset);

            File.WriteAllBytes(hashFilePath, assetHash);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error Writing Asset");
        }
    }

    private static Task AddTexturesInDir(AssetPack assetPack, FileSystemInfo root, DirectoryInfo textureDirectory)
    {
        var relPathName = textureDirectory == root ? "" : textureDirectory.FullName[(root.FullName.Length + 1)..];
        foreach (var fileInfo in textureDirectory.EnumerateFiles())
            if (fileInfo.Extension is ".png" or ".jpg" or ".bmp" or ".texture")
                assetPack.Add(AssetType.Texture, relPathName, fileInfo);
        return Task.CompletedTask;
    }

    private static async Task CheckAndAddShadersInDir(AssetPack assetPack, FileSystemInfo root, DirectoryInfo shaderDirectory, ShaderConfig config)
    {
        // region compile
        var files = shaderDirectory.GetFiles();

        var vert = files.FirstOrDefault(x => x.Extension == ".vert");
        var frag = files.FirstOrDefault(x => x.Extension == ".frag");

        if (vert is null && frag is null)
            return;
        if (vert is null)
        {
            Log.Error("[PACK/ERROR] Vertex Shader is Null! for: {Name}", shaderDirectory.Name);
            return;
        }
        if (frag is null)
        {
            Log.Error("[PACK/ERROR] Fragment Shader is Null! for: {Name}", shaderDirectory.Name);
            return;
        }
        var args = new List<string> {
            frag.Name,
            vert.Name,
            "-t",
            "-C",
            "-V"
        };
        BuildMacros(config, args);

        var output = new StringBuilder();
        var errors = new StringBuilder();
        var cli = Cli.Wrap(FindCompiler())
            .WithArguments(args)
            .WithWorkingDirectory(shaderDirectory.FullName)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(errors))
            .WithValidation(CommandResultValidation.None);

        var compiler = await cli.ExecuteAsync();

        if (output.Length != 0)
            Log.Verbose("[INFO for {Name}] {output}", shaderDirectory.Name, output.ToString().TrimEnd('\n'));
        if (errors.Length != 0)
            Log.Error("[ERROR for {Name}] {errors}", shaderDirectory.Name, errors.ToString().TrimEnd('\n'));
        Log.Information("Compiler Process for {shader} has exited with code {ExitCode}", shaderDirectory.Name, compiler.ExitCode);
        if (compiler.ExitCode != 0)
            Environment.Exit((int)(compiler.ExitCode + Const.ExitCode.ShaderCompile));

        //region pack
        files = shaderDirectory.GetFiles(); //refresh files
        var relPathName = shaderDirectory == root ? "" : shaderDirectory.FullName[(root.FullName.Length + 1)..];

        assetPack.Add(AssetType.Shader, relPathName, files.First(x => x.Name == Const.Default.VertexShaderName));
        assetPack.Add(AssetType.Shader, relPathName, files.First(x => x.Name == Const.Default.FragmentShaderName));
    }

    private static void BuildMacros(ShaderConfig config, List<string> args)
    {
        const string macroPrefix = "-D";
        args.AddRange(config.GetAll().Select(x => macroPrefix + x.name + "=" + x.value));
    }

    private static string FindCompiler()
    {
        const string search = "tools/spirv/glslangValidator.exe";

        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (dir.Parent is not null)
        {
            var path = Path.Combine(dir.FullName, search);
            if (File.Exists(path))
                return path;
            dir = dir.Parent;
        }

        throw new FileNotFoundException("Could not find glslangValidator.exe");
    }

    public static void PackDefault(AjivaConfig config, string assetsPath)
    {
        var task = Task.Run(async () => await Pack(config.AssetPath,
            new AssetSpecification(assetsPath,
                new Dictionary<AssetType, string> {
                    [AssetType.Shader] = "Shaders",
                    [AssetType.Texture] = "Textures",
                    [AssetType.Model] = "Models"
                }), config.ShaderConfig, true));
        //wait for task to finish
        task.Wait();
    }
}
