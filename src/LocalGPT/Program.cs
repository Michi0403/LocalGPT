using Azure;
using Azure.AI.OpenAI;
using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.CodeParser;
using DevExpress.DataProcessing.InMemoryDataProcessor;
using DevExpress.XtraCharts;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Components;
using LocalGPT.Diagnostics;
using LocalGPT.Helper;
using LocalGPT.Hubs;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using LocalGPT.Services.Formatting;
using LocalGPT.Services.Persistence;
using LocalGPT.Services.OneWire;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LocalGPT.Services.Helpers;
namespace LocalGPT
{
    /// <summary>
    /// Represents a program application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public static partial class Program
    {
        // Installer/WebView compatibility contract. Do not remove or silently change these defaults.
        /// <summary>
        /// Defines the default port constant used by <see cref="Program"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const int DefaultPort = 5000;
        /// <summary>
        /// Defines the default one wire port constant used by <see cref="Program"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const int DefaultOneWirePort = OneWireProtocol.DefaultServicePort;
        /// <summary>
        /// Defines the default one wire discovery port constant used by <see cref="Program"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const int DefaultOneWireDiscoveryPort = OneWireProtocol.DefaultDiscoveryPort;

        /// <summary>
        /// Stores the internal runtime port state used by <see cref="Program"/> while executing its surrounding workflow.
        /// </summary>
        private static int runtimePort = DefaultPort;
        /// <summary>
        /// Stores the internal runtime one wire port state used by <see cref="Program"/> while executing its surrounding workflow.
        /// </summary>
        private static int runtimeOneWirePort = DefaultOneWirePort;
        /// <summary>
        /// Stores the internal runtime one wire discovery port state used by <see cref="Program"/> while executing its surrounding workflow.
        /// </summary>
        private static int runtimeOneWireDiscoveryPort = DefaultOneWireDiscoveryPort;

        // Public read-only compatibility surface consumed by the WinUI wrapper and installer wiring.
        // Startup updates the private snapshot atomically; callers cannot mutate it.
        /// <summary>
        /// Gets the port value that forms part of the program state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The port value exposed by <see cref="Program"/>.</value>
        public static System.Int32 Port => System.Threading.Volatile.Read(ref runtimePort);
        /// <summary>
        /// Gets the one wire port value that forms part of the program state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The one wire port value exposed by <see cref="Program"/>.</value>
        public static System.Int32 OneWirePort => System.Threading.Volatile.Read(ref runtimeOneWirePort);
        /// <summary>
        /// Gets the one wire discovery port value that forms part of the program state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The one wire discovery port value exposed by <see cref="Program"/>.</value>
        public static System.Int32 OneWireDiscoveryPort => System.Threading.Volatile.Read(ref runtimeOneWireDiscoveryPort);
        /// <summary>
        /// Gets the base URL that identifies the network or application endpoint associated with this program state.
        /// </summary>
        /// <value>The base URL value exposed by <see cref="Program"/>.</value>
        public static string BaseUrl => $"http://127.0.0.1:{Port}";
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
        [STAThread]
        static void Main(string[] args)
        {
            var app = BuildWebApp(args);
            app.Run();
        }

        /// <summary>
        /// Builds web app for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
        /// <returns>The web application produced by the operation.</returns>
        public static WebApplication BuildWebApp(string[]? args = null)
        {
            var exeDir = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
            using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
            var logger = loggerFactory.CreateLogger("Startup");
            //EnsureGeneratedStaticWebAssetContentRoots(exeDir, logger);

            var builder = WebApplication.CreateBuilder(CreateWebApplicationOptions(args));
            builder.Host.UseDefaultServiceProvider((_, options) =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            });
            logger.LogInformation("Created builder with startup service-provider validation enabled.");
            ConfigureAppConfiguration(builder, logger);
            logger.LogInformation("Configured app configuration.");
            System.Threading.Volatile.Write(ref runtimePort, ResolveRequestedPort(args, builder.Configuration, logger));
            System.Threading.Volatile.Write(ref runtimeOneWirePort, ResolveConfiguredPort(args, builder.Configuration, "--onewire-port", "LOCALGPT_ONEWIRE_PORT", $"{OneWireOptions.SectionName}:ServicePort", DefaultOneWirePort, allowDynamic: false, logger));
            System.Threading.Volatile.Write(ref runtimeOneWireDiscoveryPort, ResolveConfiguredPort(args, builder.Configuration, "--onewire-discovery-port", "LOCALGPT_ONEWIRE_DISCOVERY_PORT", $"{OneWireOptions.SectionName}:DiscoveryPort", DefaultOneWireDiscoveryPort, allowDynamic: false, logger));
            ConfigureLogging(builder, logger);
            logger.LogInformation("Configured logging.");
            ConfigureOptionsAndServices(builder, logger);
            logger.LogInformation("Configured options and services.");
            ConfigureSignalR(builder.Services, logger);
            logger.LogInformation("Configured SignalR.");
            System.Threading.Volatile.Write(ref runtimePort, ConfigureKestrel(builder, Port, args, logger));
            ValidatePortContracts(logger);
            var port = Port;
            logger.LogInformation("Configured authoritative LocalGPT loopback endpoint on http://127.0.0.1:{Port}.", port);
            ConfigureResponseCompression(builder.Services, logger);
            logger.LogInformation("Configured response compression.");
            ConfigureBlazorAndMvc(builder, logger);
            logger.LogInformation("Configured Blazor and MVC.");
            ConfigureJsonOptions(builder.Services, logger);
            logger.LogInformation("Configured JSON options.");
            ConfigureForwardedHeaders(builder.Services, logger);
            logger.LogInformation("Configured forwarded headers.");
            new ServiceMethodDiagnosticsRegistration(logger).Apply(builder.Services, builder.Environment.IsDevelopment());
            logger.LogInformation("Configured bounded service method diagnostics.");

            var app = builder.Build();
            logger.LogInformation("Built web application.");
            ConfigureMiddlewareAndEndpoints(app, logger);
            logger.LogInformation("Configured middleware and endpoints.");
            var runtimeEndpointLogger = app.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("LocalGPT.RuntimeEndpoint");
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                WriteRuntimeEndpointFile(Port, runtimeEndpointLogger);
                runtimeEndpointLogger.LogInformation("Wrote runtime endpoint file after the LocalGPT listener started.");
            });
            app.Lifetime.ApplicationStopped.Register(() => DeleteRuntimeEndpointFile(runtimeEndpointLogger));

            return app;
        }

        //private static void TraceStartup(string message, ILogger logger)
        //{
        //    try
        //    {
        //        var line = $"[{DateTimeOffset.Now:O}] pid={Environment.ProcessId} {message}{Environment.NewLine}";
        //        //TryAppendStartupTrace(line, logger);

        //        if (!string.Equals(
        //            Environment.GetEnvironmentVariable("LOCALGPT_STARTUP_TRACE"),
        //            "1",
        //            StringComparison.OrdinalIgnoreCase))
        //        {
        //            return;
        //        }
        //        //TryAppendStartupTrace($"[LocalGPT startup] {line}", logger);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, $"Error in TraceStartup {message}");
        //    }
           
        //}

        //private static void TryAppendStartupTrace(string line, ILogger logger)
        //{
        //    try
        //    {
        //        foreach (var directory in GetRuntimeTraceDirectories())
        //        {
        //            Directory.CreateDirectory(directory);
        //            File.AppendAllText(Path.Combine(directory, $"startup-trace-{Environment.ProcessId}.log"), line);
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        logger.LogError(ex, $"Error in TryAppendStartupTrace line {line}");
        //        TraceStartup(ex.ToString(), logger);
        //        // Startup tracing must never block app launch.
        //    }
        //}

        /// <summary>
        /// Retrieves runtime trace directories for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <returns>The collection produced by the operation.</returns>
        private static IEnumerable<string> GetRuntimeTraceDirectories()
        {
            var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localApplicationData))
                return Array.Empty<string>();

            return [Path.Combine(localApplicationData, "LocalGPT", "runtime")];
        }

        /// <summary>
        /// Creates web application options for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
        /// <returns>The web application options produced by the operation.</returns>
        private static WebApplicationOptions CreateWebApplicationOptions( string[]? args)
        {
            return new WebApplicationOptions
            {
                ApplicationName = typeof(Program).Assembly.GetName().Name,
                ContentRootPath = AppContext.BaseDirectory,
                WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot"),
                Args = args ?? Array.Empty<string>()
            };
        }

        /// <summary>
        /// Performs configure app configuration for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="builder">Builder value supplied to the program operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void ConfigureAppConfiguration(WebApplicationBuilder builder, ILogger logger)
        {
            try
            {
                var userSettingsFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LocalGPT",
                    "appsettings.user.json");
                Directory.CreateDirectory(Path.GetDirectoryName(userSettingsFile)!);

                builder.Configuration
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile(
                   $"appsettings.{builder.Environment.EnvironmentName}.json",
                   optional: true,
                   reloadOnChange: true)
               .AddJsonFile(userSettingsFile, optional: true, reloadOnChange: true)
               .AddEnvironmentVariables();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Application configuration setup failed.");
                // TryAppendStartupTrace(ex.ToString(), logger);
            }
           
        }
        /// <summary>
        /// Configure Logging but also here was the logfile bypass method, anyway it... pulled that out of my core and restructured the whole app against every guide and telling so... rly bad.
        /// </summary>
        /// <param name="builder">Builder value supplied to the program operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void ConfigureLogging(WebApplicationBuilder builder, ILogger logger)
        {
            try
            {

                if (!builder.Environment.IsDevelopment())
                    builder.Logging.AddFilter((category, level) => level >= LogLevel.Warning);

                builder.Services.AddLogging(logging =>
                    new LoggingConfigurationService(builder.Services, builder.Configuration, logger).Configure(logging));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureLogging builder {builder.ToString()}", builder);
                //TryAppendStartupTrace(ex.ToString(), logger);
            }
           
        }
}
}
