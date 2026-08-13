
using LocalGPT.Helper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
///
///https://github.com/dotnet/docs/tree/main/docs/core/extensions/snippets/configuration/console-custom-logging
///
namespace LocalGPT.Helper
{
    /// <summary>
    /// Carries the configurable color console logger settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class ColorConsoleLoggerConfiguration
    {
        /// <summary>
        /// Defines the color console logger configuration core constant used by <see cref="ColorConsoleLoggerConfiguration"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string ColorConsoleLoggerConfigurationCore = "ColorConsoleLoggerConfigurationCore";

        /// <summary>
        /// Gets or sets the stable event identifier used to identify or correlate this color console logger instance with related application state.
        /// </summary>
        /// <value>The event identifier value exposed by <see cref="ColorConsoleLoggerConfiguration"/>.</value>
        public int EventId { get; set; }
        /// <summary>
        /// Gets or sets the log level to color map collection maintained or exposed by this color console logger instance for downstream processing.
        /// </summary>
        /// <value>The log level to color map value exposed by <see cref="ColorConsoleLoggerConfiguration"/>.</value>
        public Dictionary<LogLevel, ConsoleColor> LogLevelToColorMap { get; set; } = new()
        {
            [LogLevel.Information] = ConsoleColor.Cyan,
            [LogLevel.Warning] = ConsoleColor.Yellow,
            [LogLevel.Debug] = ConsoleColor.Green,
            [LogLevel.Error] = ConsoleColor.Red,
            [LogLevel.Critical] = ConsoleColor.DarkYellow,
        };
    }
}
