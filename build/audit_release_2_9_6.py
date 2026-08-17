#!/usr/bin/env python3
"""Source-only audit for LocalGPT 2.9.7 benchmark stability, role recovery, host hardware and post-run observability."""
from pathlib import Path
import sys,re
ROOT=Path(__file__).resolve().parents[1]
def read(rel):
    base = globals().get("root") or globals().get("ROOT")
    path = base / rel
    if rel.endswith(".cs"):
        stem = path.with_suffix("")
        parts = sorted(stem.parent.glob(stem.name + "*.cs"))
        if parts:
            return "\n".join(part.read_text(encoding="utf-8", errors="replace") for part in parts)
    if rel.endswith(".razor"):
        stem = path.with_suffix("")
        parts = ([path] if path.is_file() else []) + sorted(stem.parent.glob(stem.name + "*.razor.cs"))
        if parts:
            return "\n".join(part.read_text(encoding="utf-8", errors="replace") for part in parts)
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding="utf-8", errors="replace")

def require(rel,needle):
    if needle not in read(rel): raise AssertionError(f"{rel}: missing {needle!r}")
def forbid(rel,needle):
    if needle in read(rel): raise AssertionError(f"{rel}: forbidden {needle!r}")
def count(rel,needle): return read(rel).count(needle)
try:
    for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj']:
        require(rel,'<Version>3.0.7</Version>')
    protocol='src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj'
    require(protocol,'<Version>2.1.1</Version>'); require(protocol,'<PackageVersion>2.1.1</PackageVersion>')
    require('src/LocalGPT/Services/CouncilTeamConfigurationService.cs','private const int CurrentSeedVersion = 26;')

    # Benchmark: curator task pack -> one composite call per profile, no duplicate subject social round.
    seed='src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs'
    require(seed,'DisplayName = "Initial Hardware Calibration Benchmark"')
    require(seed,'AllMembersReadinessPreflightMode = CouncilAllMembersReadinessPreflightMode.Disabled')
    require(seed,'"benchmark-task-design"'); require(seed,'"benchmark-calibration"'); require(seed,'"SystemBenchmarkCalibration"')
    require(seed,'The four curator tasks are executed together in one bounded provider turn')
    require(seed,'Physical/provider host queues run in parallel while each host remains sequential')
    require(seed,'Generic AI-capability refusal receives one bounded same-role retry')
    forbid(seed,'"benchmark-subject-execution"')
    require(seed,'allowedAutomaticFunctions: ["localgpt.knowledge.list"]')
    require(seed,'Do not add UNABLE, skip, opt-out, capability-exemption, delegation, or "ask the user" clauses')
    for dangerous in ['"localgpt.models.benchmark.provider"','"localgpt.hardware.performance.presets.save"','"localgpt.hardware.performance.presets.apply"','"localgpt.hardware.performance.presets.delete"']:
        # These may exist elsewhere in other teams, but must not be advertised in the benchmark PreferredCapabilities block.
        block=read(seed).split('DisplayName = "Initial Hardware Calibration Benchmark"',1)[1].split('private OrganicCouncilTeamDefinition CreateGameDirectorRuntimeTeam',1)[0]
        if dangerous in block: raise AssertionError(f'benchmark seed advertises mutating capability {dangerous}')

    cal='src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs'
    for needle in ['BuildCuratedTaskPack(request.TaskPackText)','MaxProfilesPerModel = 4','MaxTasks = 1','TaskDefinitions = [taskPack]','StopAfterConsecutiveProfileFailures = 2','Task.WhenAll(hostQueues.Select(RunHostQueueAsync))','RequireEmbeddedJsonObject = true','EnforceRoleExecution = true','Strong independent answer exemplars','Weak/refusal/malformed evidence for reviewers']:
        require(cal,needle)

    bench='src/LocalGPT/Services/ProviderModelBenchmarkService.cs'
    for needle in ['TryParseFirstJsonObject','Malformed/truncated provider JSON is benchmark evidence','enableAutomaticTools: false','corrective same-role retry','LooksLikeGenericCapabilityRefusal','taskResult.QualityScore >= 0.30d']:
        require(bench,needle)
    # Untrusted task scoring must not route ordinary malformed provider JSON through the throwing parser.
    if count(bench,'ParseFirstJsonObject(response)') != 0:
        raise AssertionError('provider benchmark task scoring still throws on untrusted response JSON')

    # Role authority, exact provider tool gating, knowledge visibility and queue-free timing.
    models='src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs'
    require(models,'public List<string> AllowedAutomaticFunctions { get; set; } = [];')
    multi='src/LocalGPT/Services/MultiModelCouncilService.cs'
    for needle in ['CURRENT WORKFLOW ROLE TASK — AUTHORITATIVE','ROLE COMPLIANCE: being an AI model is not a reason to decline','automaticFunctionAllowList: automaticFunctionPolicy.AutomaticFunctionAllowList','Knowledge & capability state','DurationSeconds is provider execution time, not time spent waiting for this host/lane lease','stopwatch.Restart()','MultiModelCouncilServiceRunRoleComplianceRecoveryAsync','Native Ollama participants use Ollama automatic GPU-layer placement']:
        require(multi,needle)
    require(multi,'return requestedNumGpu;')
    forbid(multi,'return MultiModelCouncilServiceIsHeavyGpuRiskModel(modelName, logger) ? catalog.DefaultHeavyModelGpuLayers : null;')
    ollama='src/LocalGPT/Services/OllamaThinkingChatClient.cs'
    require(ollama,'automaticFunctionAllowList')
    require(ollama,'automaticFunctionAllowList.Contains(function.Name)')
    require('src/LocalGPT/Services/ProviderModelRuntimeService.cs','IReadOnlyCollection<string>? automaticFunctionAllowList = null')

    # Independent best-of-N style remains a normal social structure, not mixed into candidate first pass.
    require(seed,'DisplayName = "Blind independent answers"')
    require(seed,'do not ask what other Council members think, do not wait for them')
    require(seed,'correctness, completeness, useful originality and factual conflicts')

    # Knowledge can be queried instead of only listing unrelated recent rows.
    require('src/LocalGPT/BusinessObjects/DxAiFunctionParameterModels.cs','public string Query { get; set; } = string.Empty;')
    dx='src/LocalGPT/Services/DxAiFunctionRegistry.cs'
    require(dx,'optional query string for topic/content/tag filtering')
    require(dx,'.Contains(query, StringComparison.OrdinalIgnoreCase)')

    # Human approval is structurally validated before queuing, and the inbox reports execution outcomes truthfully.
    require(dx,'var parameterValidationError = ValidateInvocationParameters(descriptor, request.Parameters);')
    require(dx,'before human approval')
    require(dx,'formatElement.GetString(), "uuid"')
    inbox='src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor'
    require(inbox,'Approved action execution results')
    require(inbox,'Approval is not reported as successful execution.')
    require('src/LocalGPT/Services/HumanCollaborationService.cs','must explicitly consume the matching human answer as highest-priority role input')

    # Stopping/completion preserves the rendered participant lanes.
    chat='src/LocalGPT/Components/Pages/Chat.razor'
    require(chat,'@if (liveCouncilMessage is not null)')
    require(chat,'Completed model lanes and their final answers are preserved above for post-run inspection.')

    # Configured physical host owns durable hardware facts on /install.
    hwmodel='src/LocalGPT/BusinessObjects/ConfiguredAiHostHardwareModels.cs'
    for needle in ['ConfiguredAiHostHardwareProfile','HostKey','ProviderEndpointsJson','GpusJson','IsUserConfirmed','DedicatedMemoryBytes']:
        require(hwmodel,needle)
    require('src/LocalGPT/BusinessObjects/EFCore/LocalGptMemoryDbContext.cs','ConfiguredAiHostHardwareProfiles')
    require('src/LocalGPT/Migrations/20260816194000_AddConfiguredAiHostHardwareProfiles.cs','IX_ConfiguredAiHostHardwareProfiles_HostKey')
    require('src/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs','ConfiguredAiHostHardwareProfiles')
    require('src/LocalGPT/Program.cs','AddScoped<IConfiguredAiHostHardwareService, ConfiguredAiHostHardwareService>()')
    hostsvc='src/LocalGPT/Services/ConfiguredAiHostHardwareService.cs'
    for needle in ['ImportHwInfoAsync','24576','LocalProbe','ImportedReport','if (existing?.IsUserConfirmed == true)','uri.IsLoopback ? "local-machine" : uri.Host.Trim().ToLowerInvariant()','Total\\s+Memory\\s+Size\\s*\\[(MB|GB|GiB)\\]']:
        if needle=='24576':
            continue # report-specific number must not be hard-coded
        require(hostsvc,needle)
    install='src/LocalGPT/Components/Pages/Install.razor'
    for needle in ['Host hardware','Save host hardware','Detect local hardware','Import HWiNFO report','HWiNFO text report','Dedicated VRAM','System RAM']:
        require(install,needle)

    # OS-proof discovery: vendor tool + Linux DRM; legacy Windows AdapterRAM may appear only in warning comment.
    inventory='src/LocalGPT/Services/HardwareInventoryService.cs'
    require(inventory,'nvidia-smi')
    require(inventory,'ProbeLinuxDrmAsync')
    require(inventory,'mem_info_vram_total')
    require(inventory,'Get-CimInstance Win32_VideoController')
    adapter_lines=[line for line in read(inventory).splitlines() if 'AdapterRAM' in line and not line.lstrip().startswith('//')]
    if adapter_lines: raise AssertionError(f'active AdapterRAM usage remains: {adapter_lines}')
    require('src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs','Get-CimInstance Win32_VideoController')
    forbid('src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs','$_.AdapterRAM')
    require('src/LocalGPT/Services/AdaptiveOllamaBenchmarkWiring.cs','IConfiguredAiHostHardwareService')
    require('src/LocalGPT/Services/TimeAndStateDxAiFunction.cs','ConfiguredHostHardware')

    print('LocalGPT 2.9.7 benchmark stability/host-hardware/role-recovery source audit passed.')
except Exception as exc:
    print(f'LocalGPT 2.9.7 source audit failed: {exc}',file=sys.stderr); raise SystemExit(1)
