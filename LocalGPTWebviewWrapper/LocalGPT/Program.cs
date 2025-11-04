using DevExpress.CodeParser;
using LocalGPT.Components;
using LocalGPT.Services;
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
            var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());

            Port = GetFreePort();
            builder.WebHost.UseKestrel().UseUrls($"https://localhost:{Port}");

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

      