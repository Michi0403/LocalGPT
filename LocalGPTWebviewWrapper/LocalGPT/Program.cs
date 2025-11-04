using DevExpress.CodeParser;
using LocalGPT.Components;
using LocalGPT.Services;
using System.Net;
using System.Net.Sockets;
using static System.Net.Mime.MediaTypeNames;
namespace LocalGPT
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var app = CreateWebApplicationBuilder(args);
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAntiforgery();   // 👈 HERE — after routing, before endpoints

            app.MapHealthChecks("/health");
            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AllowAnonymous();

            app.Run();
        }
        public static int Port { get; private set; } = 0;
        public static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
        
        public static WebApplication CreateWebApplicationBuilder(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            Port = GetFreePort();
            builder.WebHost
                .UseKestrel()
                .UseUrls($"https://localhost:{Port}");

            // Add services to the container.
            builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
            builder.Services.AddHealthChecks();

            builder.Services.AddDevExpressBlazor(options =>
            {
                options.SizeMode = DevExpress.Blazor.SizeMode.Small;
            });
            builder.Services.AddMvc();
            builder.Services.AddScoped<DxThemesService>();

            builder.Services.AddDevExpressServerSideBlazorPdfViewer();

            return builder.Build();
            //if (!app.Environment.IsDevelopment())
            //{
            //    app.UseExceptionHandler("/Error", createScopeForErrors: true);
            //    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            //    app.UseHsts();
            //}

            //app.UseHttpsRedirection();

            //app.UseStaticFiles();
            //app.UseAntiforgery();

            //app.MapRazorComponents<App>()
            //    .AddInteractiveServerRenderMode()
            //    .AllowAnonymous();

            //app.Run();

        }
    } 
}

      