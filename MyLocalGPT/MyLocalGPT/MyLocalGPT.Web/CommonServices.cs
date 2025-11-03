
using MyLocalGPT.Shared.Services;

namespace MyLocalGPT.Web
{
    public class CommonServices
    {
        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDevExpressBlazor(options =>
            {
                options.SizeMode = DevExpress.Blazor.SizeMode.Medium;
            });
            services.AddScoped<DxThemesService>();
        }
    }
}