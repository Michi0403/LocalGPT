
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
///
///https://github.com/dotnet/docs/tree/main/docs/core/extensions/snippets/configuration/console-custom-logging
///
namespace LocalGPT.Helper
{

    /// <summary>
    /// Provides color console logger provider operations.
    /// </summary>
    [ProviderAlias("ColorConsole")]
    public sealed class ColorConsoleLoggerProvider : ILoggerProvider
    {
        private ColorConsoleLoggerConfiguration _currentConfig;
        private readonly ConcurrentDictionary<string, ColorConsoleLogger> _loggers =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Runs the color console logger provider operation.
        /// </summary>
        public ColorConsoleLoggerProvider(
            ColorConsoleLoggerConfiguration config)
        {
            _currentConfig = config;
        }

        /// <summary>
        /// Creates logger.
        /// </summary>
        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new ColorConsoleLogger(name, GetCurrentConfig));

        /// <summary>
        /// Gets current config.
        /// </summary>
        private ColorConsoleLoggerConfiguration GetCurrentConfig() => _currentConfig;

        /// <summary>
        /// Runs the dispose operation.
        /// </summary>
        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}