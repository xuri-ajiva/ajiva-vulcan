﻿//#define TEST_MODE
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ajiva.Application;
using Ajiva.Wrapper.Logger;
using SharpVk.Glfw;
using SharpVk.Interop;

namespace ajiva
{
    public static class Program
    {
        private static void Main()
        {
            LogHelper.UseConsoleCursorPos = false;
            Console.WriteLine($"ProcessId: {Environment.ProcessId}");
            Console.WriteLine($"Version: {Environment.Version}");
            Console.WriteLine($"Is64BitProcess: {Environment.Is64BitProcess}");
            Console.WriteLine($"Is64BitOperatingSystem: {Environment.Is64BitOperatingSystem}");
            Console.WriteLine($"OSVersion: {Environment.OSVersion}");
            
            Glfw3.Init();

            var app01 = new AjivaApplication();
            app01.Init();
            app01.Run();

            TaskSource.CancelAfter(3000);
            Task.WaitAll(Tasks.ToArray(), 4000);

            app01.Dispose();
            app01 = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            HeapUtil.FreeHGlobal();
            Glfw3.Terminate();
            Console.WriteLine("Finished, press any Key to continue.");
            Console.ReadKey();
            Environment.Exit(0);
        }

        private static readonly CancellationTokenSource TaskSource = new();
        private static readonly List<Task> Tasks = new();

        public static void TaskWatcher(Func<Task?> run)
        {
            Tasks.Add(Task.Run(run, TaskSource.Token));
        }

        // private void Menu()
        // {
        //     ConsoleMenu m = new();
        //     m.ShowMenu("Actions: ", new ConsoleMenuItem[] {new("Exit", null), new("Load Texture", LoadTexture)});
        // }

        // private void LoadTexture()
        // {
        //     Console.WriteLine("Path:");
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
}
