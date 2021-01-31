//#define TEST_MODE
using System;
using System.Threading.Tasks;
using ajiva.Application;
using SharpVk.Glfw;
using SharpVk.Interop;

namespace ajiva
{
    public class Program
    {
        private static void Main()
        {
            Glfw3.Init();

            var app01 = new AjivaApplication();
            app01.Init();
            app01.Run();
            app01.Dispose();
            app01 = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            HeapUtil.AllocHFree();
            Glfw3.Terminate();
            Console.WriteLine("Finished, press any Key to continue.");
            Console.ReadKey();
            Environment.Exit(0);
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
