
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Enums;
using LocalGPT.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace LocalGPT.Helper
{
    public static class LoggingHelper
    {
        private static void AddEmailLoggerIfConfigured(
            ILoggingBuilder loggingBuilder,
            IServiceCollection services,
            IConfiguration configuration)
        {
            try
            {
                Console.WriteLine(
                  $"Trying configure LoggingCore:EmailCore in {configuration.ToString()}");
                var configRoot = configuration.Get<BusinessObjects.ConfigurationRoot>();

                _ = services.AddOptions<IOptionsMonitor<EmailLoggerCoreOptions>>()
                    .Bind(configuration.GetSection("LoggingCore:EmailCore"));
                _ = services.Configure<EmailLoggerCoreOptions>(
                    options =>
                    configuration.GetSection("LoggingCore:EmailCore").Bind(options));

                var loggingOptions = configuration.GetSection("LoggingCore").Get<LoggingCoreOptions>();

                if (loggingOptions != null && loggingOptions.EmailCore != null && !string.IsNullOrEmpty(loggingOptions.EmailCore.SenderEmail) && loggingOptions.EmailCore.CoreLogLevel != CoreLogLevel.None)
                {

                    Console.WriteLine($"EmailCore Sqllogger as singleton in {configuration.ToString()}");
                    _ = services.AddSingleton<ILoggerProvider>(
                        provider =>
                        {
                            var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<EmailLoggerCoreOptions>>();
                            return new EmailLoggerProvider(optionsMonitor);
                        });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in AddEmailLoggerIfConfigured: " + ex.Message);
            }

        }

        public static void AddFileLoggerIfConfigured(
            ILoggingBuilder loggingBuilder,
            IServiceCollection services,
            IConfiguration configuration)
        {
            try
            {
                Console.WriteLine(
                  $"Trying configure LoggingCore:FileCore in {configuration.ToString()}");
                _ = services.AddOptions<IOptionsMonitor<FileLoggerCoreOptions>>()
                    .Bind(configuration.GetSection("LoggingCore:FileCore"));
                _ = services.Configure<FileLoggerCoreOptions>(
                    options =>
                    configuration.GetSection("LoggingCore:FileCore").Bind(options));

                var loggingOptions = configuration.GetSection("LoggingCore").Get<LoggingCoreOptions>();

                if (loggingOptions != null && loggingOptions?.FileCore != null && loggingOptions.FileCore.CoreLogLevel != CoreLogLevel.None)
                {
                    Console.WriteLine(
                  $"Adding Filelogger as singleton in {configuration.ToString()}");
                    _ = services.AddSingleton<ILoggerProvider>(
                        provider =>
                        {
                            var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<FileLoggerCoreOptions>>();
                            return new FileLoggerProvider(optionsMonitor);
                        });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in AddFileLoggerIfConfigured: " + ex.Message);
            }

        }

        public static void ConfigureCustomLoggersWithConsoleAndDebug(
            ILoggingBuilder loggingBuilder,
            IServiceCollection services,
            IConfiguration configuration)
        {
            try
            {
                Console.WriteLine(
                    $"Trying configure logging in ConfigureCustomLoggersWithConsoleAndDebug{configuration.ToString()}:{Environment.NewLine}{services}");
                services.AddOptions<LoggingCoreOptions>()
                    .Bind(configuration.GetSection("LoggingCore"));
                var loggingOptions = configuration.GetSection("LoggingCore").Get<LoggingCoreOptions>();
                services.Configure<LoggingCoreOptions>(
                    options =>
                    configuration.GetSection("LoggingCore").Bind(options));
                if (loggingOptions != null && loggingOptions.CoreLogLevel != CoreLogLevel.None)
                {
                    loggingBuilder.AddJsonConsole();
                    loggingBuilder.AddConsole();
#if DEBUG

                    loggingBuilder.AddDebug();
#endif

                    AddEmailLoggerIfConfigured(loggingBuilder, services, configuration);
                    AddFileLoggerIfConfigured(loggingBuilder, services, configuration);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ConfigureCustomLoggersWithConsoleAndDebug{ex.ToString()}");
            }
        }
    }
}
