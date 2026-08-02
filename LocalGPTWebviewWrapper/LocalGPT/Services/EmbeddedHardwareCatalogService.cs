using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class EmbeddedHardwareCatalogService(
    IWebHostEnvironment environment,
    ILogger<EmbeddedHardwareCatalogService> logger) : IEmbeddedHardwareCatalogService
{
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<EmbeddedBoardCatalog> GetCatalogAsync(CancellationToken cancellationToken = default) => new()
    {
        Boards = [.. await GetBoardProfilesAsync(cancellationToken).ConfigureAwait(false)],
        Protocols = [.. await GetProtocolDescriptorsAsync(cancellationToken).ConfigureAwait(false)],
        PublisherWorkbench = GetPublisherWorkbenchContract()
    };

    public async Task<IReadOnlyList<EmbeddedBoardProfile>> GetBoardProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = new Dictionary<string, EmbeddedBoardProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in GetProfileDirectories())
        {
            if (!Directory.Exists(directory))
                continue;

            foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                         .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    var profile = await JsonSerializer.DeserializeAsync<EmbeddedBoardProfile>(stream, jsonOptions, cancellationToken).ConfigureAwait(false);
                    if (profile is null || string.IsNullOrWhiteSpace(profile.Key))
                    {
                        logger.LogWarning("Ignored invalid embedded board profile {ProfileFile}.", Path.GetFileName(path));
                        continue;
                    }
                    Normalize(profile);
                    profiles[profile.Key] = profile;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
                {
                    logger.LogWarning(exception, "Could not read embedded board profile {ProfileFile}.", Path.GetFileName(path));
                }
            }
        }

        if (profiles.Count == 0)
        {
            foreach (var fallback in CreateFallbackProfiles())
                profiles[fallback.Key] = fallback;
        }

        var result = profiles.Values.OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        logger.LogInformation("Loaded {EmbeddedBoardProfileCount} embedded board profile(s).", result.Count);
        return result;
    }

    public async Task<EmbeddedBoardProfile?> GetBoardProfileAsync(string boardProfileKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(boardProfileKey))
            return null;
        var profiles = await GetBoardProfilesAsync(cancellationToken).ConfigureAwait(false);
        return profiles.FirstOrDefault(item => string.Equals(item.Key, boardProfileKey.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public Task<IReadOnlyList<EmbeddedProtocolDescriptor>> GetProtocolDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<EmbeddedProtocolDescriptor> result =
        [
            Protocol(EmbeddedProtocolKeys.DigitalGpio, "Digital GPIO", "Physical", "Read or drive a bounded digital signal.", ["signal"], false, false, false, true, "Check direction, logic voltage, pull-up/down and boot-pin constraints."),
            Protocol(EmbeddedProtocolKeys.AnalogAdc, "Analog ADC", "Physical", "Read an analog sensor value such as a capacitive moisture sensor.", ["signal"], false, false, false, true, "The input voltage must stay inside the selected board ADC range; calibration is sensor- and board-specific."),
            Protocol(EmbeddedProtocolKeys.Pwm, "PWM", "Physical", "Drive a duty-cycle signal for LEDs, fans or approved interfaces.", ["signal"], false, false, false, false, "Do not drive motors, relays or high-current loads directly from a GPIO."),
            Protocol(EmbeddedProtocolKeys.PhysicalOneWire, "Physical 1-Wire bus", "Physical", "Attach Dallas/Maxim-style addressable sensors such as DS18B20 devices.", ["data", "ground", "supply"], true, true, false, true, "Use the correct pull-up and wiring topology. This bus is distinct from LocalGPT's logical 1-Wire envelope."),
            Protocol(EmbeddedProtocolKeys.I2c, "I²C", "Physical", "Share SDA/SCL between addressed sensors and devices.", ["sda", "scl", "ground"], true, true, false, false, "Validate pull-ups, address conflicts and board-specific default pins."),
            Protocol(EmbeddedProtocolKeys.Spi, "SPI", "Physical", "Connect clock/data plus one chip-select per peripheral.", ["sck", "mosi", "miso", "cs", "ground"], true, true, false, false, "Validate voltage, bus mode, frequency and chip-select ownership."),
            Protocol(EmbeddedProtocolKeys.Uart, "UART", "Physical", "Exchange serial bytes through TX/RX.", ["tx", "rx", "ground"], false, true, false, true, "Cross TX/RX and verify both sides use compatible voltage and baud settings."),
            Protocol(EmbeddedProtocolKeys.Can, "CAN", "Physical", "Use a differential CAN bus through an approved transceiver.", ["tx", "rx", "can-high", "can-low"], true, true, false, false, "A GPIO-only connection is not CAN; termination and transceiver requirements remain mandatory."),
            Protocol(EmbeddedProtocolKeys.Rs485, "RS-485", "Physical", "Use a differential serial bus through an approved transceiver.", ["tx", "rx", "direction", "a", "b"], true, true, false, false, "A transceiver, termination and direction control are required."),
            Protocol(EmbeddedProtocolKeys.SerialJsonLines, "Serial JSON Lines", "Edge transport", "Send compact device telemetry to a trusted local gateway.", ["tx", "rx", "ground"], false, true, true, true, "The device emits an edge packet; the gateway validates it before creating a LocalGPT envelope."),
            Protocol(EmbeddedProtocolKeys.HttpJson, "HTTP JSON", "Edge transport", "Post a validated payload through a local network adapter.", ["network"], false, true, true, false, "Do not expose an unauthenticated device endpoint to the LAN or Internet."),
            Protocol(EmbeddedProtocolKeys.Mqtt, "MQTT", "Edge transport", "Publish readings through an explicitly configured local broker.", ["network", "topic"], true, true, true, false, "Broker trust, topic ACLs and retained-message behavior require explicit configuration."),
            Protocol(EmbeddedProtocolKeys.LocalGptOneWire, "LocalGPT logical 1-Wire envelope", "Application", "Route a validated capability invocation through LocalGPT's protected peer protocol.", ["controller", "method", "capability"], true, false, true, false, "Embedded firmware should normally use a small edge packet and let a trusted gateway apply LocalGPT security."),
            Protocol(EmbeddedProtocolKeys.OrganicPeer, "LocalGPT organic peer", "Application", "Expose a richer external workbench, simulator or hardware controller as a trusted organic capability peer.", ["peer", "capability"], true, false, true, false, "Every side-effecting capability remains subject to peer policy and fresh approval."),
            Protocol(EmbeddedProtocolKeys.Custom, "Custom protocol", "Custom", "Describe a user-owned protocol without forcing it into a predefined bus.", [], true, false, true, false, "The Council must document framing, electrical layer, validation, trust boundary and failure behavior.")
        ];
        return Task.FromResult(result);
    }

    public EmbeddedPublisherWorkbenchContract GetPublisherWorkbenchContract() => new();

    private IEnumerable<string> GetProfileDirectories()
    {
        var contentDirectory = Path.Combine(environment.ContentRootPath, "Configuration", "EmbeddedBoards");
        yield return contentDirectory;
        var outputDirectory = Path.Combine(AppContext.BaseDirectory, "Configuration", "EmbeddedBoards");
        if (!string.Equals(contentDirectory, outputDirectory, StringComparison.OrdinalIgnoreCase))
            yield return outputDirectory;
    }

    private void Normalize(EmbeddedBoardProfile profile)
    {
        profile.Key = profile.Key.Trim().ToLowerInvariant();
        profile.DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.Key : profile.DisplayName.Trim();
        profile.Family = profile.Family?.Trim() ?? string.Empty;
        profile.Framework = string.IsNullOrWhiteSpace(profile.Framework) ? "Arduino" : profile.Framework.Trim();
        profile.PlatformIoBoard = profile.PlatformIoBoard?.Trim() ?? string.Empty;
        profile.DocumentationSource = profile.DocumentationSource?.Trim() ?? string.Empty;
        profile.Status = string.IsNullOrWhiteSpace(profile.Status) ? "NeedsBoardReview" : profile.Status.Trim();
        profile.SupportedProtocols = NormalizeList(profile.SupportedProtocols);
        profile.Notes = NormalizeList(profile.Notes);
        profile.Pins = (profile.Pins ?? [])
            .Where(item => item is not null && !string.IsNullOrWhiteSpace(item.PinKey))
            .GroupBy(item => item.PinKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.CanvasY)
            .ThenBy(item => item.CanvasX)
            .ThenBy(item => item.PinKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var pin in profile.Pins)
        {
            pin.PinKey = pin.PinKey.Trim();
            pin.Label = string.IsNullOrWhiteSpace(pin.Label) ? pin.PinKey : pin.Label.Trim();
            pin.Capabilities = NormalizeList(pin.Capabilities);
            pin.Warning = pin.Warning?.Trim() ?? string.Empty;
        }
    }

    private List<string> NormalizeList(IEnumerable<string>? values) => (values ?? [])
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private EmbeddedProtocolDescriptor Protocol(
        string key,
        string displayName,
        string layer,
        string purpose,
        List<string> requiredRoles,
        bool shared,
        bool externalHardware,
        bool gateway,
        bool generated,
        string safetyNote) => new()
    {
        Key = key,
        DisplayName = displayName,
        Layer = layer,
        Purpose = purpose,
        RequiredRoles = requiredRoles,
        SupportsSharedBus = shared,
        RequiresExternalHardware = externalHardware,
        RequiresGateway = gateway,
        SupportedByGeneratedSketch = generated,
        SafetyNote = safetyNote
    };

    private IEnumerable<EmbeddedBoardProfile> CreateFallbackProfiles()
    {
        yield return new EmbeddedBoardProfile
        {
            Key = "esp32-family-generic",
            DisplayName = "ESP32 family (generic review profile)",
            Family = "ESP32",
            Framework = "Arduino",
            PlatformIoBoard = "esp32dev",
            LogicVoltage = 3.3,
            Status = "NeedsBoardReview",
            SupportedProtocols = [EmbeddedProtocolKeys.DigitalGpio, EmbeddedProtocolKeys.AnalogAdc, EmbeddedProtocolKeys.Pwm, EmbeddedProtocolKeys.PhysicalOneWire, EmbeddedProtocolKeys.I2c, EmbeddedProtocolKeys.Spi, EmbeddedProtocolKeys.Uart, EmbeddedProtocolKeys.SerialJsonLines],
            Notes = ["Fallback profile only. Select or import the exact board profile before compile or flash."]
        };
        yield return new EmbeddedBoardProfile
        {
            Key = "arduino-family-generic",
            DisplayName = "Arduino-compatible board (generic review profile)",
            Family = "Arduino",
            Framework = "Arduino",
            LogicVoltage = 5.0,
            Status = "NeedsBoardReview",
            SupportedProtocols = [EmbeddedProtocolKeys.DigitalGpio, EmbeddedProtocolKeys.AnalogAdc, EmbeddedProtocolKeys.Pwm, EmbeddedProtocolKeys.PhysicalOneWire, EmbeddedProtocolKeys.I2c, EmbeddedProtocolKeys.Spi, EmbeddedProtocolKeys.Uart, EmbeddedProtocolKeys.SerialJsonLines],
            Notes = ["Fallback profile only. Operating voltage and pin capabilities vary by board."]
        };
    }
}
