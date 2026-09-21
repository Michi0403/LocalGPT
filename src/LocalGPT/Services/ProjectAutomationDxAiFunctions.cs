using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Runs deterministic quarantine inspection without extracting uploaded archives.</summary>
public sealed class InspectProjectIngestionFunction(IProjectIngestionService ingestion, IDxAiFunctionJsonService json, ILogger<InspectProjectIngestionFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.ingestion.inspect", "POST", "/api/dxai/functions/project.ingestion.inspect/invoke",
        "Inspects a chat upload in quarantine, including hashes, safe archive metadata, reviewed regex matches and toolchain hints, without extracting it.",
        "JSON: workspaceName required.", "Read/analysis operation over quarantined data; it does not promote or execute project content.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["workspaceName"],"properties":{"workspaceName":{"type":"string","minLength":1}},"additionalProperties":false}""", IsCoordinationOnly: true);
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<WorkspaceNameParameters>(request.Parameters); if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await ingestion.InspectAsync(binding.Value.WorkspaceName, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) { logger.LogError(ex, "Project ingestion inspect DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

/// <summary>Records one independent Council/model review of quarantined project evidence.</summary>
public sealed class ReviewProjectIngestionFunction(IProjectIngestionService ingestion, IDxAiFunctionJsonService json, ILogger<ReviewProjectIngestionFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.ingestion.review", "POST", "/api/dxai/functions/project.ingestion.review/invoke",
        "Records the current Council/model invocation as an independent review of one quarantined project gate.",
        "JSON: workspaceName, approved, notes optional.", "Reviewer identity comes from the trusted ambient invocation context; a model cannot invent another reviewer identity.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["workspaceName","approved"],"properties":{"workspaceName":{"type":"string"},"approved":{"type":"boolean"},"notes":{"type":"string","maxLength":3000}},"additionalProperties":false}""", IsCoordinationOnly: true);
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProjectIngestionReviewRequest>(request.Parameters); if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await ingestion.ReviewAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) { logger.LogError(ex, "Project ingestion review DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

/// <summary>Promotes a fully reviewed quarantine into its workspace after explicit user approval.</summary>
public sealed class PromoteProjectIngestionFunction(IProjectIngestionService ingestion, IDxAiFunctionJsonService json, ILogger<PromoteProjectIngestionFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.ingestion.promote", "POST", "/api/dxai/functions/project.ingestion.promote/invoke",
        "Promotes a quarantine only after deterministic checks, independent review quorum and explicit user confirmation.",
        "JSON: workspaceName and userConfirmed=true.", "Consequential file promotion. Normal DXFunction human confirmation and the explicit userConfirmed gate are both required.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["workspaceName","userConfirmed"],"properties":{"workspaceName":{"type":"string"},"userConfirmed":{"type":"boolean"}},"additionalProperties":false}""");
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<WorkspaceConfirmationParameters>(request.Parameters); if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await ingestion.PromoteAsync(binding.Value.WorkspaceName, binding.Value.UserConfirmed, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) { logger.LogError(ex, "Project ingestion promote DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

public sealed class StartProjectBlobFunction(IProjectBlobReconstructionService blobs, IDxAiFunctionJsonService json, ILogger<StartProjectBlobFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.blob.start", "POST", "/api/dxai/functions/project.blob.start/invoke",
        "Starts bounded hash-verified multi-file reconstruction for a declared project manifest.", "JSON is ProjectBlobManifest including files and optional manifestSha256.",
        "Creates only a bounded reconstruction session; no project files are promoted or executed.", IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["files"],"properties":{"name":{"type":"string"},"manifestSha256":{"type":"string"},"files":{"type":"array"}},"additionalProperties":false}""", IsCoordinationOnly: true);
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var b = json.Bind<ProjectBlobManifest>(request.Parameters); if (!b.Succeeded) return json.InvalidParameters(b.Error); return json.Success(await blobs.StartAsync(b.Value, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) { logger.LogError(ex, "Blob start DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

public sealed class AddProjectBlobChunkFunction(IProjectBlobReconstructionService blobs, IDxAiFunctionJsonService json, ILogger<AddProjectBlobChunkFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.blob.chunk", "POST", "/api/dxai/functions/project.blob.chunk/invoke",
        "Adds one ordered base64 chunk to a declared reconstruction file.", "JSON: sessionId, relativePath, chunkIndex, base64Data.",
        "Bounded data write into the isolated reconstruction session only.", IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["sessionId","relativePath","chunkIndex","base64Data"],"properties":{"sessionId":{"type":"string","format":"uuid"},"relativePath":{"type":"string"},"chunkIndex":{"type":"integer","minimum":0},"base64Data":{"type":"string"}},"additionalProperties":false}""", IsCoordinationOnly: true);
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var b = json.Bind<BlobChunkParameters>(request.Parameters); if (!b.Succeeded) return json.InvalidParameters(b.Error); return json.Success(await blobs.AddChunkAsync(b.Value.SessionId, b.Value.RelativePath, b.Value.ChunkIndex, b.Value.Base64Data, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) { logger.LogError(ex, "Blob chunk DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

public sealed class FinalizeProjectBlobFunction(IProjectBlobReconstructionService blobs, IDxAiFunctionJsonService json, ILogger<FinalizeProjectBlobFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.blob.finalize", "POST", "/api/dxai/functions/project.blob.finalize/invoke",
        "Verifies every declared file length/hash and canonical manifest, then creates a normal quarantined chat workspace.", "JSON: sessionId.",
        "The result remains quarantined and must pass project.ingestion review/promotion before extraction.", IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["sessionId"],"properties":{"sessionId":{"type":"string","format":"uuid"}},"additionalProperties":false}""", IsCoordinationOnly: true);
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var b = json.Bind<SessionIdParameters>(request.Parameters); if (!b.Succeeded) return json.InvalidParameters(b.Error); return json.Success(await blobs.FinalizeAsync(b.Value.SessionId, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) { logger.LogError(ex, "Blob finalize DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

public sealed class ListRegexCuratorFunction(IRegexCuratorService curator, ILogger<ListRegexCuratorFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.regex.curator.list", "POST", "/api/dxai/functions/localgpt.regex.curator.list/invoke", "Lists regex provenance, approval and independent-review state.", "No parameters.",
        "Read-only curator metadata.", IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler", ParameterSchemaJson: """{"type":"object","additionalProperties":false}""");
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { return new ProjectAutomationDxFunctionResults().Success(await curator.ListAsync(cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) { logger.LogError(ex, "Regex curator list DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

public sealed class ReviewRegexCuratorFunction(IRegexCuratorService curator, IDxAiFunctionJsonService json, ILogger<ReviewRegexCuratorFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.regex.curator.review", "POST", "/api/dxai/functions/localgpt.regex.curator.review/invoke", "Records the current Council/model invocation as one independent regex review.",
        "JSON: name, approved, notes optional.", "Reviewer identity is derived from ambient Council context, not supplied by the model.", IsReadOnly: false, AvailableToAi: true,
        RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["name","approved"],"properties":{"name":{"type":"string"},"approved":{"type":"boolean"},"notes":{"type":"string"}},"additionalProperties":false}""", IsCoordinationOnly: true);
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var b = json.Bind<RegexCuratorReviewRequest>(request.Parameters); if (!b.Succeeded) return json.InvalidParameters(b.Error); return json.Success(await curator.ReviewAsync(b.Value, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) { logger.LogError(ex, "Regex curator review DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

public sealed class ApproveRegexCuratorFunction(IRegexCuratorService curator, IDxAiFunctionJsonService json, ILogger<ApproveRegexCuratorFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.regex.curator.approve", "POST", "/api/dxai/functions/localgpt.regex.curator.approve/invoke", "Applies explicit human approval or withdrawal to one reviewed regex.",
        "JSON: name, approved, userConfirmed=true.", "Consequential curator approval used by security-sensitive ingestion.", IsReadOnly: false, AvailableToAi: true,
        RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["name","approved","userConfirmed"],"properties":{"name":{"type":"string"},"approved":{"type":"boolean"},"userConfirmed":{"type":"boolean"}},"additionalProperties":false}""");
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var b = json.Bind<RegexApprovalParameters>(request.Parameters); if (!b.Succeeded) return json.InvalidParameters(b.Error); return json.Success(await curator.SetUserApprovalAsync(b.Value.Name, b.Value.Approved, b.Value.UserConfirmed, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) { logger.LogError(ex, "Regex curator approval DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

public sealed class OneWireTeamStatusFunction(IOneWireRuntimeSecurityService security, IOneWireCouncilHostEnrollmentService hosts, ILogger<OneWireTeamStatusFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "onewire.team.status", "POST", "/api/dxai/functions/onewire.team.status/invoke", "Shows normal 1-Wire security status, trusted peers and user-enrolled Council work hosts.", "No parameters.",
        "Read-only; does not enable LAN transport or bypass pairing.", IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler", ParameterSchemaJson: """{"type":"object","additionalProperties":false}""");
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { return new ProjectAutomationDxFunctionResults().Success(new { Security = await security.GetStatusAsync(cancellationToken).ConfigureAwait(false), TrustedPeers = await security.GetTrustedPeersAsync(cancellationToken).ConfigureAwait(false), CouncilHosts = await hosts.ListAsync(cancellationToken).ConfigureAwait(false) }); }
        catch (Exception ex) { logger.LogError(ex, "1-Wire team status DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

public sealed class OneWirePairingTicketFunction(IOneWireRuntimeSecurityService security, IDxAiFunctionJsonService json, ILogger<OneWirePairingTicketFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "onewire.pairing.ticket", "POST", "/api/dxai/functions/onewire.pairing.ticket/invoke", "Creates the normal signed 1-Wire pairing ticket that the user exchanges with another LocalGPT.", "JSON: lifetimeMinutes optional 2..1440.",
        "Does not establish trust by itself.", IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler", ParameterSchemaJson: """{"type":"object","properties":{"lifetimeMinutes":{"type":"integer","minimum":2,"maximum":1440}},"additionalProperties":false}""", IsCoordinationOnly: true);
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var b = json.Bind<PairingTicketParameters>(request.Parameters); if (!b.Succeeded) return json.InvalidParameters(b.Error); return json.Success(await security.CreatePairingTicketAsync(TimeSpan.FromMinutes(Math.Clamp(b.Value.LifetimeMinutes <= 0 ? 15 : b.Value.LifetimeMinutes, 2, 1440)), cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) { logger.LogError(ex, "1-Wire pairing ticket DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

public sealed class OneWireEstablishTrustFunction(IOneWireRuntimeSecurityService security, IDxAiFunctionJsonService json, ILogger<OneWireEstablishTrustFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "onewire.trust.establish", "POST", "/api/dxai/functions/onewire.trust.establish/invoke", "Establishes normal MFA-verified 1-Wire trust from a reviewed pairing ticket.", "JSON is OneWireTrustEstablishmentRequest.",
        "Requires human confirmation; normal ticket signature and local MFA validation remain authoritative.", IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["ticket","mfaCode"],"properties":{"ticket":{"type":"object"},"mfaCode":{"type":"string"},"validForMinutes":{"type":"integer"}},"additionalProperties":false}""");
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var b = json.Bind<OneWireTrustEstablishmentRequest>(request.Parameters); if (!b.Succeeded) return json.InvalidParameters(b.Error); return json.Success(new { established = await security.EstablishTrustAsync(b.Value, cancellationToken).ConfigureAwait(false) }); }
        catch (Exception ex) { logger.LogError(ex, "1-Wire trust DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

public sealed class OneWireCouncilHostsFunction(IOneWireCouncilHostEnrollmentService hosts, ILogger<OneWireCouncilHostsFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "onewire.council.hosts", "POST", "/api/dxai/functions/onewire.council.hosts/invoke", "Lists already-trusted LocalGPT peers enrolled by the user as Council work hosts.", "No parameters.",
        "Read-only; enrollment never substitutes for pairing/trust.", IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler", ParameterSchemaJson: """{"type":"object","additionalProperties":false}""");
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { return new ProjectAutomationDxFunctionResults().Success(await hosts.ListAsync(cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) { logger.LogError(ex, "1-Wire Council hosts DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

public sealed class EnrollOneWireCouncilHostFunction(IOneWireCouncilHostEnrollmentService hosts, IDxAiFunctionJsonService json, ILogger<EnrollOneWireCouncilHostFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "onewire.council.host.enroll", "POST", "/api/dxai/functions/onewire.council.host.enroll/invoke", "Enrolls an already trusted, unexpired 1-Wire peer as a Council work host.", "JSON: peerId, userConfirmed=true.",
        "Requires human confirmation and refuses untrusted peers.", IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["peerId","userConfirmed"],"properties":{"peerId":{"type":"string"},"userConfirmed":{"type":"boolean"}},"additionalProperties":false}""");
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var b = json.Bind<PeerConfirmationParameters>(request.Parameters); if (!b.Succeeded) return json.InvalidParameters(b.Error); return json.Success(await hosts.EnrollAsync(b.Value.PeerId, b.Value.UserConfirmed, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) { logger.LogError(ex, "1-Wire Council host enrollment DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

public sealed class ListAsciiSemanticActionsFunction(IAsciiSemanticActionService actions, ILogger<ListAsciiSemanticActionsFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.ascii.actions.list", "POST", "/api/dxai/functions/localgpt.ascii.actions.list/invoke", "Lists semantic ASCII/game actions shared by keyboard, gamepad, pointer/touch and AI/team invocation.", "No parameters.",
        "Read-only semantic action catalog.", IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler", ParameterSchemaJson: """{"type":"object","additionalProperties":false}""");
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new ProjectAutomationDxFunctionResults().Success(actions.ListActions());
            logger.LogDebug("Listed semantic ASCII actions for DXFunction invocation.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Listing semantic ASCII actions failed.");
            return Task.FromResult(new ProjectAutomationDxFunctionResults().Failed(ex));
        }
    }
}

public sealed class InvokeAsciiSemanticActionFunction(IAsciiSemanticActionService actions, IDxAiFunctionJsonService json, ILogger<InvokeAsciiSemanticActionFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.ascii.actions.invoke", "POST", "/api/dxai/functions/localgpt.ascii.actions.invoke/invoke", "Invokes one legal semantic ASCII/game action through the authoritative game service instead of synthesizing OS mouse coordinates.",
        "JSON: sessionId, action, expectedTurn optional.", "GameDirector/control-mode policy remains authoritative; this does not provide arbitrary desktop control.", IsReadOnly: false, AvailableToAi: true,
        RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["sessionId","action"],"properties":{"sessionId":{"type":"string","format":"uuid"},"action":{"type":"string"},"expectedTurn":{"type":"integer"}},"additionalProperties":false}""");
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { var b = json.Bind<AsciiSemanticActionRequest>(request.Parameters); if (!b.Succeeded) return json.InvalidParameters(b.Error); return json.Success(await actions.InvokeAsync(b.Value, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) { logger.LogError(ex, "ASCII semantic action DXFunction failed."); return new ProjectAutomationDxFunctionResults().Failed(ex); }
    }
}

/// <summary>Builds an advisory processing plan for a quarantined upload from approved evidence, knowledge, and existing Council teams.</summary>
public sealed class RecommendProjectIngestionFunction(IUploadProcessingRecommendationService recommendations, IDxAiFunctionJsonService json, ILogger<RecommendProjectIngestionFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.ingestion.recommend", "POST", "/api/dxai/functions/project.ingestion.recommend/invoke",
        "Recommends what to do with one quarantined upload and which existing LocalGPT Council team/configuration is a good fit, using curator-approved evidence and approved knowledge.",
        "JSON: workspaceName required; userGoal optional.",
        "Advisory only. It cannot promote, execute, build, install, publish, or bypass the quarantine/review gate.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["workspaceName"],"properties":{"workspaceName":{"type":"string","minLength":1},"userGoal":{"type":"string","maxLength":4000}},"additionalProperties":false}""", IsCoordinationOnly: true);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<UploadProcessingRecommendationRequest>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            return json.Success(await recommendations.RecommendAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Project ingestion recommendation DXFunction failed.");
            return new ProjectAutomationDxFunctionResults().Failed(ex);
        }
    }
}

public sealed class WorkspaceNameParameters { public string WorkspaceName { get; set; } = string.Empty; }
public sealed class WorkspaceConfirmationParameters { public string WorkspaceName { get; set; } = string.Empty; public bool UserConfirmed { get; set; } }
public sealed class BlobChunkParameters { public Guid SessionId { get; set; } public string RelativePath { get; set; } = string.Empty; public int ChunkIndex { get; set; } public string Base64Data { get; set; } = string.Empty; }
public sealed class SessionIdParameters { public Guid SessionId { get; set; } }
public sealed class RegexApprovalParameters { public string Name { get; set; } = string.Empty; public bool Approved { get; set; } public bool UserConfirmed { get; set; } }
public sealed class PairingTicketParameters { public int LifetimeMinutes { get; set; } = 15; }
public sealed class PeerConfirmationParameters { public string PeerId { get; set; } = string.Empty; public bool UserConfirmed { get; set; } }
