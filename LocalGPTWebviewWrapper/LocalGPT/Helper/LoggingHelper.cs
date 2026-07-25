
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
                  "Configuring the optional email logger.");
                var configRoot = configuration.Get<BusinessObjects.ConfigurationRoot>();

                _ = services.AddOptions<IOptionsMonitor<EmailLoggerCoreOptions>>()
                    .Bind(configuration.GetSection("LoggingCore:EmailCore"));
                _ = services.Configure<EmailLoggerCoreOptions>(
                    options =>
                    configuration.GetSection("LoggingCore:EmailCore").Bind(options));

                var loggingOptions = configuration.GetSection("LoggingCore").Get<LoggingCoreOptions>();

                if (loggingOptions != null && loggingOptions.EmailCore != null && !string.IsNullOrEmpty(loggingOptions.EmailCore.SenderEmail) && loggingOptions.EmailCore.CoreLogLevel != CoreLogLevel.None)
                {

                    Console.WriteLine("Registering the optional email logger provider.");
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
                  "Configuring the optional file logger.");
                _ = services.AddOptions<IOptionsMonitor<FileLoggerCoreOptions>>()
                    .Bind(configuration.GetSection("LoggingCore:FileCore"));
                _ = services.Configure<FileLoggerCoreOptions>(
                    options =>
                    configuration.GetSection("LoggingCore:FileCore").Bind(options));

                var loggingOptions = configuration.GetSection("LoggingCore").Get<LoggingCoreOptions>();

                if (loggingOptions != null && loggingOptions?.FileCore != null && loggingOptions.FileCore.CoreLogLevel != CoreLogLevel.None)
                {
                    Console.WriteLine(
                  "Registering the optional file logger provider.");
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

        public static void AddDatabaseLoggerIfConfigured(
            ILoggingBuilder loggingBuilder,
            IServiceCollection services,
            IConfiguration configuration)
        {
            try
            {
                Console.WriteLine(
                  $"Trying configure LoggingCore:DatabaseCore in {configuration}");
                services.AddOptions<DatabaseLoggerCoreOptions>()
                    .Bind(configuration.GetSection("LoggingCore:DatabaseCore"));
                services.Configure<DatabaseLoggerCoreOptions>(
                    options =>
                    configuration.GetSection("LoggingCore:DatabaseCore").Bind(options));

                var loggingOptions = configuration.GetSection("LoggingCore").Get<LoggingCoreOptions>();

                if (loggingOptions?.DatabaseCore is not null && loggingOptions.DatabaseCore.CoreLogLevel != CoreLogLevel.None)
                {
                    Console.WriteLine("Registering the optional database logger provider.");
                    loggingBuilder.AddFilter<DatabaseLoggerProvider>((_, _) => true);
                    _ = services.AddSingleton<ILoggerProvider, DatabaseLoggerProvider>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in AddDatabaseLoggerIfConfigured: " + ex.Message);
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
                    "Configuring LocalGPT logging providers.");
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
                    AddDatabaseLoggerIfConfigured(loggingBuilder, services, configuration);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ConfigureCustomLoggersWithConsoleAndDebug{ex.ToString()}");
            }
        }
    }
}
