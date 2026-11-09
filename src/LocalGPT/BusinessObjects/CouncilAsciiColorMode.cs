using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Selects the palette contract used by the shared LocalGPT ASCII game surface.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilAsciiColorMode
{
    /// <summary>Preserves the existing LocalGPT terminal theme for unstyled cells.</summary>
    TerminalDefault = 0,
    /// <summary>Uses the standard sixteen ANSI terminal colors.</summary>
    Ansi16 = 1,
    /// <summary>Uses xterm-compatible indexed colors in the range 0 through 255.</summary>
    Indexed256 = 2
}
