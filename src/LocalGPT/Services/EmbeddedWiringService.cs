using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates embedded wiring behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="catalog">Embedded hardware catalog service dependency used by the embedded wiring workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class EmbeddedWiringService(
    IEmbeddedHardwareCatalogService catalog,
    ILogger<EmbeddedWiringService> logger) : IEmbeddedWiringService
{
    /// <summary>
    /// Creates draft as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="boardProfileKey">Board profile key value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded wiring draft produced by the operation.</returns>
    public async Task<EmbeddedWiringDraft> CreateDraftAsync(string boardProfileKey, string name, CancellationToken cancellationToken = default)
    {
    try
    {
            var profile = await catalog.GetBoardProfileAsync(boardProfileKey, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Embedded board profile '{boardProfileKey}' was not found.");
            var draft = new EmbeddedWiringDraft
            {
                Name = string.IsNullOrWhiteSpace(name) ? $"{profile.DisplayName} wiring" : name.Trim(),
                BoardProfileKey = profile.Key,
                Nodes =
                [
                    new EmbeddedWiringNode
                    {
                        Id = "board",
                        Kind = "Board",
                        Label = profile.DisplayName,
                        PartKey = profile.Key,
                        X = 220,
                        Y = 80,
                        Width = 260,
                        Height = 680,
                        StyleKey = "embedded-board"
                    }
                ]
            };
            draft.Nodes.AddRange(profile.Pins.Select(pin => new EmbeddedWiringNode
            {
                Id = $"board-pin:{NormalizeId(pin.PinKey)}",
                Kind = "BoardPin",
                Label = pin.Label,
                PartKey = profile.Key,
                PinKey = pin.PinKey,
                ElectricalRole = pin.IsGroundPin ? "Ground" : pin.IsPowerPin ? "Power" : "Signal",
                Direction = pin.IsInputOnly ? "Input" : "Bidirectional",
                Voltage = pin.Voltage ?? profile.LogicVoltage,
                X = pin.CanvasX,
                Y = pin.CanvasY,
                Width = 110,
                Height = 28,
                StyleKey = pin.IsReserved ? "pin-danger" : pin.IsBootStrap ? "pin-warning" : "pin-normal"
            }));
            logger.LogInformation("Created embedded wiring draft {DraftId} for board profile {BoardProfileKey} with {NodeCount} node(s).", draft.Id, profile.Key, draft.Nodes.Count);
            return draft;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(CreateDraftAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(CreateDraftAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs validate as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded wiring validation result produced by the operation.</returns>
    public async Task<EmbeddedWiringValidationResult> ValidateAsync(EmbeddedWiringValidationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Draft);
            cancellationToken.ThrowIfCancellationRequested();
            var draft = request.Draft;
            var findings = new List<EmbeddedPlanFinding>();
            var profile = await catalog.GetBoardProfileAsync(draft.BoardProfileKey, cancellationToken).ConfigureAwait(false);
            if (profile is null)
                findings.Add(new("Danger", "BOARD_PROFILE_MISSING", $"Board profile '{draft.BoardProfileKey}' is not available."));
            else if (profile.Status.Contains("Danger", StringComparison.OrdinalIgnoreCase))
                findings.Add(new("Danger", "BOARD_PROFILE_PLACEHOLDER", "The selected board profile is a family placeholder. Select or import an exact board profile before artifact approval."));
            else if (!string.Equals(profile.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                findings.Add(new("Warning", "BOARD_REVIEW_REQUIRED", "The selected board profile still requires an exact board/schematic review before compile or flash."));

            if (draft.CanvasWidth is < 320 or > 20000 || draft.CanvasHeight is < 240 or > 20000)
                findings.Add(new("Warning", "CANVAS_BOUNDS", "Canvas dimensions were outside the normal PublisherStudio workbench range."));

            var nodes = new Dictionary<string, EmbeddedWiringNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in draft.Nodes ?? [])
            {
                if (node is null)
                {
                    findings.Add(new("Danger", "NODE_NULL", "Wiring drafts may not contain null nodes."));
                    continue;
                }
                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    findings.Add(new("Danger", "NODE_ID_MISSING", "Every wiring node requires a stable id."));
                    continue;
                }
                if (!nodes.TryAdd(node.Id.Trim(), node))
                    findings.Add(new("Danger", "NODE_ID_DUPLICATE", $"Wiring node id '{node.Id}' is duplicated."));
                if (node.X < 0 || node.Y < 0 || node.X > draft.CanvasWidth || node.Y > draft.CanvasHeight)
                    findings.Add(new("Warning", "NODE_OUTSIDE_CANVAS", $"Node '{node.Id}' is outside the configured canvas."));
            }

            var connectionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var boardPinUse = new Dictionary<string, List<EmbeddedWiringConnection>>(StringComparer.OrdinalIgnoreCase);
            var protocols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sharedBuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var connection in draft.Connections ?? [])
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (connection is null)
                {
                    findings.Add(new("Danger", "CONNECTION_NULL", "Wiring drafts may not contain null connections."));
                    continue;
                }
                if (string.IsNullOrWhiteSpace(connection.Id) || !connectionIds.Add(connection.Id.Trim()))
                    findings.Add(new("Danger", "CONNECTION_ID", "Every connection requires a unique stable id."));
                if (!nodes.TryGetValue(connection.SourceNodeId ?? string.Empty, out var source) || !nodes.TryGetValue(connection.TargetNodeId ?? string.Empty, out var target))
                {
                    findings.Add(new("Danger", "CONNECTION_ENDPOINT", $"Connection '{connection.Id}' references a missing node."));
                    continue;
                }
                if (string.Equals(source.Id, target.Id, StringComparison.OrdinalIgnoreCase))
                    findings.Add(new("Danger", "CONNECTION_SELF", $"Connection '{connection.Id}' connects a node to itself."));

                var protocolKey = NormalizeProtocol(connection.ProtocolKey);
                protocols.Add(protocolKey);
                if (!string.IsNullOrWhiteSpace(connection.BusKey))
                    sharedBuses.Add(connection.BusKey.Trim());
                EvaluateElectricalConnection(connection, source, target, findings);
                EvaluateBoardEndpoint(profile, request.RequireBoardPinProfileMatch, connection, source, findings, boardPinUse);
                EvaluateBoardEndpoint(profile, request.RequireBoardPinProfileMatch, connection, target, findings, boardPinUse);
            }

            var protocolDescriptors = await catalog.GetProtocolDescriptorsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var pair in boardPinUse.Where(item => item.Value.Count > 1))
            {
                var usedProtocols = pair.Value.Select(item => NormalizeProtocol(item.ProtocolKey)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var allShared = usedProtocols.All(key => protocolDescriptors.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))?.SupportsSharedBus == true);
                var busKeys = pair.Value.Select(item => item.BusKey).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (!allShared || busKeys.Count != 1)
                    findings.Add(new("Danger", "PIN_MULTI_USE", $"Board pin '{pair.Key}' is used by multiple connections without one explicit compatible shared bus.", PinKey: pair.Key));
                else
                    findings.Add(new("Warning", "SHARED_BUS_REVIEW", $"Board pin '{pair.Key}' is shared on bus '{busKeys[0]}'; address, pull-up, termination and topology rules still require review.", PinKey: pair.Key));
            }

            if (request.RequireGroundPath)
            {
                var groundNodes = nodes.Values.Where(IsGround).Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (groundNodes.Count == 0)
                    findings.Add(new("Warning", "GROUND_NODE_MISSING", "No ground node is present in the wiring draft."));
                else if (!(draft.Connections ?? []).Any(item => groundNodes.Contains(item.SourceNodeId) || groundNodes.Contains(item.TargetNodeId)))
                    findings.Add(new("Warning", "GROUND_PATH_MISSING", "Ground exists in the draft but is not connected to the external sensor/device wiring."));
            }

            var status = SeverityStatus(findings);
            var result = new EmbeddedWiringValidationResult
            {
                DraftId = draft.Id,
                Status = status,
                Findings = findings,
                UsedProtocols = protocols.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList(),
                SharedBuses = sharedBuses.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList(),
                CouncilReviewPrompt = BuildCouncilReviewPrompt(draft, profile, findings, protocols, sharedBuses)
            };
            logger.LogInformation("Validated embedded wiring draft {DraftId} with status {Status}, {ConnectionCount} connection(s), and {FindingCount} finding(s).", draft.Id, status, draft.Connections?.Count ?? 0, findings.Count);
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(ValidateAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(ValidateAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs evaluate electrical connection as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connection">Connection value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="findings">Findings value supplied to the embedded wiring operation and used when producing its result.</param>
    private void EvaluateElectricalConnection(
        EmbeddedWiringConnection connection,
        EmbeddedWiringNode source,
        EmbeddedWiringNode target,
        List<EmbeddedPlanFinding> findings)
    {
    try
    {
            if ((IsPower(source) && IsGround(target)) || (IsGround(source) && IsPower(target)))
                findings.Add(new("Danger", "POWER_GROUND_SHORT", $"Connection '{connection.Id}' directly joins power and ground."));
            if (IsOutput(source) && IsOutput(target))
                findings.Add(new("Danger", "OUTPUT_TO_OUTPUT", $"Connection '{connection.Id}' joins two output-driving nodes."));
            if (!IsGround(source) && !IsGround(target) && source.Voltage > 0 && target.Voltage > 0 && Math.Abs(source.Voltage - target.Voltage) > 0.25)
                findings.Add(new("Danger", "VOLTAGE_MISMATCH", $"Connection '{connection.Id}' joins {source.Voltage:0.##} V and {target.Voltage:0.##} V nodes without an explicit level interface."));
            if (connection.Voltage > 0 && source.Voltage > 0 && connection.Voltage - source.Voltage > 0.25)
                findings.Add(new("Danger", "WIRE_VOLTAGE_SOURCE", $"Connection '{connection.Id}' voltage exceeds the source node's declared voltage."));
            if (connection.Voltage > 0 && target.Voltage > 0 && connection.Voltage - target.Voltage > 0.25)
                findings.Add(new("Danger", "WIRE_VOLTAGE_TARGET", $"Connection '{connection.Id}' voltage exceeds the target node's declared voltage."));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(EvaluateElectricalConnection)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(EvaluateElectricalConnection)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs evaluate board endpoint as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="requireProfileMatch">Value indicating whether require profile match should apply to this operation.</param>
    /// <param name="connection">Connection value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="node">Node value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="findings">Findings value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="boardPinUse">Board pin use value supplied to the embedded wiring operation and used when producing its result.</param>
    private void EvaluateBoardEndpoint(
        EmbeddedBoardProfile? profile,
        bool requireProfileMatch,
        EmbeddedWiringConnection connection,
        EmbeddedWiringNode node,
        List<EmbeddedPlanFinding> findings,
        Dictionary<string, List<EmbeddedWiringConnection>> boardPinUse)
    {
    try
    {
            if (!string.Equals(node.Kind, "BoardPin", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(node.PinKey))
                return;
            if (string.IsNullOrWhiteSpace(node.PinKey))
            {
                findings.Add(new("Danger", "BOARD_PIN_KEY_MISSING", $"Board pin node '{node.Id}' has no pin key."));
                return;
            }
            var pinKey = node.PinKey.Trim();
            if (!boardPinUse.TryGetValue(pinKey, out var uses))
                boardPinUse[pinKey] = uses = [];
            uses.Add(connection);
            if (profile is null)
                return;
            var pin = profile.Pins.FirstOrDefault(item => string.Equals(item.PinKey, pinKey, StringComparison.OrdinalIgnoreCase));
            if (pin is null)
            {
                if (requireProfileMatch)
                    findings.Add(new("Danger", "PIN_NOT_IN_PROFILE", $"Pin '{pinKey}' is not present in board profile '{profile.Key}'.", PinKey: pinKey));
                return;
            }
            if (pin.IsReserved)
                findings.Add(new("Danger", "PIN_RESERVED", $"Pin '{pinKey}' is reserved by the selected board profile. {pin.Warning}", pin.Gpio, pinKey));
            if (pin.IsBootStrap)
                findings.Add(new("Warning", "PIN_BOOT_STRAP", $"Pin '{pinKey}' affects boot configuration. {pin.Warning}", pin.Gpio, pinKey));
            var protocol = NormalizeProtocol(connection.ProtocolKey);
            if (pin.Capabilities.Count > 0 && !pin.Capabilities.Contains(protocol, StringComparer.OrdinalIgnoreCase) && !IsGround(node) && !IsPower(node))
                findings.Add(new("Warning", "PIN_PROTOCOL_MISMATCH", $"Pin '{pinKey}' does not advertise protocol '{protocol}' in the selected board profile.", pin.Gpio, pinKey));
            if (pin.IsInputOnly && IsOutput(node))
                findings.Add(new("Danger", "PIN_INPUT_ONLY", $"Pin '{pinKey}' is input-only but the wiring node is configured to drive output.", pin.Gpio, pinKey));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(EvaluateBoardEndpoint)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(EvaluateBoardEndpoint)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds council review prompt as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="draft">Draft value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="profile">Profile value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="findings">Embedded plan finding dependency used by the embedded wiring workflow to provide the corresponding application capability.</param>
    /// <param name="protocols">String dependency used by the embedded wiring workflow to provide the corresponding application capability.</param>
    /// <param name="buses">String dependency used by the embedded wiring workflow to provide the corresponding application capability.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildCouncilReviewPrompt(
        EmbeddedWiringDraft draft,
        EmbeddedBoardProfile? profile,
        IReadOnlyList<EmbeddedPlanFinding> findings,
        IEnumerable<string> protocols,
        IEnumerable<string> buses) {
    try
    {
        return $"""
Review embedded wiring draft '{draft.Name}' ({draft.Id}) for board profile '{profile?.Key ?? draft.BoardProfileKey}'.
Nodes: {draft.Nodes?.Count ?? 0}; connections: {draft.Connections?.Count ?? 0}.
Protocols: {string.Join(", ", protocols.DefaultIfEmpty("none"))}.
Shared buses: {string.Join(", ", buses.DefaultIfEmpty("none"))}.
Deterministic status: {SeverityStatus(findings)}.
Resolve every danger finding, verify the exact board documentation, describe voltage/pull-up/transceiver needs, then map each sensor signal to firmware read logic and a transport-neutral LocalGPT telemetry contract. Physical 1-Wire is only one optional sensor bus; do not force other devices into it. End with a dry-run capture and a learning-round proposal.
""".Trim();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(BuildCouncilReviewPrompt)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(BuildCouncilReviewPrompt)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes protocol as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeProtocol(string? value) {
    try
    {
        return string.IsNullOrWhiteSpace(value) ? EmbeddedProtocolKeys.Custom : value.Trim().ToLowerInvariant();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(NormalizeProtocol)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(NormalizeProtocol)} failed.");
        throw;
    }
}
    /// <summary>
    /// Normalizes identifier as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeId(string? value) {
    try
    {
        return new string((value ?? string.Empty).Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-').ToArray());
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(NormalizeId)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(NormalizeId)} failed.");
        throw;
    }
}
    /// <summary>
    /// Determines whether ground as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="node">Node value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsGround(EmbeddedWiringNode node) {
    try
    {
        return string.Equals(node.ElectricalRole, "Ground", StringComparison.OrdinalIgnoreCase) || string.Equals(node.PinKey, "GND", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(IsGround)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(IsGround)} failed.");
        throw;
    }
}
    /// <summary>
    /// Determines whether power as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="node">Node value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsPower(EmbeddedWiringNode node) {
    try
    {
        return string.Equals(node.ElectricalRole, "Power", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(IsPower)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(IsPower)} failed.");
        throw;
    }
}
    /// <summary>
    /// Determines whether output as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="node">Node value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsOutput(EmbeddedWiringNode node) {
    try
    {
        return (node.Direction ?? string.Empty).Contains("Output", StringComparison.OrdinalIgnoreCase) || (node.Direction ?? string.Empty).Contains("Drive", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(IsOutput)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(IsOutput)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs severity status as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="findings">Embedded plan finding dependency used by the embedded wiring workflow to provide the corresponding application capability.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SeverityStatus(IEnumerable<EmbeddedPlanFinding> findings) {
    try
    {
        return findings.Any(item => string.Equals(item.Severity, "Danger", StringComparison.OrdinalIgnoreCase)) ? "Danger" : findings.Any(item => string.Equals(item.Severity, "Warning", StringComparison.OrdinalIgnoreCase)) ? "Warning" : "Approved";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(SeverityStatus)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedWiringService)}.{nameof(SeverityStatus)} failed.");
        throw;
    }
}
}
