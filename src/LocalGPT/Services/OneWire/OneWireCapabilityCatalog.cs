using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using System.Text.Json;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Maintains the authoritative directory of one wire capability entries used for discovery, validation, and runtime lookup.
/// </summary>
/// <param name="vocabulary">Local gpt vocabulary service dependency used by the one wire capability workflow to provide the corresponding application capability.</param>
/// <param name="scopeFactory">Service scope factory dependency used by the one wire capability workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the one wire capability workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the one wire capability workflow to provide the corresponding application capability.</param>
/// <param name="hardwareInventory">Hardware inventory service dependency used by the one wire capability workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWireCapabilityCatalog(ILocalGptVocabularyService vocabulary,
    
    IServiceScopeFactory scopeFactory,
    IOneWirePeerRegistry peers,
    IOneWireConnectionRegistry connections,
    IHardwareInventoryService hardwareInventory,
    ILogger<OneWireCapabilityCatalog> logger) : IOneWireCapabilityCatalog
{
    /// <summary>
    /// Retrieves local capabilities in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetLocalCapabilitiesAsync(CancellationToken cancellationToken = default) {
    try
    {
        return BuildCapabilitiesAsync(peerId: null, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(GetLocalCapabilitiesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(GetLocalCapabilitiesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves capabilities in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireCapabilityDescriptor>> IOneWireCapabilityProvider.GetCapabilitiesAsync(CancellationToken cancellationToken) =>
        GetLocalCapabilitiesAsync(cancellationToken);

    /// <summary>
    /// Retrieves skills in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireSkillDescriptor>> IOneWireCapabilityProvider.GetSkillsAsync(CancellationToken cancellationToken) =>
        GetLocalSkillsAsync(cancellationToken);

    /// <summary>
    /// Retrieves UI features in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireUiFeatureDescriptor>> IOneWireCapabilityProvider.GetUiFeaturesAsync(CancellationToken cancellationToken) =>
        GetLocalUiFeaturesAsync(cancellationToken);

    /// <summary>
    /// Retrieves hardware in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireHardwareDescriptor>> IOneWireCapabilityProvider.GetHardwareAsync(CancellationToken cancellationToken) =>
        GetLocalHardwareAsync(cancellationToken);

    /// <summary>
    /// Retrieves local capabilities for peer in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetLocalCapabilitiesForPeerAsync(string peerId, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(peerId);
            return BuildCapabilitiesAsync(peerId, cancellationToken);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(GetLocalCapabilitiesForPeerAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(GetLocalCapabilitiesForPeerAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds capabilities in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<OneWireCapabilityDescriptor>> BuildCapabilitiesAsync(string? peerId, CancellationToken cancellationToken)
    {
    try
    {
            await using var functionScope = scopeFactory.CreateAsyncScope();
            var functionCatalog = functionScope.ServiceProvider.GetRequiredService<IDxAiFunctionCatalogService>();
            var entries = string.IsNullOrWhiteSpace(peerId)
                ? await functionCatalog.GetEntriesAsync(cancellationToken).ConfigureAwait(false)
                : await functionCatalog.GetExposedToPeerAsync(peerId, cancellationToken).ConfigureAwait(false);
            var functions = entries
                .Where(entry => entry.Kind == vocabulary.Get().CatalogDxFunction && entry.IsAvailable && entry.IsEnabled &&
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
                Key = "localgpt.documentation.profile", DisplayName = "LocalGPT documentation profile",
                Description = "Returns same-origin HTML, PDF and API documentation routes plus availability metadata without exposing filesystem paths.",
                Controller = "Documentation", Method = "GET", Route = "/api/documentation/profile", Organs = ["eyes"],
                Skills = ["documentation", "api-discovery"], RequiredSkillKeys = ["documentation"],
                UiActivationKeys = ["localgpt.help"], IsReadOnly = true, RequiresHumanConfirmation = false,
                RequiresFrontendUserConfirmation = false, IsExposedToPeer = true, AllowPeerInvocation = true,
                InteractionEditor = OneWireInteractionEditor.None, ConfigurationKey = "builtin:localgpt.documentation.profile", Source = "LocalGPT",
                InputContract = "No parameters. Only public same-origin routes and availability metadata are returned.",
                OutputContract = "JSON containing documentation version, availability and HTML/API/PDF routes.",
                SecurityContract = "Read-only. Physical paths, private files and browser state are never returned.",
                OrganicUseCase = "Documentation discovery for PublisherStudio and other linked organic clients.",
                SuggestedCouncilRoles = ["documentation navigator", "API discovery specialist"]
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

            foreach (var serviceEntry in entries.Where(entry => entry.Kind == vocabulary.Get().CatalogPublicServiceMethod && entry.IsAvailable && entry.IsEnabled &&
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(BuildCapabilitiesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(BuildCapabilitiesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves local skills in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<OneWireSkillDescriptor>> GetLocalSkillsAsync(CancellationToken cancellationToken = default)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(GetLocalSkillsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(GetLocalSkillsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves local hardware in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public Task<IReadOnlyList<OneWireHardwareDescriptor>> GetLocalHardwareAsync(CancellationToken cancellationToken = default) {
    try
    {
        return hardwareInventory.GetHardwareAsync(cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(GetLocalHardwareAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(GetLocalHardwareAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves local UI features in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<OneWireUiFeatureDescriptor>> GetLocalUiFeaturesAsync(CancellationToken cancellationToken = default)
    {
    try
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
                CreateCapabilityFeature("localgpt.publisher.openscad", "OpenSCAD Team tools", connectedPeers, availableSkills, ["publisher.openscad.generate"], ["openscad"]),
                CreateCapabilityFeature("localgpt.publisher.embedded.wiring", "Embedded wiring workbench", connectedPeers, availableSkills, ["publisher.embedded.wiring.edit.request"], ["embedded"])
            ];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(GetLocalUiFeaturesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(GetLocalUiFeaturesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates feature in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the one wire capability operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the one wire capability operation and used when producing its result.</param>
    /// <param name="enabled">Value indicating whether enabled should apply to this operation.</param>
    /// <param name="disabledReason">Disabled reason value supplied to the one wire capability operation and used when producing its result.</param>
    /// <returns>The one wire UI feature descriptor produced by the operation.</returns>
    private OneWireUiFeatureDescriptor CreateFeature(string key, string name, bool enabled, string disabledReason) {
    try
    {
        return new()
    {
        Key = key,
        DisplayName = name,
        State = enabled ? OneWireUiFeatureState.Enabled : OneWireUiFeatureState.Hidden,
        Reason = enabled ? string.Empty : disabledReason
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(CreateFeature)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(CreateFeature)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates capability feature in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the one wire capability operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the one wire capability operation and used when producing its result.</param>
    /// <param name="connectedPeers">One wire peer advertisement dependency used by the one wire capability workflow to provide the corresponding application capability.</param>
    /// <param name="availableSkills">String dependency used by the one wire capability workflow to provide the corresponding application capability.</param>
    /// <param name="requiredCapabilities">String dependency used by the one wire capability workflow to provide the corresponding application capability.</param>
    /// <param name="requiredSkills">String dependency used by the one wire capability workflow to provide the corresponding application capability.</param>
    /// <returns>The one wire UI feature descriptor produced by the operation.</returns>
    private OneWireUiFeatureDescriptor CreateCapabilityFeature(
        string key,
        string name,
        IReadOnlyList<OneWirePeerAdvertisement> connectedPeers,
        IReadOnlySet<string> availableSkills,
        IReadOnlyList<string> requiredCapabilities,
        IReadOnlyList<string> requiredSkills)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(CreateCapabilityFeature)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(CreateCapabilityFeature)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs built in skills in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<OneWireSkillDescriptor> BuiltInSkills() {
    try
    {
        return [
        new() { Key = "council", DisplayName = "AI Council", Description = "Council heartbeat planning and participation.", SourcePeerId = "localgpt", Organs = ["brain"], CapabilityKeys = ["council.run"], UiActivationKeys = ["localgpt.council.run"], IsOnline = true, IsEnabled = true },
        new() { Key = "spreadsheet", DisplayName = "Spreadsheet", Description = "Spreadsheet analysis and guided editing through an organic plugin.", SourcePeerId = "localgpt", Organs = ["brain", "eyes", "hands"], CapabilityKeys = ["publisher.spreadsheet.inspect"], UiActivationKeys = ["localgpt.publisher.spreadsheet.request"], IsOnline = true, IsEnabled = true },
        new() { Key = "openscad", DisplayName = "OpenSCAD", Description = "OpenSCAD project planning and canonical shape generation.", SourcePeerId = "localgpt", Organs = ["brain", "eyes", "hands"], CapabilityKeys = ["publisher.openscad.generate"], UiActivationKeys = ["localgpt.publisher.openscad"], IsOnline = true, IsEnabled = true },
        new() { Key = "vision", DisplayName = "Vision", Description = "Screen capture, recurring screen reading and visual evidence.", SourcePeerId = "localgpt", Organs = ["eyes"], CapabilityKeys = ["publisher.screen.capture", "publisher.screenreader.start"], UiActivationKeys = ["localgpt.publisher.screenreader"], IsOnline = true, IsEnabled = true },
        new() { Key = "organic-routing", DisplayName = "Organic routing", Description = "Maps project/member skills to DX functions, controllers and external capabilities.", SourcePeerId = "localgpt", Organs = ["brain"], CapabilityKeys = ["organic.skills.manage"], UiActivationKeys = ["localgpt.organic.skills"], IsOnline = true, IsEnabled = true },
        new() { Key = "learning", DisplayName = "Learning Round", Description = "Studies bounded chat memory, logs, knowledge and database regexes and stores untrusted reusable evidence.", SourcePeerId = "localgpt", Organs = ["brain"], CapabilityKeys = ["localgpt.learning.snapshot", "localgpt.learning.maintain", "localgpt.regex.list", "localgpt.regex.test", "localgpt.regex.upsert"], UiActivationKeys = ["localgpt.learning.round"], IsOnline = true, IsEnabled = true },
        new()
        {
            Key = "embedded",
            DisplayName = "Embedded wiring and firmware",
            Description = "Plans bounded ESP32/Arduino pins, buses, telemetry contracts, firmware artifacts and PublisherStudio wiring handoffs without forcing one physical protocol.",
            SourcePeerId = "localgpt",
            Organs = ["brain", "eyes", "hands"],
            CapabilityKeys =
            [
                "embedded.catalog.get",
                "embedded.wiring.draft.create",
                "embedded.wiring.validate",
                "embedded.firmware.plan",
                "embedded.firmware.artifacts.create",
                "embedded.telemetry.preview",
                "embedded.telemetry.onewire-envelope.preview",
                "embedded.sensor.telemetry.publish",
                "publisher.embedded.wiring.edit.request"
            ],
            UiActivationKeys = ["localgpt.publisher.embedded.wiring"],
            IsOnline = true,
            IsEnabled = true
        }
    ];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(BuiltInSkills)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(BuiltInSkills)} failed.");
        throw;
    }
}

    /// <summary>
    /// Parses list in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="json">Json value supplied to the one wire capability operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> ParseList(string? json)
    {
    try
    {
            try { return JsonSerializer.Deserialize<List<string>>(json ?? "[]") ?? []; }
            catch (JsonException) { return []; }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(ParseList)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(ParseList)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs infer organs in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="name">Name value supplied to the one wire capability operation and used when producing its result.</param>
    /// <param name="purpose">Purpose value supplied to the one wire capability operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> InferOrgans(string name, string purpose)
    {
    try
    {
            var text = $"{name} {purpose}";
            var result = new List<string> { "brain" };
            if (text.Contains("screen", StringComparison.OrdinalIgnoreCase) || text.Contains("image", StringComparison.OrdinalIgnoreCase)) result.Add("eyes");
            if (text.Contains("mouse", StringComparison.OrdinalIgnoreCase) || text.Contains("keyboard", StringComparison.OrdinalIgnoreCase) || text.Contains("write", StringComparison.OrdinalIgnoreCase)) result.Add("hands");
            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(InferOrgans)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(InferOrgans)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs infer skills in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="name">Name value supplied to the one wire capability operation and used when producing its result.</param>
    /// <param name="purpose">Purpose value supplied to the one wire capability operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> InferSkills(string name, string purpose)
    {
    try
    {
            var tokens = $"{name} {purpose}".Split([' ', '.', '-', '_', '/', ':'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return tokens.Where(token => token.Length >= 4).Select(token => token.ToLowerInvariant()).Distinct().Take(12).ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(InferSkills)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(InferSkills)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs populate teaching in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="capability">Capability value supplied to the one wire capability operation and used when producing its result.</param>
    private void PopulateTeaching(OneWireCapabilityDescriptor capability)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(PopulateTeaching)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCapabilityCatalog)}.{nameof(PopulateTeaching)} failed.");
        throw;
    }
}

}
