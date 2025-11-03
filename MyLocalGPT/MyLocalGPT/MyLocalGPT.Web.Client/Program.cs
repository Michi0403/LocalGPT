using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MyLocalGPT.Shared.Services;
using MyLocalGPT.Web.Client.Services;

namespace MyLocalGPT.Web.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // Add device-specific services used by the MyLocalGPT.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            builder.Services.AddDevExpressWebAssemblyBlazorPdfViewer();

            DevExpress.XtraPrinting.PrintingOptions.Pdf.RenderingEngine = DevExpress.XtraPrinting.XRPdfRenderingEngine.Skia;

            await builder.Build().RunAsync();
        }
    }
}
