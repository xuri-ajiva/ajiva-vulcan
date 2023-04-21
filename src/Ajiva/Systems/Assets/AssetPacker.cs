using System.Diagnostics;
using System.Security.Cryptography;
using Ajiva.Application;
using Ajiva.Systems.Assets.Contracts;
using ProtoBuf;

namespace Ajiva.Systems.Assets;

public class AssetPacker
{
    public static async Task Pack(string assetOutput, AssetSpecification assetSpecification, ShaderConfig config, bool overide = false)
    {
        var assetPack = new AssetPack();

        var shaderRoot = assetSpecification.Get(AssetType.Shader);

        Task.WaitAll(shaderRoot.EnumerateDirectories("", SearchOption.AllDirectories).Select(directory => CheckAndAddShadersInDir(assetPack, shaderRoot, directory, config)).ToArray());

        var textureRoot = assetSpecification.Get(AssetType.Texture);

        Task.WaitAll(textureRoot.EnumerateDirectories("", SearchOption.AllDirectories).Select(directory => AddTexturesInDir(assetPack, textureRoot, directory)).ToArray());

        await AddTexturesInDir(assetPack, textureRoot, textureRoot);

        WriteAsset(assetOutput, overide, assetPack);
    }

    private static void WriteAsset(string assetOutput, bool overide, AssetPack assetPack)
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
                if (overide)
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

        var compiler = new Process
        {
            StartInfo = new ProcessStartInfo(FindCompiler(), $"{frag.Name} {vert.Name} -t -C -V " + BuildMacros(config))
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = shaderDirectory.FullName,
                CreateNoWindow = true
            }
        };
        compiler.Start();
        compiler.PriorityClass = ProcessPriorityClass.High;
        compiler.EnableRaisingEvents = true;
        compiler.PriorityBoostEnabled = true;

        const int maxWait = 10000;
        const int waitInterval = 100;
        for (var i = 0; !compiler.HasExited && i < maxWait / waitInterval; i += waitInterval)
        {
            compiler.Refresh();
            await Task.Delay(waitInterval);
        }

        var errors = await compiler.StandardError.ReadToEndAsync();
        var output = await compiler.StandardOutput.ReadToEndAsync();

        lock (_lock)
        {
            Log.Information("[COMPILE/INFO]: Shaders for: {Name}", shaderDirectory.Name);
            if (!string.IsNullOrEmpty(output))
                Log.Information("[COMPILE/RESULT/INFO]\n{output}", output.TrimEnd('\n'));
            if (!string.IsNullOrEmpty(errors))
                Log.Error("[COMPILE/RESULT/ERROR]\n{errors}", errors.TrimEnd('\n'));
            Log.Information("[COMPILE/RESULT/EXIT] Compiler Process has exited with code {ExitCode}", compiler.ExitCode);
            if (compiler.ExitCode != 0) Environment.Exit((int)(compiler.ExitCode + Const.ExitCode.ShaderCompile));
        }

        //region pack
        files = shaderDirectory.GetFiles(); //refresh files
        var relPathName = shaderDirectory == root ? "" : shaderDirectory.FullName[(root.FullName.Length + 1)..];

        assetPack.Add(AssetType.Shader, relPathName, files.First(x => x.Name == Const.Default.VertexShaderName));
        assetPack.Add(AssetType.Shader, relPathName, files.First(x => x.Name == Const.Default.FragmentShaderName));
    }

    private static string BuildMacros(ShaderConfig config)
    {

        const string macroPrefix = " -D";
        return config.GetAll().Select(x => macroPrefix + x.name + "=" + x.value).Aggregate((x, y) => x + y);
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

    private static object _lock = new();
}
