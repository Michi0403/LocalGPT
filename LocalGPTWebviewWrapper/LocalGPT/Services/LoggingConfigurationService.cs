
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Enums;
using LocalGPT.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace LocalGPT.Services
{
    public sealed class LoggingConfigurationService
    {
        private readonly IServiceCollection services;
        private readonly IConfiguration configuration;

        public LoggingConfigurationService(IServiceCollection services, IConfiguration configuration)
        {
            this.services = services;
            this.configuration = configuration;
        }
        private void AddEmailLoggerIfConfigured(ILoggingBuilder loggingBuilder)
        {
            try
            {
                Console.WriteLine(
                  "Configuring the optional email logger.");
                var configRoot = configuration.Get<BusinessObjects.ConfigurationRoot>();

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

        private void AddFileLoggerIfConfigured(ILoggingBuilder loggingBuilder)
        {
            try
            {
                Console.WriteLine(
                  "Configuring the optional file logger.");
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

        private void AddDatabaseLoggerIfConfigured(ILoggingBuilder loggingBuilder)
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


        public void Configure(ILoggingBuilder loggingBuilder)
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

                    AddEmailLoggerIfConfigured(loggingBuilder);
                    AddFileLoggerIfConfigured(loggingBuilder);
                    AddDatabaseLoggerIfConfigured(loggingBuilder);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ConfigureCustomLoggersWithConsoleAndDebug{ex.ToString()}");
            }
        }
    }
}
