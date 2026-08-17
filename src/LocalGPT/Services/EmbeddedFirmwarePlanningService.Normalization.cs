using System.IO.Compression;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates embedded firmware planning behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class EmbeddedFirmwarePlanningService
    {
    /// <summary>
    /// Normalizes mode as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="mode">Mode value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <param name="protocolKey">Protocol key value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeMode(string? mode, string protocolKey)
    {
    try
    {
            var value = Text(mode, "Input", 80);
            if (protocolKey == EmbeddedProtocolKeys.AnalogAdc || value.Contains("analog", StringComparison.OrdinalIgnoreCase)) return "AnalogInput";
            if (protocolKey == EmbeddedProtocolKeys.PhysicalOneWire || value.Contains("onewire", StringComparison.OrdinalIgnoreCase) || value.Contains("1-wire", StringComparison.OrdinalIgnoreCase)) return "PhysicalOneWire";
            if (value.Contains("output", StringComparison.OrdinalIgnoreCase) || value.Contains("drive", StringComparison.OrdinalIgnoreCase)) return "Output";
            if (value.Contains("pullup", StringComparison.OrdinalIgnoreCase)) return "InputPullup";
            return "Input";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(NormalizeMode)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(NormalizeMode)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes protocol as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="protocolKey">Protocol key value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <param name="hint">Hint value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeProtocol(string? protocolKey, string? hint)
    {
    try
    {
            if (!string.IsNullOrWhiteSpace(protocolKey))
            {
                var value = protocolKey.Trim().ToLowerInvariant();
                if (value is not "analog" and not "digital" and not "onewire" and not "1-wire") return value;
            }
            var text = $"{protocolKey} {hint}";
            if ((text.Contains("physical", StringComparison.OrdinalIgnoreCase) && text.Contains("wire", StringComparison.OrdinalIgnoreCase)) || text.Contains("ds18", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.PhysicalOneWire;
            if (text.Contains("onewire", StringComparison.OrdinalIgnoreCase) || text.Contains("1-wire", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.PhysicalOneWire;
            if (text.Contains("analog", StringComparison.OrdinalIgnoreCase) || text.Contains("adc", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.AnalogAdc;
            if (text.Contains("pwm", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.Pwm;
            if (text.Contains("i2c", StringComparison.OrdinalIgnoreCase) || text.Contains("i²c", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.I2c;
            if (text.Contains("spi", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.Spi;
            if (text.Contains("uart", StringComparison.OrdinalIgnoreCase) || text.Contains("serial", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.Uart;
            return EmbeddedProtocolKeys.DigitalGpio;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(NormalizeProtocol)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(NormalizeProtocol)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes transport as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeTransport(string? value)
    {
    try
    {
            var transport = Text(value, EmbeddedProtocolKeys.SerialJsonLines, 120).ToLowerInvariant();
            return transport switch
            {
                "serialjsonlines" or "serial-json-lines" or "serial" => EmbeddedProtocolKeys.SerialJsonLines,
                "httpjson" or "http-json" => EmbeddedProtocolKeys.HttpJson,
                "mqtt" => EmbeddedProtocolKeys.Mqtt,
                "onewire" or "localgpt-onewire" => EmbeddedProtocolKeys.LocalGptOneWire,
                _ => transport
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(NormalizeTransport)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(NormalizeTransport)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves pin key as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <param name="gpio">Gpio value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolvePinKey(EmbeddedBoardProfile? profile, int gpio) {
    try
    {
        return profile?.Pins.FirstOrDefault(item => item.Gpio == gpio)?.PinKey ?? $"GPIO{gpio}";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(ResolvePinKey)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(ResolvePinKey)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs infer metric as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sensorType">Sensor type value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string InferMetric(string? sensorType) {
    try
    {
        return sensorType?.Contains("moist", StringComparison.OrdinalIgnoreCase) == true ? "moisture" : sensorType?.Contains("temp", StringComparison.OrdinalIgnoreCase) == true ? "temperature" : "reading";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(InferMetric)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(InferMetric)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs default unit as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sensorType">Sensor type value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DefaultUnit(string? sensorType) {
    try
    {
        return sensorType?.Contains("temp", StringComparison.OrdinalIgnoreCase) == true ? "celsius" : sensorType?.Contains("moist", StringComparison.OrdinalIgnoreCase) == true ? "raw_adc" : "raw";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(DefaultUnit)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(DefaultUnit)} failed.");
        throw;
    }
}
    /// <summary>
    /// Determines whether esp32 adc2 pin as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="gpio">Gpio value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsEsp32Adc2Pin(int gpio) {
    try
    {
        return gpio is 0 or 2 or 4 or 12 or 13 or 14 or 15 or 25 or 26 or 27;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(IsEsp32Adc2Pin)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(IsEsp32Adc2Pin)} failed.");
        throw;
    }
}
    /// <summary>
    /// Determines whether output mode as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="mode">Mode value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsOutputMode(string? mode) {
    try
    {
        return (mode ?? string.Empty).Contains("Output", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(IsOutputMode)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(IsOutputMode)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs arduino pin mode as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="mode">Mode value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ArduinoPinMode(string mode) {
    try
    {
        return mode == "Output" ? "OUTPUT" : mode == "InputPullup" || mode == "PhysicalOneWire" ? "INPUT_PULLUP" : "INPUT";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(ArduinoPinMode)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(ArduinoPinMode)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs matches pin as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="finding">Finding value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <param name="assignment">Assignment value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool MatchesPin(EmbeddedPlanFinding finding, EmbeddedPinAssignment assignment) {
    try
    {
        return finding.Gpio == assignment.Gpio || (!string.IsNullOrWhiteSpace(finding.PinKey) && string.Equals(finding.PinKey, assignment.PinKey, StringComparison.OrdinalIgnoreCase));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(MatchesPin)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(MatchesPin)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs severity status as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="findings">Embedded plan finding dependency used by the embedded firmware planning workflow to provide the corresponding application capability.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SeverityStatus(IEnumerable<EmbeddedPlanFinding> findings) {
    try
    {
        return findings.Any(item => string.Equals(item.Severity, "Danger", StringComparison.OrdinalIgnoreCase)) ? "Danger" : findings.Any(item => string.Equals(item.Severity, "Warning", StringComparison.OrdinalIgnoreCase)) ? "Warning" : "Approved";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(SeverityStatus)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(SeverityStatus)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs sanitize identifier as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <param name="index">Index value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SanitizeIdentifier(string value, int index)
    {
    try
    {
            var chars = value.Select(ch => char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_').ToArray();
            var result = new string(chars).Trim('_');
            return string.IsNullOrWhiteSpace(result) ? $"PIN_{index}" : result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(SanitizeIdentifier)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(SanitizeIdentifier)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs escape cpp as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string EscapeCpp(string value) {
    try
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(EscapeCpp)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(EscapeCpp)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs escape ini as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string EscapeIni(string value) {
    try
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(EscapeIni)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(EscapeIni)} failed.");
        throw;
    }
}
    /// <summary>
    /// Normalizes JSON as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeJson(string? value)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(value)) return "{}";
            try { using var document = JsonDocument.Parse(value); return document.RootElement.GetRawText(); }
            catch (JsonException) { return value.Trim(); }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(NormalizeJson)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(NormalizeJson)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs text as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Text(string? value, string fallback, int maximum)
    {
    try
    {
            var result = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            return result.Length <= maximum ? result : result[..maximum];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(Text)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(Text)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs safe file name as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SafeFileName(string value)
    {
    try
    {
            var invalid = Path.GetInvalidFileNameChars().ToHashSet();
            var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? "embedded-node" : cleaned;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(SafeFileName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(SafeFileName)} failed.");
        throw;
    }
}

    }
}
