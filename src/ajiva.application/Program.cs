// See https://aka.ms/new-console-template for more information
using ajiva;
using ajiva.application;
using ajiva.Ecs.Component;
using ajiva.Ecs.ComponentSytem;
using ajiva.Extensions;
using ajiva.Systems.Assets;
using ajiva.Systems.Assets.Contracts;
using Ajiva.Wrapper.Logger;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using SharpVk.Glfw;

/*
if (args.Length > 0)
{
    PackAssets();
}
*/

ALog.MinimumLogLevel = ALogLevel.Debug;
ALog.Log(ALogLevel.Info, $"ProcessId: {Environment.ProcessId}");
ALog.Log(ALogLevel.Info, $"Version: {Environment.Version}");
ALog.Log(ALogLevel.Info, $"Is64BitProcess: {Environment.Is64BitProcess}");
ALog.Log(ALogLevel.Info, $"Is64BitOperatingSystem: {Environment.Is64BitOperatingSystem}");
ALog.Log(ALogLevel.Info, $"OSVersion: {Environment.OSVersion}");

ALog.MinimumLogLevel = ALogLevel.Debug;
Glfw3.Init();
var builder = new ContainerBuilder();

//todo generate this
builder.AddFactoryData();

builder.AddEngine();

var container = builder.Build();
var proxy = container.CreateBaseLayer();

var app = new Application(container, proxy);
app.InitData();

app.SetupUpdate();
var src = new CancellationTokenSource();
src.CancelAfter(TimeSpan.FromMinutes(10));
await app.Run(src.Token);

app.Dispose();
await container.DisposeAsync();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();


async void PackAssets()
{
    await AssetPacker.Pack(Const.Default.AssetsFile,
        new AssetSpecification(Const.Default.AssetsPath,
            new Dictionary<AssetType, string> {
                [AssetType.Shader] = "Shaders",
                [AssetType.Texture] = "Textures",
                [AssetType.Model] = "Models"
            }), true);
}

public class MySource : IRegistrationSource
{
    /// <inheritdoc />
    public IEnumerable<IComponentRegistration>
        RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
    {
        var swt = service as IServiceWithType;
        if (swt == null || !typeof(IComponentSystem).IsAssignableFrom(swt.ServiceType))
            yield break;

        var rb = RegistrationBuilder.ForDelegate(swt.ServiceType, (c, p) =>
        {
            var type = swt.ServiceType.GetGenericArguments()[0];
            var target = typeof(IComponentSystem<>).MakeGenericType(type);
            return c.Resolve(target);
        }).CreateRegistration();

        yield return rb;

        // if swt is IComponent then just create a new instance
        if (typeof(IComponent).IsAssignableFrom(swt.ServiceType))
        {
            var rb2 = RegistrationBuilder.ForDelegate(swt.ServiceType, (c, p) => { return Activator.CreateInstance(swt.ServiceType); }).CreateRegistration();

            yield return rb2;
        }
    }

    /// <inheritdoc />
    public bool IsAdapterForIndividualComponents { get; set; }
}
