using System.Diagnostics;
using System.Text.Json;
using Ajiva.Application;
using Ajiva.Assets;
using Ajiva.Extensions;
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
    .AddJsonFile("Appsettings.json", true, true)
    .AddJsonFile($"Appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true, true)
    .Build();

var config = JsonSerializer.Deserialize<AjivaConfig>(File.ReadAllText(AjivaConfig.FileName), AjivaConfigJsonSerializerContext.Default.AjivaConfig);
/*new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile(AjivaConfig.FileName, false)
    .Build().Get<AjivaConfig>();*/

//builder.RegisterModule(new ConfigurationModule(configuration));
builder.RegisterInstance(configuration);
builder.RegisterInstance(config);

var loggerConfiguration = new LoggerConfiguration()
    .Enrich.With<CallerEnricher>()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithThreadName()
    .MinimumLevel.Debug()
    .Destructure.ToMaximumDepth(4)
    .Destructure.ToMaximumStringLength(100)
    .Destructure.ToMaximumCollectionCount(10)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} [{Level:u3}]#{ThreadId,2} {Caller}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, path: "Logs/Ajiva-engine.txt", outputTemplate: "{Timestamp:o}|{Level:u3}|({ThreadId}/{ThreadName})|{SourceContext}|{Message}|{Exception}{NewLine}");
builder.RegisterSerilog(loggerConfiguration);
builder.RegisterInstance(Log.Logger);

var logger = Log.ForContext<Program>();
logger.Information("ProcessId: {ProcessId}", Environment.ProcessId);
logger.Information("Version: {Version}", Environment.Version);
logger.Information("Is64BitProcess: {Is64BitProcess}", Environment.Is64BitProcess);
logger.Information("Is64BitOperatingSystem: {Is64BitOperatingSystem}", Environment.Is64BitOperatingSystem);
logger.Information("OSVersion: {OSVersion}", Environment.OSVersion);

if (args.Length > 0) AssetPacker.PackDefault(config, Const.Default.AssetsPath);

//todo generate this
builder.AddFactoryData();

builder.AddEngine();
try
{
    var container = builder.Build();
    var proxy = container.CreateBaseLayer();

    var app = new Application(container, proxy);
    app.InitData();

    app.SetupUpdate();
    var src = new CancellationTokenSource();
    src.CancelAfter(TimeSpan.FromMinutes(10));

    try
    {
        await app.Run(src.Token);
        src.Cancel();
    }
    catch (Exception e)
    {
        logger.Error(e, "Error running application");
    }
    finally
    {
        app.Dispose();
    }

    try
    {
        await container.DisposeAsync();
    }
    catch (Exception e)
    {
        logger.Error(e, "Error disposing container");
    }
}
catch (Exception e)
{
    logger.Error(e, "Error building container");
}

File.WriteAllText(AjivaConfig.FileName, JsonSerializer.Serialize(config, AjivaConfigJsonSerializerContext.Default.AjivaConfig));
logger.Information("Writing Config to {FileName}", AjivaConfig.FileName);
Log.CloseAndFlush();

Console.WriteLine("Press any key to exit...");
Console.ReadKey();

namespace Ajiva.Application
{
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

    internal class CallerEnricher : ILogEventEnricher
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
}
