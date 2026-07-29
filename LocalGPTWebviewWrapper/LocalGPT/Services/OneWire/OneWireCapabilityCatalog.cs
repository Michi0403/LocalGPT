using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using System.Text.Json;

namespace LocalGPT.Services.OneWire;

public sealed class OneWireCapabilityCatalog(
    IServiceScopeFactory scopeFactory,
    IOneWirePeerRegistry peers,
    IOneWireConnectionRegistry connections,
    IHardwareInventoryService hardwareInventory,
    ILogger<OneWireCapabilityCatalog> logger) : IOneWireCapabilityCatalog
{
    public Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetLocalCapabilitiesAsync(CancellationToken cancellationToken = default) =>
        BuildCapabilitiesAsync(peerId: null, cancellationToken);

    Task<IReadOnlyList<OneWireCapabilityDescriptor>> IOneWireCapabilityProvider.GetCapabilitiesAsync(CancellationToken cancellationToken) =>
        GetLocalCapabilitiesAsync(cancellationToken);

    Task<IReadOnlyList<OneWireSkillDescriptor>> IOneWireCapabilityProvider.GetSkillsAsync(CancellationToken cancellationToken) =>
        GetLocalSkillsAsync(cancellationToken);

    Task<IReadOnlyList<OneWireUiFeatureDescriptor>> IOneWireCapabilityProvider.GetUiFeaturesAsync(CancellationToken cancellationToken) =>
        GetLocalUiFeaturesAsync(cancellationToken);

    Task<IReadOnlyList<OneWireHardwareDescriptor>> IOneWireCapabilityProvider.GetHardwareAsync(CancellationToken cancellationToken) =>
        GetLocalHardwareAsync(cancellationToken);

    public Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetLocalCapabilitiesForPeerAsync(string peerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(peerId);
        return BuildCapabilitiesAsync(peerId, cancellationToken);
    }

    private async Task<IReadOnlyList<OneWireCapabilityDescriptor>> BuildCapabilitiesAsync(string? peerId, CancellationToken cancellationToken)
    {
        await using var functionScope = scopeFactory.CreateAsyncScope();
        var functionCatalog = functionScope.ServiceProvider.GetRequiredService<IDxAiFunctionCatalogService>();
        var entries = string.IsNullOrWhiteSpace(peerId)
            ? await functionCatalog.GetEntriesAsync(cancellationToken).ConfigureAwait(false)
            : await functionCatalog.GetExposedToPeerAsync(peerId, cancellationToken).ConfigureAwait(false);
        var functions = entries
            .Where(entry => entry.Kind == DxAiFunctionCatalogKinds.DxFunction && entry.IsAvailable && entry.IsEnabled &&
                (string.IsNullOrWhiteSpace(peerId) || entry.ExposeToOneWire))
            .Select(entry => new OneWireCapabilityDescriptor
            {
                Key = entry.FunctionName,
                DisplayName = entry.DisplayName,
                Description = entry.Purpose,
                Controller = "DxAiFunctions",
                Method = entry.Method,
                Route = entry.Route,
                ParameterSchemaJson = entry.ParameterSchemaJson,
                Organs = InferOrgans(entry.FunctionName, entry.Purpose),
                Skills = InferSkills(entry.FunctionName, entry.Purpose),
                RequiredSkillKeys = InferSkills(entry.FunctionName, entry.Purpose),
                UiActivationKeys = [$"localgpt.dxfunction.{entry.FunctionName}"],
                IsOnline = entry.IsAvailable,
                IsEnabled = entry.IsEnabled,
                IsReadOnly = entry.IsReadOnly,
                RequiresHumanConfirmation = entry.RequiresFrontendConfirmation,
                RequiresHumanInteractionOnTargetSystem = entry.RequiresFrontendConfirmation,
                RequiresFrontendUserConfirmation = entry.RequiresFrontendConfirmation,
                SupportsScheduling = entry.AllowRemoteInvocation,
                SupportsRecurringExecution = entry.AllowRemoteInvocation && entry.IsReadOnly,
                IsExposedToPeer = entry.ExposeToOneWire,
                AllowPeerInvocation = entry.AllowRemoteInvocation,
                InteractionEditor = entry.InteractionEditor,
                ConfigurationKey = entry.CatalogKey,
                Source = entry.Source
            }).ToList();

        await using var scope = scopeFactory.CreateAsyncScope();
        var councilBlueprints = scope.ServiceProvider.GetRequiredService<IOrganicCouncilBlueprintService>();
        functions.Add(new OneWireCapabilityDescriptor
        {
            Key = "council.run", DisplayName = "Run AI Council",
            Description = "Starts a LocalGPT Council heartbeat after the receiving LocalGPT frontend confirms the exact request.",
            Controller = "OneWire", Method = "POST", Route = "/api/onewire/council", Organs = ["brain"],
            Skills = (await councilBlueprints.GetTeamsAsync(cancellationToken).ConfigureAwait(false)).Select(team => team.Key).ToList(),
            RequiredSkillKeys = ["council", "planning"], UiActivationKeys = ["localgpt.council.run", "publisherstudio.council.run"],
            IsReadOnly = false, RequiresHumanConfirmation = true, RequiresHumanInteractionOnTargetSystem = true,
            RequiresFrontendUserConfirmation = true, SupportsScheduling = true, IsExposedToPeer = true, AllowPeerInvocation = true,
            InteractionEditor = OneWireInteractionEditor.RichText, ConfigurationKey = "builtin:council.run", Source = "LocalGPT"
        });
        functions.Add(new OneWireCapabilityDescriptor
        {
            Key = "council.teams", DisplayName = "Council team catalog",
            Description = "Lists configured Council teams.", Controller = "OneWire", Method = "GET", Route = "/api/onewire/council/teams",
            Organs = ["brain"], Skills = ["planning", "role-routing"], RequiredSkillKeys = ["council"],
            UiActivationKeys = ["publisherstudio.council.team-picker"], IsReadOnly = true, RequiresHumanConfirmation = false,
            RequiresFrontendUserConfirmation = false, IsExposedToPeer = true, AllowPeerInvocation = true,
            InteractionEditor = OneWireInteractionEditor.None, ConfigurationKey = "builtin:council.teams", Source = "LocalGPT"
        });
        functions.Add(new OneWireCapabilityDescriptor
        {
            Key = "localgpt.screenreader.help", DisplayName = "Recurring screen-reader help",
            Description = "Processes debounced PublisherStudio screenshot evidence and returns bounded LocalGPT guidance.",
            Controller = "OneWire", Method = "POST", Route = "/api/onewire/screenreader/help", Organs = ["eyes", "brain"],
            Skills = ["vision", "screenreader", "council"], RequiredSkillKeys = ["vision"], UiActivationKeys = ["publisherstudio.screenreader.recurring"],
            IsReadOnly = true, RequiresHumanConfirmation = false, RequiresAutomatedInteractionOnTargetSystem = true,
            SupportsScheduling = true, SupportsRecurringExecution = true, IsExposedToPeer = true, AllowPeerInvocation = true,
            RequiresFrontendUserConfirmation = false, InteractionEditor = OneWireInteractionEditor.None,
            InteractionValueSchemaJson = "{\"type\":\"object\",\"properties\":{\"prompt\":{\"type\":\"string\"},\"selector\":{\"type\":\"string\"},\"dataUrl\":{\"type\":\"string\"}}}",
            ConfigurationKey = "builtin:localgpt.screenreader.help", Source = "LocalGPT"
        });
        functions.Add(new OneWireCapabilityDescriptor
        {
            Key = "localgpt.vision.ocr", DisplayName = "Local vision OCR",
            Description = "Recognizes text in one explicitly approved image using a configured local Ollama-compatible OCR/vision model such as DeepSeek OCR.",
            Controller = "OneWire", Method = "POST", Route = "/api/onewire/http-json", Organs = ["eyes", "brain"],
            Skills = ["vision", "ocr", "text-recognition"], RequiredSkillKeys = ["vision", "ocr"],
            UiActivationKeys = ["publisherstudio.picture.ocr"], IsReadOnly = true, RequiresHumanConfirmation = true,
            RequiresHumanInteractionOnTargetSystem = true, RequiresFrontendUserConfirmation = true, IsExposedToPeer = true,
            AllowPeerInvocation = true, InteractionEditor = OneWireInteractionEditor.ConfirmationOnly,
            ParameterSchemaJson = "{\"type\":\"object\",\"required\":[\"imageDataUrl\"],\"properties\":{\"imageDataUrl\":{\"type\":\"string\"},\"prompt\":{\"type\":\"string\"},\"modelName\":{\"type\":\"string\"},\"maximumOutputTokens\":{\"type\":\"integer\",\"minimum\":128,\"maximum\":4096}}}",
            ConfigurationKey = "builtin:localgpt.vision.ocr", Source = "LocalGPT",
            InputContract = "A base64 image data URL rendered in the requesting frontend, plus an optional OCR prompt and configured model name.",
            OutputContract = "JSON containing recognized text, the model used, media type and NeedsHumanReview=true.",
            SecurityContract = "Both frontends must approve the current request. Image content is encrypted when an MFA-verified trusted link exists and is never accepted as a server file path.",
            OrganicUseCase = "Eyes organ for OCR in Picture Studio and other user-installed organic clients.",
            SuggestedCouncilRoles = ["OCR-capable vision member", "DeepSeek OCR", "evidence verification specialist"]
        });

        functions.Add(new OneWireCapabilityDescriptor
        {
            Key = "organic.skills.manage", DisplayName = "Maintain organic skills",
            Description = "Maintains user-approved organic skills and links them to projects and Council members.",
            Controller = "OneWire", Method = "POST", Route = "/api/onewire/skills", Organs = ["brain"],
            Skills = ["skills", "project-context", "member-routing"], RequiredSkillKeys = ["council", "organic-routing"],
            UiActivationKeys = ["localgpt.organic.skills"], IsReadOnly = false, RequiresHumanConfirmation = true,
            RequiresHumanInteractionOnTargetSystem = true, RequiresFrontendUserConfirmation = true, IsExposedToPeer = true,
            AllowPeerInvocation = true, InteractionEditor = OneWireInteractionEditor.Json, ConfigurationKey = "builtin:organic.skills.manage", Source = "LocalGPT"
        });

        foreach (var serviceEntry in entries.Where(entry => entry.Kind == DxAiFunctionCatalogKinds.PublicServiceMethod && entry.IsAvailable && entry.IsEnabled &&
            (string.IsNullOrWhiteSpace(peerId) || entry.ExposeToOneWire)))
        {
            functions.Add(new OneWireCapabilityDescriptor
            {
                Key = serviceEntry.FunctionName, DisplayName = serviceEntry.DisplayName, Description = serviceEntry.Purpose,
                Controller = "ConfiguredPublicService", Method = "POST", Route = "/api/dxai/public-service/invoke",
                ParameterSchemaJson = serviceEntry.ParameterSchemaJson, Organs = ["brain"], Skills = ["service-method"],
                RequiredSkillKeys = ["service-method"], UiActivationKeys = [$"localgpt.service.{serviceEntry.CatalogKey}"],
                IsOnline = serviceEntry.IsAvailable, IsEnabled = serviceEntry.IsEnabled, IsReadOnly = serviceEntry.IsReadOnly,
                RequiresHumanConfirmation = serviceEntry.RequiresFrontendConfirmation,
                RequiresHumanInteractionOnTargetSystem = serviceEntry.RequiresFrontendConfirmation,
                RequiresFrontendUserConfirmation = serviceEntry.RequiresFrontendConfirmation, IsExposedToPeer = serviceEntry.ExposeToOneWire,
                AllowPeerInvocation = serviceEntry.AllowRemoteInvocation, InteractionEditor = serviceEntry.InteractionEditor,
                ConfigurationKey = serviceEntry.CatalogKey, Source = serviceEntry.Source
            });
        }

        foreach (var capability in functions)
            PopulateTeaching(capability);

        logger.LogDebug("Published {CapabilityCount} LocalGPT 1-Wire capabilities for peer {PeerId}.", functions.Count, peerId ?? "local-ui");
        return functions.OrderBy(capability => capability.Key, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<IReadOnlyList<OneWireSkillDescriptor>> GetLocalSkillsAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var registry = scope.ServiceProvider.GetRequiredService<IOrganicSkillRegistryService>();
        var persisted = await registry.GetSkillsAsync(includeDisabled: true, cancellationToken).ConfigureAwait(false);
        var mapped = persisted.Select(skill => new OneWireSkillDescriptor
        {
            Key = skill.Key,
            DisplayName = skill.DisplayName,
            Description = skill.Description,
            SourcePeerId = skill.SourcePeerId,
            Organs = ParseList(skill.OrgansJson),
            CapabilityKeys = ParseList(skill.CapabilityKeysJson),
            UiActivationKeys = ParseList(skill.UiActivationKeysJson),
            IsOnline = skill.IsOnline,
            IsEnabled = skill.IsEnabled,
            UpdatedUtc = new DateTimeOffset(DateTime.SpecifyKind(skill.UpdatedAtUtc, DateTimeKind.Utc))
        }).ToList();

        foreach (var builtIn in BuiltInSkills())
        {
            if (mapped.All(item => !string.Equals(item.Key, builtIn.Key, StringComparison.OrdinalIgnoreCase)))
                mapped.Add(builtIn);
        }

        return mapped.OrderBy(skill => skill.Key, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public Task<IReadOnlyList<OneWireHardwareDescriptor>> GetLocalHardwareAsync(CancellationToken cancellationToken = default) =>
        hardwareInventory.GetHardwareAsync(cancellationToken);

    public async Task<IReadOnlyList<OneWireUiFeatureDescriptor>> GetLocalUiFeaturesAsync(CancellationToken cancellationToken = default)
    {
        var skills = await GetLocalSkillsAsync(cancellationToken).ConfigureAwait(false);
        var availableSkills = skills.Where(skill => skill.IsEnabled && skill.IsOnline).Select(skill => skill.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var connectedPeers = peers.GetPeers().Where(peer => connections.IsConnected(peer.PeerId)).ToList();

        return
        [
            CreateFeature("localgpt.organic.plugins", "Organic Plugins", connectedPeers.Count > 0, "No organic plugin system is connected."),
            CreateFeature("localgpt.learning.round", "Learning Round", true, string.Empty),
            CreateCapabilityFeature("localgpt.publisher.spreadsheet.request", "Request spreadsheet help", connectedPeers, availableSkills, ["publisher.spreadsheet.inspect"], ["spreadsheet"]),
            CreateCapabilityFeature("localgpt.publisher.screenreader", "Recurring screen reader", connectedPeers, availableSkills, ["publisher.screen.capture"], ["vision"]),
            CreateCapabilityFeature("localgpt.publisher.openscad", "OpenSCAD Team tools", connectedPeers, availableSkills, ["publisher.openscad.generate"], ["openscad"])
        ];
    }

    private static OneWireUiFeatureDescriptor CreateFeature(string key, string name, bool enabled, string disabledReason) => new()
    {
        Key = key,
        DisplayName = name,
        State = enabled ? OneWireUiFeatureState.Enabled : OneWireUiFeatureState.Hidden,
        Reason = enabled ? string.Empty : disabledReason
    };

    private static OneWireUiFeatureDescriptor CreateCapabilityFeature(
        string key,
        string name,
        IReadOnlyList<OneWirePeerAdvertisement> connectedPeers,
        IReadOnlySet<string> availableSkills,
        IReadOnlyList<string> requiredCapabilities,
        IReadOnlyList<string> requiredSkills)
    {
        var capabilityAvailable = connectedPeers.Any(peer => requiredCapabilities.All(required =>
            peer.Capabilities.Any(capability => capability.IsEnabled && capability.IsOnline && string.Equals(capability.Key, required, StringComparison.OrdinalIgnoreCase))));
        var skillsAvailable = requiredSkills.Count == 0 || requiredSkills.All(availableSkills.Contains) || connectedPeers.Any(peer =>
            requiredSkills.All(required => peer.Skills.Any(skill => skill.IsEnabled && skill.IsOnline && string.Equals(skill.Key, required, StringComparison.OrdinalIgnoreCase))));
        var enabled = capabilityAvailable && skillsAvailable;
        return new OneWireUiFeatureDescriptor
        {
            Key = key,
            DisplayName = name,
            State = enabled ? OneWireUiFeatureState.Enabled : OneWireUiFeatureState.Hidden,
            Reason = enabled ? string.Empty : "The connected organic plugin does not currently advertise every required capability/skill.",
            RequiredCapabilityKeys = requiredCapabilities.ToList(),
            RequiredSkillKeys = requiredSkills.ToList()
        };
    }

    private static IReadOnlyList<OneWireSkillDescriptor> BuiltInSkills() =>
    [
        new() { Key = "council", DisplayName = "AI Council", Description = "Council heartbeat planning and participation.", SourcePeerId = "localgpt", Organs = ["brain"], CapabilityKeys = ["council.run"], UiActivationKeys = ["localgpt.council.run"], IsOnline = true, IsEnabled = true },
        new() { Key = "spreadsheet", DisplayName = "Spreadsheet", Description = "Spreadsheet analysis and guided editing through an organic plugin.", SourcePeerId = "localgpt", Organs = ["brain", "eyes", "hands"], CapabilityKeys = ["publisher.spreadsheet.inspect"], UiActivationKeys = ["localgpt.publisher.spreadsheet.request"], IsOnline = true, IsEnabled = true },
        new() { Key = "openscad", DisplayName = "OpenSCAD", Description = "OpenSCAD project planning and canonical shape generation.", SourcePeerId = "localgpt", Organs = ["brain", "eyes", "hands"], CapabilityKeys = ["publisher.openscad.generate"], UiActivationKeys = ["localgpt.publisher.openscad"], IsOnline = true, IsEnabled = true },
        new() { Key = "vision", DisplayName = "Vision", Description = "Screen capture, recurring screen reading and visual evidence.", SourcePeerId = "localgpt", Organs = ["eyes"], CapabilityKeys = ["publisher.screen.capture", "publisher.screenreader.start"], UiActivationKeys = ["localgpt.publisher.screenreader"], IsOnline = true, IsEnabled = true },
        new() { Key = "organic-routing", DisplayName = "Organic routing", Description = "Maps project/member skills to DX functions, controllers and external capabilities.", SourcePeerId = "localgpt", Organs = ["brain"], CapabilityKeys = ["organic.skills.manage"], UiActivationKeys = ["localgpt.organic.skills"], IsOnline = true, IsEnabled = true },
        new() { Key = "learning", DisplayName = "Learning Round", Description = "Studies bounded chat memory, logs, knowledge and database regexes and stores untrusted reusable evidence.", SourcePeerId = "localgpt", Organs = ["brain"], CapabilityKeys = ["localgpt.learning.snapshot", "localgpt.learning.maintain", "localgpt.regex.list", "localgpt.regex.test", "localgpt.regex.upsert"], UiActivationKeys = ["localgpt.learning.round"], IsOnline = true, IsEnabled = true }
    ];

    private static List<string> ParseList(string? json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json ?? "[]") ?? []; }
        catch (JsonException) { return []; }
    }

    private static List<string> InferOrgans(string name, string purpose)
    {
        var text = $"{name} {purpose}";
        var result = new List<string> { "brain" };
        if (text.Contains("screen", StringComparison.OrdinalIgnoreCase) || text.Contains("image", StringComparison.OrdinalIgnoreCase)) result.Add("eyes");
        if (text.Contains("mouse", StringComparison.OrdinalIgnoreCase) || text.Contains("keyboard", StringComparison.OrdinalIgnoreCase) || text.Contains("write", StringComparison.OrdinalIgnoreCase)) result.Add("hands");
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> InferSkills(string name, string purpose)
    {
        var tokens = $"{name} {purpose}".Split([' ', '.', '-', '_', '/', ':'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.Where(token => token.Length >= 4).Select(token => token.ToLowerInvariant()).Distinct().Take(12).ToList();
    }

    private static void PopulateTeaching(OneWireCapabilityDescriptor capability)
    {
        capability.InputContract = string.IsNullOrWhiteSpace(capability.InputContract)
            ? $"Parameters matching this JSON schema: {capability.ParameterSchemaJson}"
            : capability.InputContract;
        capability.OutputContract = string.IsNullOrWhiteSpace(capability.OutputContract)
            ? "A bounded JSON WorkResult associated with the original CorrelationId."
            : capability.OutputContract;
        capability.SecurityContract = string.IsNullOrWhiteSpace(capability.SecurityContract)
            ? capability.RequiresFrontendUserConfirmation || capability.RequiresHumanConfirmation
                ? "The receiving frontend is authoritative and must approve the exact current request; reusable permission rules cannot bypass forced frontend or browser security prompts."
                : "The capability remains limited to an explicitly linked peer and the local exposure/invocation policy."
            : capability.SecurityContract;
        capability.OrganicUseCase = string.IsNullOrWhiteSpace(capability.OrganicUseCase)
            ? $"Organic {string.Join("/", capability.Organs.DefaultIfEmpty("service"))} capability supplied by {capability.Source}."
            : capability.OrganicUseCase;
        if (capability.SuggestedCouncilRoles.Count == 0)
            capability.SuggestedCouncilRoles = capability.Skills.Concat(capability.Organs).Distinct(StringComparer.OrdinalIgnoreCase).Take(6).ToList();
    }

}
