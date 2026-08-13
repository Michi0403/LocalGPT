
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
    /// Provides color console logger data or behavior to callers while hiding the underlying acquisition and configuration details.
    /// </summary>
    [ProviderAlias("ColorConsole")]
    public sealed class ColorConsoleLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// Stores the internal current config state used by <see cref="ColorConsoleLoggerProvider"/> while executing its surrounding workflow.
        /// </summary>
        private ColorConsoleLoggerConfiguration _currentConfig;
        /// <summary>
        /// Stores the in-memory loggers collection maintained internally by <see cref="ColorConsoleLoggerProvider"/> for its current workflow state.
        /// </summary>
        private readonly ConcurrentDictionary<string, ColorConsoleLogger> _loggers =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new <see cref="ColorConsoleLoggerProvider"/> instance and captures the dependencies or initial state required by its color console logger workflow.
        /// </summary>
        /// <param name="config">Config value supplied to the color console logger operation and used when producing its result.</param>
        public ColorConsoleLoggerProvider(
            ColorConsoleLoggerConfiguration config)
        {
            _currentConfig = config;
        }

        /// <summary>
        /// Creates logger for <see cref="ColorConsoleLoggerProvider"/>, keeping the operation consistent with the state and invariants of the surrounding color console logger workflow.
        /// </summary>
        /// <param name="categoryName">Category name value supplied to the color console logger operation and used when producing its result.</param>
        /// <returns>The i logger produced by the operation.</returns>
        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new ColorConsoleLogger(name, GetCurrentConfig));

        /// <summary>
        /// Retrieves current config for <see cref="ColorConsoleLoggerProvider"/>, keeping the operation consistent with the state and invariants of the surrounding color console logger workflow.
        /// </summary>
        /// <returns>The color console logger configuration produced by the operation.</returns>
        private ColorConsoleLoggerConfiguration GetCurrentConfig() => _currentConfig;

        /// <summary>
        /// Releases resources owned by <see cref="ColorConsoleLoggerProvider"/> and leaves the color console logger workflow in a safely disposed state.
        /// </summary>
        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
