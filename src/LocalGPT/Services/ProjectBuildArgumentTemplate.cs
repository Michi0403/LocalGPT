using System.Text;

namespace LocalGPT.Services;

/// <summary>Formats saved workspace build arguments without owning compiler selection or inventing build flags.</summary>
internal static class ProjectBuildArgumentTemplate
{
    /// <summary>Expands persisted workspace argument templates for any registered language toolchain.</summary>
    public static string Expand(string template, string target, string configuration, string? outputDirectory)
    {
        var targetDirectory = Path.GetDirectoryName(target) ?? string.Empty;
        foreach (var (name, value) in new[]
        {
            ("target", target), ("targetDirectory", targetDirectory),
            ("configuration", string.IsNullOrWhiteSpace(configuration) ? "Debug" : configuration.Trim()),
            ("outputDirectory", outputDirectory ?? string.Empty)
        })
        {
            var placeholder = "{" + name + "}";
            var quoted = QuoteArgument(value);
            template = template.Replace("\"" + placeholder + "\"", quoted, StringComparison.OrdinalIgnoreCase)
                .Replace(placeholder, quoted, StringComparison.OrdinalIgnoreCase);
        }
        return template;
    }

    /// <summary>Quotes a single process argument, including trailing backslashes and embedded quotation marks.</summary>
    private static string QuoteArgument(string value)
    {
        var result = new StringBuilder("\"");
        var pendingBackslashes = 0;
        foreach (var ch in value)
        {
            if (ch == '\\') { pendingBackslashes++; continue; }
            if (ch == '"')
            {
                result.Append('\\', pendingBackslashes * 2 + 1).Append('"');
                pendingBackslashes = 0;
                continue;
            }
            result.Append('\\', pendingBackslashes).Append(ch);
            pendingBackslashes = 0;
        }
        result.Append('\\', pendingBackslashes * 2).Append('"');
        return result.ToString();
    }

}
