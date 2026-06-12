
using LocalGPT.Helper;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
///
///https://github.com/dotnet/docs/tree/main/docs/core/extensions/snippets/configuration/console-custom-logging
///
namespace LocalGPT.Helper
{
    public class ColorConsoleLoggerConfiguration
    {
        public const string ColorConsoleLoggerConfigurationCore = "ColorConsoleLoggerConfigurationCore";

        public int EventId { get; set; }
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
