using LocalGPT.BusinessObjects;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Runtime.Plugins;

/// <summary>Provides deterministic validation and copying helpers for persisted runtime-extension definitions.</summary>
public sealed class RuntimePluginDefinitionSupport
{
    /// <summary>Normalizes and validates one persisted runtime-extension definition.</summary>
    /// <param name="definition">Definition to normalize and validate.</param>
    public void NormalizeAndValidate(RuntimePluginDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        definition.Name = BoundRequired(definition.Name, 200, "Plugin name");
        definition.FunctionName = BoundRequired(definition.FunctionName, 120, "Function name").ToLowerInvariant();
        if (!definition.FunctionName.StartsWith("plugin.", StringComparison.Ordinal) || definition.FunctionName.Any(ch => !(char.IsLower(ch) || char.IsDigit(ch) || ch is '.' or '-' or '_')))
            throw new ArgumentException("Runtime extension function names must use the plugin.* namespace and lowercase letters, digits, dot, dash, or underscore.");
        definition.Purpose = BoundRequired(definition.Purpose, 2000, "Purpose");
        definition.SafetyNotes = Bound(definition.SafetyNotes, 2000);
        definition.ParameterSchemaJson = string.IsNullOrWhiteSpace(definition.ParameterSchemaJson) ? "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":true}" : definition.ParameterSchemaJson.Trim();
        using var schema = JsonDocument.Parse(definition.ParameterSchemaJson);
        if (schema.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Parameter schema must be a JSON object.");
        if (definition.Kind is RuntimePluginKind.CSharpScript or RuntimePluginKind.JavaScript && string.IsNullOrWhiteSpace(definition.SourceCode))
            throw new ArgumentException("Script source is required.");
        definition.EntryAssemblyName = Bound(definition.EntryAssemblyName, 260);
        definition.EntryTypeName = Bound(definition.EntryTypeName, 500);
    }

    /// <summary>Computes the deterministic content hash used to invalidate previously loaded executable material.</summary>
    /// <param name="definition">Definition whose executable content is hashed.</param>
    /// <returns>Lowercase SHA-256 content hash.</returns>
    public string HashContent(RuntimePluginDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var value = $"{definition.Kind}\n{definition.SourceCode}\n{definition.PackagePayloadBase64}\n{definition.EntryAssemblyName}\n{definition.EntryTypeName}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    /// <summary>Creates a detached copy suitable for runtime caches and UI editing.</summary>
    /// <param name="source">Source definition.</param>
    /// <returns>Detached copy.</returns>
    public RuntimePluginDefinition Clone(RuntimePluginDefinition source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var clone = new RuntimePluginDefinition();
        CopyEditable(source, clone);
        clone.Id = source.Id;
        clone.CreatedAtUtc = source.CreatedAtUtc;
        clone.UpdatedAtUtc = source.UpdatedAtUtc;
        clone.ContentHash = source.ContentHash;
        clone.LastBuildStatus = source.LastBuildStatus;
        clone.LastBuildMessage = source.LastBuildMessage;
        clone.LastLoadedAtUtc = source.LastLoadedAtUtc;
        return clone;
    }

    /// <summary>Copies user-editable runtime-extension values into an existing persistence row.</summary>
    /// <param name="source">Source values.</param>
    /// <param name="target">Persistence target.</param>
    public void CopyEditable(RuntimePluginDefinition source, RuntimePluginDefinition target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        target.Name = source.Name;
        target.FunctionName = source.FunctionName;
        target.Purpose = source.Purpose;
        target.SafetyNotes = source.SafetyNotes;
        target.ParameterSchemaJson = source.ParameterSchemaJson;
        target.Kind = source.Kind;
        target.SourceCode = source.SourceCode;
        target.PackagePayloadBase64 = source.PackagePayloadBase64;
        target.EntryAssemblyName = source.EntryAssemblyName;
        target.EntryTypeName = source.EntryTypeName;
        target.CompilerInstallationId = source.CompilerInstallationId;
        target.IsEnabled = source.IsEnabled;
        target.AvailableToAi = source.AvailableToAi;
        target.IsReadOnly = source.IsReadOnly;
        target.RequiresHumanConfirmation = source.RequiresHumanConfirmation;
        target.SupportsAutomaticInvocation = source.SupportsAutomaticInvocation;
    }

    /// <summary>Formats plain runtime-extension source as one HTML code block for the DevExpress HTML editor.</summary>
    /// <param name="source">Plain C# or JavaScript source.</param>
    /// <returns>HTML markup whose code text is safely encoded and whitespace-preserving.</returns>
    public string ToEditorMarkup(string? source)
    {
        var encoded = WebUtility.HtmlEncode(source ?? string.Empty);
        return $"<pre class=\"localgpt-runtime-source\"><code>{encoded}</code></pre>";
    }

    /// <summary>Extracts plain executable source from DevExpress HTML-editor markup without persisting formatting tags.</summary>
    /// <param name="markup">HTML markup produced by the runtime-extension editor.</param>
    /// <returns>Decoded plain source suitable for compilation or JavaScript execution.</returns>
    public string FromEditorMarkup(string? markup)
    {
        if (string.IsNullOrWhiteSpace(markup))
            return string.Empty;

        var normalized = markup.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        normalized = Regex.Replace(normalized, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"</(?:p|div|pre|li|h[1-6])\s*>", "\n", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"<[^>]+>", string.Empty, RegexOptions.CultureInvariant);
        normalized = WebUtility.HtmlDecode(normalized).Replace('\u00a0', ' ');
        if (normalized.EndsWith('\n'))
            normalized = normalized[..^1];
        return normalized;
    }

    /// <summary>Bounds a persisted string without changing its semantic content.</summary>
    /// <param name="value">Optional source value.</param>
    /// <param name="maximum">Maximum retained character count.</param>
    /// <returns>Bounded string.</returns>
    public string Bound(string? value, int maximum)
    {
        var normalized = value ?? string.Empty;
        return normalized.Length <= maximum ? normalized : normalized[..maximum];
    }

    private string BoundRequired(string? value, int maximum, string label)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException($"{label} is required.");
        return Bound(normalized, maximum);
    }
}
