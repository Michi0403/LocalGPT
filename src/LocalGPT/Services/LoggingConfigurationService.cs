using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Enums;
using LocalGPT.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services;

/// <summary>Configures the optional LocalGPT logging providers during composition-root startup.</summary>
/// <param name="services">Service collection dependency used by the logging configuration workflow to provide the corresponding application capability.</param>
/// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class LoggingConfigurationService(
    IServiceCollection services,
    IConfiguration configuration,
    ILogger logger)
{
    /// <summary>
    /// Performs configure as part of the logging configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="loggingBuilder">Logging builder dependency used by the logging configuration workflow to provide the corresponding application capability.</param>
    public void Configure(ILoggingBuilder loggingBuilder)
    {
        ArgumentNullException.ThrowIfNull(loggingBuilder);
        try
        {
            logger.LogInformation("Configuring LocalGPT logging providers.");
            services.AddOptions<LoggingCoreOptions>()
                .Bind(configuration.GetSection("LoggingCore"));
            services.Configure<LoggingCoreOptions>(options =>
                configuration.GetSection("LoggingCore").Bind(options));

            var loggingOptions = configuration.GetSection("LoggingCore").Get<LoggingCoreOptions>();
            if (loggingOptions is null || loggingOptions.CoreLogLevel == CoreLogLevel.None)
            {
                logger.LogInformation("Optional LocalGPT logging providers are disabled by configuration.");
                return;
            }

            loggingBuilder.AddJsonConsole();
            loggingBuilder.AddConsole();
#if DEBUG
            loggingBuilder.AddDebug();
#endif
            AddEmailLoggerIfConfigured(loggingOptions);
            AddFileLoggerIfConfigured(loggingOptions);
            AddDatabaseLoggerIfConfigured(loggingBuilder, loggingOptions);
            logger.LogInformation("Configured the enabled LocalGPT logging providers.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not configure the LocalGPT logging providers; startup will continue with the providers configured before the failure.");
        }
    }

    /// <summary>
    /// Adds email logger if configured as part of the logging configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="loggingOptions">Logging options value supplied to the logging configuration operation and used when producing its result.</param>
    private void AddEmailLoggerIfConfigured(LoggingCoreOptions loggingOptions)
    {
        try
        {
            logger.LogInformation("Evaluating the optional email logger configuration.");
            services.Configure<EmailLoggerCoreOptions>(options =>
                configuration.GetSection("LoggingCore:EmailCore").Bind(options));

            if (loggingOptions.EmailCore is null ||
                string.IsNullOrWhiteSpace(loggingOptions.EmailCore.SenderEmail) ||
                loggingOptions.EmailCore.CoreLogLevel == CoreLogLevel.None)
            {
                return;
            }

            services.AddSingleton<ILoggerProvider>(provider =>
                new EmailLoggerProvider(provider.GetRequiredService<IOptionsMonitor<EmailLoggerCoreOptions>>()));
            logger.LogInformation("Registered the optional email logger provider.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not configure the optional email logger provider; startup will continue without it.");
        }
    }

    /// <summary>
    /// Adds file logger if configured as part of the logging configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="loggingOptions">Logging options value supplied to the logging configuration operation and used when producing its result.</param>
    private void AddFileLoggerIfConfigured(LoggingCoreOptions loggingOptions)
    {
        try
        {
            logger.LogInformation("Evaluating the optional file logger configuration.");
            services.Configure<FileLoggerCoreOptions>(options =>
                configuration.GetSection("LoggingCore:FileCore").Bind(options));

            if (loggingOptions.FileCore is null || loggingOptions.FileCore.CoreLogLevel == CoreLogLevel.None)
                return;

            services.AddSingleton<ILoggerProvider>(provider =>
                new FileLoggerProvider(provider.GetRequiredService<IOptionsMonitor<FileLoggerCoreOptions>>()));
            logger.LogInformation("Registered the optional file logger provider.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not configure the optional file logger provider; startup will continue without it.");
        }
    }

    /// <summary>
    /// Adds database logger if configured as part of the logging configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="loggingBuilder">Logging builder dependency used by the logging configuration workflow to provide the corresponding application capability.</param>
    /// <param name="loggingOptions">Logging options value supplied to the logging configuration operation and used when producing its result.</param>
    private void AddDatabaseLoggerIfConfigured(ILoggingBuilder loggingBuilder, LoggingCoreOptions loggingOptions)
    {
        try
        {
            logger.LogInformation("Evaluating the optional database logger configuration.");
            services.AddOptions<DatabaseLoggerCoreOptions>()
                .Bind(configuration.GetSection("LoggingCore:DatabaseCore"));
            services.Configure<DatabaseLoggerCoreOptions>(options =>
                configuration.GetSection("LoggingCore:DatabaseCore").Bind(options));

            if (loggingOptions.DatabaseCore is null || loggingOptions.DatabaseCore.CoreLogLevel == CoreLogLevel.None)
                return;

            loggingBuilder.AddFilter<DatabaseLoggerProvider>((_, _) => true);
            services.AddSingleton<ILoggerProvider, DatabaseLoggerProvider>();
            logger.LogInformation("Registered the optional database logger provider.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not configure the optional database logger provider; startup will continue without it.");
        }
    }
}
