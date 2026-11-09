using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Applies the 1-Wire traffic payload mode: Full keeps the decrypted payload, Redacted masks the values of secret-like JSON
/// properties (nested objects and arrays included), None keeps nothing. Payloads that are not JSON are kept as text in Full
/// mode and replaced by a size note in Redacted mode, because their secrets cannot be located.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWireTrafficPayloadFormatter(ILogger<OneWireTrafficPayloadFormatter> logger) : IOneWireTrafficPayloadFormatter
{
    /// <summary>Secret-like property names masked in Redacted mode when the options name none.</summary>
    private const string DefaultRedactedPropertyNames = "secret,password,passphrase,token,otp,totp,privatekey,sharedkey,pairingcode,apikey,credential,cookie,authorization";

    /// <summary>Replacement written for every masked value.</summary>
    private const string Mask = "***";

    /// <inheritdoc />
    public string Format(string plaintextJson, OneWireTrafficOptions options)
    {
        try
        {
            if (string.IsNullOrEmpty(plaintextJson) ||
                string.Equals(options.PayloadMode, OneWireTrafficPayloadModes.None, StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            if (!string.Equals(options.PayloadMode, OneWireTrafficPayloadModes.Redacted, StringComparison.OrdinalIgnoreCase))
                return plaintextJson;

            var names = options.RedactedPropertyNames is { Length: > 0 }
                ? options.RedactedPropertyNames
                : DefaultRedactedPropertyNames.Split(',');
            JsonNode? root;
            try
            {
                root = JsonNode.Parse(plaintextJson);
            }
            catch (JsonException exception)
            {
                logger.LogDebug(exception, "A 1-Wire traffic payload is not JSON; Redacted mode replaces it by its size.");
                return $"[non-JSON payload of {plaintextJson.Length} characters withheld in Redacted mode]";
            }

            if (root is null)
                return plaintextJson;
            MaskSecrets(root, names);
            return root.ToJsonString();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting a 1-Wire traffic payload failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public string Bound(string text, int maximumCharacters)
    {
        try
        {
            if (maximumCharacters <= 0 || string.IsNullOrEmpty(text) || text.Length <= maximumCharacters)
                return text ?? string.Empty;
            return $"{text[..maximumCharacters]}… [{text.Length - maximumCharacters} more characters]";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bounding 1-Wire traffic text failed.");
            throw;
        }
    }

    /// <summary>Masks the values of secret-like properties anywhere in a JSON tree.</summary>
    /// <param name="node">The JSON node to walk.</param>
    /// <param name="names">Secret-like property name fragments.</param>
    private void MaskSecrets(JsonNode node, IReadOnlyList<string> names)
    {
        try
        {
            if (node is JsonObject jsonObject)
            {
                foreach (var property in jsonObject.ToList())
                {
                    if (names.Any(name => !string.IsNullOrWhiteSpace(name) &&
                                          property.Key.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase)))
                        jsonObject[property.Key] = Mask;
                    else if (property.Value is not null)
                        MaskSecrets(property.Value, names);
                }
            }
            else if (node is JsonArray jsonArray)
            {
                foreach (var item in jsonArray)
                {
                    if (item is not null)
                        MaskSecrets(item, names);
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Masking secrets in a 1-Wire traffic payload failed.");
            throw;
        }
    }
}
