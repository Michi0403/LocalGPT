
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Text;
///
///https://github.com/dotnet/docs/tree/main/docs/core/extensions/snippets/configuration/console-custom-logging
///
namespace LocalGPT.Helper
{
    /// <summary>
    /// Represents a color console logger application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="name">Name value supplied to the color console logger operation and used when producing its result.</param>
    /// <param name="getCurrentConfig">Get current config value supplied to the color console logger operation and used when producing its result.</param>
    public sealed class ColorConsoleLogger(
    string name,
    Func<ColorConsoleLoggerConfiguration> getCurrentConfig) : ILogger
    {
        /// <summary>
        /// Performs begin scope for <see cref="ColorConsoleLogger"/>, keeping the operation consistent with the state and invariants of the surrounding color console logger workflow.
        /// </summary>
        /// <typeparam name="TState">Type used for t state values handled by <see cref="ColorConsoleLogger"/>.</typeparam>
        /// <param name="state">State value supplied to the color console logger operation and used when producing its result.</param>
        /// <returns>The i disposable produced by the operation.</returns>
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => default!;

        /// <summary>
        /// Determines whether enabled for <see cref="ColorConsoleLogger"/>, keeping the operation consistent with the state and invariants of the surrounding color console logger workflow.
        /// </summary>
        /// <param name="logLevel">Log level value supplied to the color console logger operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsEnabled(LogLevel logLevel) =>
            getCurrentConfig().LogLevelToColorMap.ContainsKey(logLevel);

        /// <summary>
        /// Performs log for <see cref="ColorConsoleLogger"/>, keeping the operation consistent with the state and invariants of the surrounding color console logger workflow.
        /// </summary>
        /// <typeparam name="TState">Type used for t state values handled by <see cref="ColorConsoleLogger"/>.</typeparam>
        /// <param name="logLevel">Log level value supplied to the color console logger operation and used when producing its result.</param>
        /// <param name="eventId">Identifier of the event to use for this operation.</param>
        /// <param name="state">State value supplied to the color console logger operation and used when producing its result.</param>
        /// <param name="exception">Exception value supplied to the color console logger operation and used when producing its result.</param>
        /// <param name="formatter">Formatter value supplied to the color console logger operation and used when producing its result.</param>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            ColorConsoleLoggerConfiguration config = getCurrentConfig();
            if (config.EventId == 0 || config.EventId == eventId.Id)
            {
                ConsoleColor originalColor = Console.ForegroundColor;

                Console.ForegroundColor = config.LogLevelToColorMap[logLevel];
                Console.WriteLine($"[{eventId.Id,2}: {logLevel,-12}]");

                Console.ForegroundColor = originalColor;
                Console.Write($"     {name} - ");

                Console.ForegroundColor = config.LogLevelToColorMap[logLevel];
                Console.Write($"{formatter(state, exception)}");

                Console.ForegroundColor = originalColor;
                Console.WriteLine();
            }
        }
    }
}
