// See https://aka.ms/new-console-template for more information
using ajiva;
using ajiva.application;
using ajiva.Extensions;
using ajiva.Systems.Assets;
using ajiva.Systems.Assets.Contracts;
using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Autofac.Core;
using Microsoft.Extensions.Configuration;
using Serilog.Extensions.Autofac.DependencyInjection;

Console.WriteLine("Starting Ajiva Engine at " + DateTime.Now);
Thread.CurrentThread.Name = "Main";

var builder = new ContainerBuilder();
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true, true)
    .Build();

builder.RegisterModule(new ConfigurationModule(configuration));
var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration);
builder.RegisterSerilog(loggerConfiguration);
builder.RegisterInstance(Log.Logger);

var logger = Log.ForContext<Program>();
logger.Information("ProcessId: {ProcessId}", Environment.ProcessId);
logger.Information("Version: {Version}", Environment.Version);
logger.Information("Is64BitProcess: {Is64BitProcess}", Environment.Is64BitProcess);
logger.Information("Is64BitOperatingSystem: {Is64BitOperatingSystem}", Environment.Is64BitOperatingSystem);
logger.Information("OSVersion: {OSVersion}", Environment.OSVersion);

if (args.Length > 0)
{
    PackAssets();
}

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
