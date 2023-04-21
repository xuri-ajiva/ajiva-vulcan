// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using Ajiva;
using Ajiva.Application;
using Ajiva.Extensions;
using Ajiva.Systems.Assets;
using Ajiva.Systems.Assets.Contracts;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Microsoft.Extensions.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Autofac.DependencyInjection;

Console.WriteLine("Starting Ajiva Engine at " + DateTime.Now);
Thread.CurrentThread.Name = "Main";

var builder = new ContainerBuilder();
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("Appsettings.json", false, true)
    .AddJsonFile($"Appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true, true)
    .Build();
var config = configuration.GetSection("Ajiva").Get<AjivaConfig>();

//builder.RegisterModule(new ConfigurationModule(configuration));
builder.RegisterInstance(configuration);
builder.RegisterInstance(config);

var loggerConfiguration = new LoggerConfiguration()
    .Enrich.With<CallerEnricher>()
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
    AssetPacker.PackDefault(config, Const.Default.AssetsPath);
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

class CallerEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var skip = 3;
        while (true)
        {
            var stack = new StackFrame(skip, true);
            if (!stack.HasMethod())
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty("Caller", new ScalarValue("<unknown method>")));
                return;
            }

            var method = stack.GetMethod();
            if (method.DeclaringType.Assembly != typeof(Log).Assembly)
            {
                var methodName = $"{method.DeclaringType.Name}.{method.Name}";
                var fileName = Path.GetFileName(stack.GetFileName());
                var caller = $"{methodName} ({fileName}:{stack.GetFileLineNumber()})";
                logEvent.AddPropertyIfAbsent(new LogEventProperty("Caller", new ScalarValue(caller)));
                return;
            }

            skip++;
        }
    }
}
