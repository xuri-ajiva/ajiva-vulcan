//#define TEST_MODE
using System.Diagnostics;
using ajiva.Application;
using ajiva.Systems.Assets;
using ajiva.Systems.Assets.Contracts;
using SharpVk.Glfw;
using SharpVk.Interop;

namespace ajiva;

public static class Program
{
    private static readonly string ShaderCompiler = Path.GetFullPath("./tools/spirv/glslangValidator.exe");

    private static readonly object ConsoleLock = new object();

    private static void Main(string[] args)
    {
        if (args.Length > 0)
        {
        }
        else
        {
            PackAssets();
        }
        //CompileShaders();

        ALog.Log(ALogLevel.Info, $"ProcessId: {Environment.ProcessId}");
        ALog.Log(ALogLevel.Info, $"Version: {Environment.Version}");
        ALog.Log(ALogLevel.Info, $"Is64BitProcess: {Environment.Is64BitProcess}");
        ALog.Log(ALogLevel.Info, $"Is64BitOperatingSystem: {Environment.Is64BitOperatingSystem}");
        ALog.Log(ALogLevel.Info, $"OSVersion: {Environment.OSVersion}");

        Glfw3.Init();

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(10));
        var app01 = new AjivaApplication(cancellationTokenSource);
        app01.Init();
        try
        {
            app01.Run();
        }
        catch (Exception e)
        {
            ALog.Error(e);
        }

        TaskWatcher.Cancel();

        app01.Dispose();
        app01 = null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        HeapUtil.FreeHGlobal();
        Glfw3.Terminate();
        ALog.Info("Finished, press any Key to continue.");
        Console.ReadKey();
        Environment.Exit(0);
    }

    private static async void PackAssets()
    {
        await AssetPacker.Pack(Const.Default.AssetsFile, new AssetSpecification(Const.Default.AssetsPath, new Dictionary<AssetType, string>
        {
            [AssetType.Shader] = "Shaders",
            [AssetType.Texture] = "Textures",
            [AssetType.Model] = "Models"
        }), true);
    }

    private static void CompileShaders()
    {
        /*
         * ./tools/glslangValidator.exe shader.frag shader.vert -V
         */

        Task.WaitAll(
            new DirectoryInfo(Path.Combine(Const.Default.AssetsPath, "Shaders"))
                .EnumerateDirectories("", SearchOption.AllDirectories)
                .Select(shaderDirectory =>
                    Task.Run(
                        async () => await CheckAndCompilerShadersInDir(shaderDirectory)
                    )
                ).ToArray()
        );
    }

    private static async Task CheckAndCompilerShadersInDir(DirectoryInfo shaderDirectory)
    {
        var files = shaderDirectory.GetFiles();
        var vert = files.FirstOrDefault(x => x.Extension == ".vert");
        var frag = files.FirstOrDefault(x => x.Extension == ".frag");

        if (vert is null && frag is null)
            return;
        if (vert is null)
        {
            ALog.Info($"[COMPILE/ERROR] Vertex Shader is Null! for: {shaderDirectory.Name}");
            return;
        }
        if (frag is null)
        {
            ALog.Info($"[COMPILE/ERROR] Fragment Shader is Null! for: {shaderDirectory.Name}");
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
            }
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

        lock (ConsoleLock)
        {
            ALog.Info($"[COMPILE/INFO]: Shaders for: {shaderDirectory.Name}");
            if (!string.IsNullOrEmpty(output))
                ALog.Info($"[COMPILE/RESULT/INFO] {output}");
            if (!string.IsNullOrEmpty(errors))
                ALog.Info($"[COMPILE/RESULT/ERROR] {errors}");
            ALog.Info($"[COMPILE/RESULT/EXIT] Compiler Process has exited with code {compiler.ExitCode}");
            if (compiler.ExitCode != 0) Environment.Exit((int)(compiler.ExitCode + Const.ExitCode.ShaderCompile));
        }
    }

    // private void Menu()
    // {
    //     ConsoleMenu m = new();
    //     m.ShowMenu("Actions: ", new ConsoleMenuItem[] {new("Exit", null), new("Load Texture", LoadTexture)});
    // }

    // private void LoadTexture()
    // {
    //     ALog.Info("Path:");
    //     var path = Console.ReadLine()!;
    //     engine.TextureComponent.AddAndMapTextureToDescriptor(Texture.FromFile(engine, path));
    //     Menu();
    // }

    // private void OnUpdate(TimeSpan delta)
    // {
    //     //UpdateApplication();
    //
    //     UpdateUniformBuffer(delta);
    // }

//         private void UpdateUniformBuffer(TimeSpan timeSpan)
//         {
//             //var currentTimestamp = Stopwatch.GetTimestamp();
//             //var totalTime = (currentTimestamp - engine.InitialTimestamp) / (float)Stopwatch.Frequency;
//
//             /*foreach (var aEntity in Ecs.Entities.Values.Where(e => e.RenderAble != null && e.RenderAble.Render))
//             {
//                 if (engine.ShaderComponent.UniformModels.Staging.Value.Length > aEntity.RenderAble!.Id)
//                 {
//                     engine.ShaderComponent.UniformModels.Staging.Value[aEntity.RenderAble!.Id] = new()
//                     {
//                         Model = aEntity.Transform.ModelMat, TextureSamplerId = aEntity.RenderAble!.Id
//                     };
//                 }
//             }
//
//             engine.ShaderComponent.UniformModels.Staging.CopyValueToBuffer();
//             lock (engine.RenderLock)
//                 engine.ShaderComponent.UniformModels.Copy();*/
//         }

    //private readonly Queue<Action> applicationQueue = new();

    /* private void UpdateApplication()
     {
         while (applicationQueue.TryDequeue(out var action))
             action.Invoke();
     }  */
}
