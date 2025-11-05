using DevExpress.CodeParser;
using LocalGPT.Components;
using LocalGPT.Services;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
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

            //var builder = WebApplication.CreateBuilder(options);
            var builder = WebApplication.CreateBuilder();
            //StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

            Port = GetFreePort();
            builder.WebHost.UseKestrel().UseUrls($"https://localhost:{Port}");

            //builder.Host.UseContentRoot(options.ContentRootPath);
            //builder.WebHost.UseWebRoot(Path.Combine(options.WebRootPath, "wwwroot"));         // ensure /wwwroot is found

            // 2) Load static web assets for THIS assembly (enables /_content/* and isolated CSS)
            // Load static web assets (/_content/** and CSS isolation)
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddHealthChecks();
            builder.Services.AddDevExpressBlazor(o => o.SizeMode = DevExpress.Blazor.SizeMode.Small);
            builder.Services.AddMvc();
            builder.Services.AddScoped<DxThemesService>();
            builder.Services.AddDevExpressServerSideBlazorPdfViewer();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAntiforgery();                 // ✅ after routing, before endpoints

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

      