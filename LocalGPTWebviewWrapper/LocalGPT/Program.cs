using DevExpress.CodeParser;
using DevExpress.XtraCharts;
using LocalGPT.Components;
using LocalGPT.Helper;
using LocalGPT.Hubs;
using LocalGPT.Services;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;
namespace LocalGPT
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var app = BuildWebApp(args);

            app.Run();
        }
        public static int Port { get; private set; } = 0;
        public static WebApplication BuildWebApp(string[]? args = null)
        {
            // Put the content root/web root where the LocalGPT assembly lives.
            // This is crucial when you start the server from the WinUI process.
            var exeDir = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;

            //var options = new WebApplicationOptions
            //{
            //    ContentRootPath = exeDir,
            //    WebRootPath = Path.Combine(exeDir, "wwwroot"),
            //    Args = args ?? Array.Empty<string>()
            //};
            var options = new WebApplicationOptions
            {
                ApplicationName = typeof(Program).Assembly.GetName().Name, // "LocalGPT"
                ContentRootPath = exeDir,
                WebRootPath = Path.Combine(exeDir, "wwwroot"),
                Args = args ?? Array.Empty<string>()
            };

            var builder = WebApplication.CreateBuilder(options);
            var configuration = builder.Configuration;
            var configRoot = configuration.Get<LocalGPT.BusinessObjects.ConfigurationRoot>();
            builder.Services.AddLogging(
               logging => LoggingHelper.ConfigureCustomLoggersWithConsoleAndDebug(
                   logging,
                   builder.Services,
                   configuration));
            builder.Services.Configure<CircuitOptions>(
            o =>
o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(30));
            //var builder = WebApplication.CreateBuilder();
            StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
            builder.Services.AddOptions();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSignalR()
              .AddMessagePackProtocol(options =>
              {
                  options.SerializerOptions = MessagePack.MessagePackSerializerOptions.Standard
                      .WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance)
                      .WithSecurity(MessagePack.MessagePackSecurity.UntrustedData);
              }).AddJsonProtocol(options =>
              {
                  options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                  options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                  options.PayloadSerializerOptions.WriteIndented = true;
                  options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                  options.PayloadSerializerOptions.IgnoreReadOnlyFields = false;
                  options.PayloadSerializerOptions.IgnoreReadOnlyProperties = false;
                  options.PayloadSerializerOptions.IncludeFields = false;
                  options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                  options.PayloadSerializerOptions.AllowTrailingCommas = true;
                  options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                  options.PayloadSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString;


              });
            Port = GetFreePort();
            builder.WebHost.UseKestrel().UseUrls($"https://localhost:{Port}");
            builder.Services.AddResponseCompression
               (opts =>
               {
                   opts.EnableForHttps = true;
                   opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] {
        "application/octet-stream"
   });
               });
                   //builder.Host.UseContentRoot(options.ContentRootPath);
                   //builder.WebHost.UseWebRoot(Path.Combine(options.WebRootPath, "wwwroot"));         // ensure /wwwroot is found

                   // 2) Load static web assets for THIS assembly (enables /_content/* and isolated CSS)
                   // Load static web assets (/_content/** and CSS isolation)
                   builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddHealthChecks();
            builder.Services.AddDevExpressBlazor(o => o.SizeMode = DevExpress.Blazor.SizeMode.Small);
            builder.Services.AddMvc();
            builder.Services.AddScoped<ThemeService>();
            builder.Services.Configure<ConfigurationRoot>(configuration);
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ConfigurationRoot>>().Value);
            builder.Services.Configure<JsonOptions>(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.IgnoreReadOnlyFields = false;
                options.JsonSerializerOptions.IgnoreReadOnlyProperties = false;
                options.JsonSerializerOptions.IncludeFields = false;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.AllowTrailingCommas = true;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString;
            });
            builder.Services.Configure<ForwardedHeadersOptions>(
                options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });
            builder.Services.AddDevExpressServerSideBlazorPdfViewer();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }
            _ = app.UseForwardedHeaders(
                new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });
            app.UseHttpsRedirection();
            _ = app.UseRequestLocalization();
            app.UseStaticFiles();
            app.UseRouting();
            _ = app.UseResponseCompression();
            app.UseAntiforgery();                 // ✅ after routing, before endpoints
            app.MapControllers();
            _ = app.MapHub<ChatHub>("/chathub");
            app.MapHealthChecks("/health");
            //app.MapBlazorHub();
            //app.MapFallbackToPage("/_Host");
            app.MapGet("/__diag", (IWebHostEnvironment env) => new {
                env.EnvironmentName,
                env.ContentRootPath,
                env.WebRootPath,
                AppAssembly = typeof(Program).Assembly.Location
            });
            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode()
               .AllowAnonymous();

            return app;                           // ⬅️ no Run() here
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    } 
}

      