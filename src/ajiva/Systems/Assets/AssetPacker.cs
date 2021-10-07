using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Markup;
using ajiva.Systems.Assets.Contracts;
using ajiva.Systems.VulcanEngine;
using Ajiva.Wrapper.Logger;
using ProtoBuf;
using ProtoBuf.Meta;

namespace ajiva.Systems.Assets
{
    internal class AssetPacker
    {
        public static async Task Pack(string assetOutput, AssetSpecification assetSpecification, bool overide = false)
        {
            AssetPack assetPack = new AssetPack();

            var shaderRoot = assetSpecification.Get(AssetType.Shader);

            Task.WaitAll(shaderRoot.EnumerateDirectories("", SearchOption.AllDirectories).Select(directory => CheckAndAddShadersInDir(assetPack, shaderRoot, directory)).ToArray());

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

                var assetHash = new SHA1CryptoServiceProvider().ComputeHash(serializedAsset);
                if (File.Exists(assetOutput))
                {
                    if (overide)
                    {
                        if (File.Exists(hashFilePath))
                        {
                            var diskHash = File.ReadAllBytes(hashFilePath);
                            if (assetHash.SequenceEqual(diskHash))
                            {
                                LogHelper.Log($"Asset Already Build, Content Identical: {assetHash.Select(x => x.ToString("X")).Aggregate((x, y) => x + y)}");
                                return;
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("File Already Exists");
                    }
                }
                File.WriteAllBytes(assetOutput,serializedAsset);

                File.WriteAllBytes(hashFilePath, assetHash);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static Task AddTexturesInDir(AssetPack assetPack, FileSystemInfo root, DirectoryInfo textureDirectory)
        {
            var relPathName = textureDirectory == root ? "" : textureDirectory.FullName[(root.FullName.Length + 1)..];
            foreach (var fileInfo in textureDirectory.EnumerateFiles())
            {
                if (fileInfo.Extension is ".png" or ".jpg" or ".bmp" or ".texture")
                {
                    assetPack.Add(AssetType.Texture, relPathName, fileInfo);
                }
            }
            return Task.CompletedTask;
        }

        private static async Task CheckAndAddShadersInDir(AssetPack assetPack, FileSystemInfo root, DirectoryInfo shaderDirectory)
        {
            // region compile
            var files = shaderDirectory.GetFiles();

            var vert = files.FirstOrDefault(x => x.Extension == ".vert");
            var frag = files.FirstOrDefault(x => x.Extension == ".frag");

            if (vert is null && frag is null)
                return;
            if (vert is null)
            {
                LogHelper.WriteLine($"[PACK/ERROR] Vertex Shader is Null! for: {shaderDirectory.Name}");
                return;
            }
            if (frag is null)
            {
                LogHelper.WriteLine($"[PACK/ERROR] Fragment Shader is Null! for: {shaderDirectory.Name}");
                return;
            }

            var compiler = new Process
            {
                StartInfo = new ProcessStartInfo(ShaderCompiler, $"{frag.Name} {vert.Name} -V")
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = shaderDirectory.FullName,
                    CreateNoWindow = true
                },
            };
            compiler.Start();
            compiler.PriorityClass = ProcessPriorityClass.High;
            compiler.EnableRaisingEvents = true;
            compiler.PriorityBoostEnabled = true;
            while (!compiler.HasExited)
            {
                compiler.Refresh();
                await Task.Delay(100);
            }
            var errors = await compiler.StandardError.ReadToEndAsync();
            var output = await compiler.StandardOutput.ReadToEndAsync();
            LogHelper.WriteLine($"[COMPILE/INFO]: Shaders for: {shaderDirectory.Name}");
            if (!string.IsNullOrEmpty(output))
                LogHelper.WriteLine($"[COMPILE/RESULT/INFO] {output}");
            if (!string.IsNullOrEmpty(errors))
                LogHelper.WriteLine($"[COMPILE/RESULT/ERROR] {errors}");
            LogHelper.WriteLine($"[COMPILE/RESULT/EXIT] Compiler Process has exited with code {compiler.ExitCode}");
            if (compiler.ExitCode != 0)
            {
                Environment.Exit((int)(compiler.ExitCode + Const.ExitCode.ShaderCompile));
            }

            //region pack
            files = shaderDirectory.GetFiles(); //refresh files
            var relPathName = shaderDirectory == root ? "" : shaderDirectory.FullName[(root.FullName.Length + 1)..];

            assetPack.Add(AssetType.Shader, relPathName, files.First(x => x.Name == Const.Default.VertexShaderName));
            assetPack.Add(AssetType.Shader, relPathName, files.First(x => x.Name == Const.Default.FragmentShaderName));
        }

        private static readonly string ShaderCompiler = Path.GetFullPath("./tools/spirv/glslangValidator.exe");
    }
}
