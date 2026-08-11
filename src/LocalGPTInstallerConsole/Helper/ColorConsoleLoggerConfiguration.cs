
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
    /// Represents a color console logger configuration.
    /// </summary>
    public class ColorConsoleLoggerConfiguration
    {
        /// <summary>
        /// Stores color console logger configuration core.
        /// </summary>
        public const string ColorConsoleLoggerConfigurationCore = "ColorConsoleLoggerConfigurationCore";

        /// <summary>
        /// Gets or sets event identifier.
        /// </summary>
        public int EventId { get; set; }
        /// <summary>
        /// Gets or sets log level to color map.
        /// </summary>
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
