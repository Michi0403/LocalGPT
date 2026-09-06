#!/usr/bin/env python3
from pathlib import Path
import json, re, sys, xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
errors=[]
def read(rel):
    p=ROOT/rel
    if not p.is_file(): errors.append(f'missing file: {rel}'); return ''
    return p.read_text(encoding='utf-8-sig', errors='replace')
def req(rel,m):
    if m not in read(rel): errors.append(f'{rel} missing marker: {m}')
def forbid_all(pattern, message):
    rx=re.compile(pattern,re.I|re.M)
    for p in ROOT.rglob('*'):
        if p.suffix.lower() not in ('.ps1','.psm1') or not p.is_file(): continue
        rel=p.relative_to(ROOT).as_posix()
        if re.search(r'(^|/)(bin|obj|artifacts|packages|node_modules)(/|$)',rel): continue
        text=p.read_text(encoding='utf-8-sig',errors='replace')
        for m in rx.finditer(text):
            line=text.count('\n',0,m.start())+1
            errors.append(f'{rel}:{line} {message}')
version=(3, 8, 6)
if version[1]>9 or version[2]>9: errors.append('version violates one-digit minor/patch policy')
for rel in ('src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj'):
    req(rel,'<Version>3.8.6</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
try:
    meta=json.loads(read('docs/docfx.json')).get('build',{}).get('globalMetadata',{})
    if meta.get('localgptVersion')!='3.8.6': errors.append('docs/docfx.json localgptVersion != 3.8.6')
except Exception as exc: errors.append(f'docfx json parse failed: {exc}')
for rel,mark in (
    ('docs/index.md','**Version 3.8.6**'),('docs/pdf/toc.yml','LocalGPT-3.8.6.pdf'),
    ('RELEASE.md','# LocalGPT 3.8.6'),('CHANGELOG-v3.8.6-STARTUP-GUARD-PARITY-REPAIR.md','startup guard parity repair'),
    ('VALIDATION-v3.8.6-source.md','# LocalGPT 3.8.6 source validation'),
    ('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=3.8.6'),
    ('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/3.8.6')): req(rel,mark)

docs=read('build/Build-Documentation.ps1')
for marker in (
    "$tail.IndexOf('%%EOF', [StringComparison]::Ordinal) -ge 0",
    'function Set-PortableProcessArguments','GetProperty(\'ArgumentList\')',
    'function Stop-PortableProcessTree',"GetMethod('Kill', [Type[]]@([bool]))",
    'Invoke-LocalGptChunkedBrowserPdf','Reusing durable documentation PDF chunk','html-browser-chunked'):
    if marker not in docs: errors.append(f'Build-Documentation missing: {marker}')
native=read('build/NativeReleasePackaging.ps1')
for marker in ('function Get-RelativePathPortable',"GetMethod('GetRelativePath'",'function Invoke-MacNotaryToolWithCredentialRecovery','function Save-MacNotarySubmittedState'):
    if marker not in native: errors.append(f'NativeReleasePackaging missing: {marker}')
compat=read('build/Assert-PowerShellCompatibility.ps1')
for marker in ('$unsupportedContainsPattern','$unsupportedPathRelativePattern','$unsupportedArgumentListPattern','$unsupportedKillTreePattern','Windows PowerShell 5.1 and modern pwsh'):
    if marker not in compat: errors.append(f'compatibility guard missing: {marker}')
# Direct runtime APIs that are absent from Windows PowerShell 5.1/.NET Framework are forbidden.
forbid_all(r'\.Contains\([^\r\n]*,\s*\[(?:System\.)?StringComparison\]::','uses incompatible String.Contains comparison overload')
forbid_all(r'\[(?:System\.)?IO\.Path\]::GetRelativePath\s*\(','uses direct Path.GetRelativePath')
forbid_all(r'\.ArgumentList(?:\.|\s*=)','uses direct ProcessStartInfo.ArgumentList')
forbid_all(r'\.Kill\(\s*\$true\s*\)','uses direct Process.Kill(true)')
# Common PowerShell 7-only language/cmdlet surfaces must not enter maintained scripts.
forbid_all(r'ForEach-Object\s+-Parallel\b','uses PowerShell 7-only ForEach-Object -Parallel')
forbid_all(r'ConvertFrom-Json\s+[^\r\n]*-AsHashtable\b','uses PowerShell 6+ ConvertFrom-Json -AsHashtable')
forbid_all(r'\bJoin-String\b','uses PowerShell 6+ Join-String')
forbid_all(r'\bTest-Json\b','uses PowerShell 6+ Test-Json')
forbid_all(r'\$PSStyle\b','uses PowerShell 7.2+ PSStyle')


# Notarization is a per-artifact transaction. `submit` is non-idempotent and must never be
# run through a retry loop. Pending state is written before upload and ambiguous local results
# are reconciled through idempotent Apple history queries.
build_release=read('Build-Release.ps1')
if build_release.count("build/Initialize-MacReleaseTrust.ps1") != 1: errors.append('Build-Release must invoke Initialize-MacReleaseTrust exactly once')
trust=read('build/Initialize-MacReleaseTrust.ps1')
if 'Read-Host' in trust: errors.append('Initialize-MacReleaseTrust must not block on Read-Host')
native=read('build/NativeReleasePackaging.ps1')
for marker in (
    'function Invoke-MacNotaryToolOnce',
    'function Invoke-MacNotaryToolWithCredentialRecovery',
    'function New-MacNotaryPendingState',
    'function Save-MacNotarySubmittedState',
    'function Get-MacNotaryHistoryEntries',
    'function Resolve-MacNotaryPendingSubmission',
    'baselineSubmissionIds',
    "phase = 'submit-pending'",
    'Submitting $artifactName to Apple notary service exactly once for this transaction',
    'no duplicate upload was issued',
    "Get-MacNotaryObjectPropertyText -InputObject $info -PropertyName 'status'",
    'Set-MacNotaryStatePhase -ArtifactPath $ArtifactPath -State $state -Phase complete'):
    if marker not in native: errors.append(f'NativeReleasePackaging missing artifact-local notary marker: {marker}')
if 'Read-Host' in native: errors.append('NativeReleasePackaging notarization must not block on Read-Host')
if 'Assert-MacNotaryCredentialsUsable' in native: errors.append('redundant pre-submit notary assertion must stay removed')
# Exactly one executable submit argument construction exists in the native packager.
submit_lines=[line for line in native.splitlines() if "@('submit',$ArtifactPath)" in line]
if len(submit_lines)!=1: errors.append(f'expected exactly one notary submit construction, found {len(submit_lines)}')
# The submit call itself must use Invoke-MacNotaryToolOnce, while recovery wrapper call sites are
# limited to history/info/log (idempotent operations).
if 'Invoke-MacNotaryToolOnce -Operation "single upload of $artifactName" -Arguments $submitArguments' not in native:
    errors.append('notary submit is not using the one-shot invocation path')
for m in re.finditer(r'Invoke-MacNotaryToolWithCredentialRecovery[^\n]*', native):
    line=m.group(0)
    if line.startswith('Invoke-MacNotaryToolWithCredentialRecovery('):
        continue
    if not any(token in line for token in ('submission-history query','status query for submission','failure-log download')):
        errors.append(f'retry wrapper used outside idempotent notary query: {line.strip()}')
for forbidden in ('$submit.status','$info.status','$state.submissionId','$state.artifactSha256'):
    if forbidden in native: errors.append(f'NativeReleasePackaging reintroduced StrictMode-unsafe optional property access: {forbidden}')
# Completed state is retained as a hash-bound audit/resume record; it must not be deleted after staple.
if re.search(r'(?m)^\s*Remove-MacNotaryState\s+\$ArtifactPath\s*$', native):
    errors.append('completed artifact state is still deleted instead of retained')
# Application-runtime/provider onboarding parity: clean publishes must contain the knowledge and
# platform discovery needed by the /install workflow, and Chat must not invent a connected model.
csproj=read('src/LocalGPT/LocalGPT.csproj')
for marker_text in (
    'docs\\reference\\toolchain-discovery.md', 'docs\\reference\\ai-provider-installation.md',
    'docs\\reference\\canonical-repositories.md', 'docs\\reference\\design-evolution.md',
    'docs\\guide\\embedded-and-games.md', 'docs\\COUNCIL_KNOWLEDGE_SEED.sql',
    'LocalGPT.Knowledge.ai-provider-installation.md', 'LocalGPT.Knowledge.toolchain-discovery.md'):
    if marker_text not in csproj: errors.append(f'LocalGPT.csproj missing packaged knowledge marker: {marker_text}')
if csproj.count('CopyToPublishDirectory="PreserveNewest"') < 16:
    errors.append('LocalGPT.csproj does not explicitly preserve the complete runtime knowledge set in publish output')
initial=read('src/LocalGPT/Services/Persistence/InitialDataCatalog.cs')
for marker_text in ('TryReadEmbeddedKnowledgeAsync', 'LocalGPT.Knowledge.ai-provider-installation.md', 'LocalGPT.Knowledge.toolchain-discovery.md', 'embedded:{relative}'):
    if marker_text not in initial: errors.append(f'InitialDataCatalog missing embedded knowledge fallback marker: {marker_text}')
ollama=read('src/LocalGPT/Services/OllamaPlatformServices.cs')
for marker_text in ('/Applications/Ollama.app/Contents/Resources/ollama','~/Applications/Ollama.app/Contents/Resources/ollama'):
    if marker_text not in ollama: errors.append(f'macOS Ollama discovery missing: {marker_text}')
lm=read('src/LocalGPT/Services/LmStudioPlatformServices.cs')
for marker_text in ('class WindowsLmStudioPlatformService','class MacOsLmStudioPlatformService','class LinuxLmStudioPlatformService','~/.lmstudio/bin/lms','.cache", "lm-studio", "bin", "lms.exe'):
    if marker_text not in lm: errors.append(f'LM Studio platform discovery missing: {marker_text}')
registration=read('src/LocalGPT/Program.ServiceRegistration.cs')
for marker_text in ('ILmStudioPlatformService, WindowsLmStudioPlatformService','ILmStudioPlatformService, MacOsLmStudioPlatformService','ILmStudioPlatformService, LinuxLmStudioPlatformService'):
    if marker_text not in registration: errors.append(f'LM Studio DI registration missing: {marker_text}')
bootstrap=read('src/LocalGPT/Services/AiProviderBootstrapService.cs')
for marker_text in ('IOllamaPlatformService ollamaPlatform','ILmStudioPlatformService lmStudioPlatform','Environment = BuildProviderCommandEnvironment(profile)','Name = "PATH"','ResolveProviderExecutable'):
    if marker_text not in bootstrap: errors.append(f'provider bootstrap path enrichment missing: {marker_text}')
provider_article=read('docs/reference/ai-provider-installation.md')
if 'lms get {{model}} && lms load {{model}}' not in provider_article: errors.append('Unix LM Studio guided model install does not download and load the model')
if 'lms get {{model}}; if ($LASTEXITCODE -eq 0) { lms load {{model}} }' not in provider_article: errors.append('Windows LM Studio guided model install does not download and load the model')
if provider_article.count('"detectCommand": "lms --help"') != 3: errors.append('LM Studio bootstrap profiles are malformed or duplicated')
for rel in ('src/LocalGPT/appsettings.json','src/LocalGPT/appsettings.Development.json'):
    try:
        app=json.loads(read(rel)); ai=app.get('AICore',{})
        if ai.get('OllamaCore',{}).get('ModelName')!='': errors.append(f'{rel} still preselects an unverified Ollama model')
        if ai.get('ChatGPTLocalCore',{}).get('ModelName')!='': errors.append(f'{rel} still preselects an unverified local OpenAI-compatible model')
    except Exception as exc: errors.append(f'{rel} JSON parse failed: {exc}')
options=read('src/LocalGPT/BusinessObjects/AICoreOptions.cs')
if 'public string ModelName { get; set; } = "gpt-oss:20b";' in options: errors.append('AICoreOptions still contains an unverified gpt-oss local model default')
chat_presets=read('src/LocalGPT/Components/Pages/Chat.PresetsAndCouncilConfiguration.razor.cs')
if re.search(r'defaultPreset.*?ApplyModelPreset\(defaultPreset\)',chat_presets,re.S): errors.append('Chat still auto-applies the DB default model preset before provider discovery')
install=read('src/LocalGPT/Components/Pages/Install.razor')
for marker_text in ('Official download','Guided install/start/model actions','/Applications/Ollama.app','lms get <model>','lms load <model>'):
    if marker_text not in install: errors.append(f'/install provider help missing: {marker_text}')
# Stapling mutates the artifact: completed state must accept both submitted and final stapled hashes.
for marker_text in ('schema = 3','finalArtifactSha256','$stateBeforeStaple = Get-MacNotaryState $ArtifactPath','outside the recorded submitted/stapled hashes'):
    if marker_text not in native: errors.append(f'NativeReleasePackaging missing final stapled-state marker: {marker_text}')

pages=ROOT/'src/LocalGPT/Components/Pages'
for p in pages.rglob('*.razor'):
    t=p.read_text(encoding='utf-8-sig',errors='replace')
    if '@page' in t and p.name!='Error.razor' and '@rendermode InteractiveServer' not in t:
        errors.append(f'routed page lost InteractiveServer: {p.relative_to(ROOT).as_posix()}')
for p in ROOT.rglob('*'):
    if p.is_dir() and p.name in ('bin','obj') and 'src' in p.parts: errors.append(f'repository-local build state present: {p.relative_to(ROOT)}')
# 3.8.4 application-owned storage contract. Mutable LocalGPT state remains per-user by default;
# provider/tool install discovery is intentionally allowed to use host user/system locations.
platform_paths=read('src/LocalGPT/Program.ServiceRegistration.cs')
for marker_text in (
    'internal static class LocalGptApplicationDataPaths',
    'Path.Combine(userProfile, "AppData", "Local")',
    'Path.Combine(userProfile, "Library", "Application Support")',
    'Environment.GetEnvironmentVariable("XDG_DATA_HOME")',
    'Path.Combine(userProfile, ".local", "share")',
    'public static string ResolveUserRoot() => Path.Combine(ResolveUserDataBase(), ProductName)',
    '/Library/Application Support/LocalGPT', '/var/lib/LocalGPT', '/usr/share/LocalGPT', '/opt/LocalGPT'):
    if marker_text not in platform_paths: errors.append(f'LocalGPT application-data policy missing: {marker_text}')
if platform_paths.index('Environment.GetEnvironmentVariable("XDG_DATA_HOME")') > platform_paths.index('var linuxLocal = Environment.GetFolderPath'):
    errors.append('Linux XDG_DATA_HOME must be evaluated before the generic LocalApplicationData fallback')
path_service=read('src/LocalGPT/Services/LocalGptApplicationPathService.cs')
for marker_text in ('runtime", "path-layout.json','FirstBootDetected = !File.Exists(reportFile)','EnsureAndDocumentLayout','BuildKnowledgeSummary','Per-user writable root (default and authoritative)'):
    if marker_text not in path_service: errors.append(f'LocalGPT first-boot path service missing: {marker_text}')
program=read('src/LocalGPT/Program.cs')
for marker_text in ('ResolveUserPath("appsettings.user.json")','AddJsonFile(userSettingsFile, optional: true','EnsureAndDocumentLayout()'):
    if marker_text not in program: errors.append(f'LocalGPT startup path/config marker missing: {marker_text}')
service_registration=read('src/LocalGPT/Program.ServiceRegistration.cs')
for marker_text in ('ResolveUserPath("localgpt-memory.db")','ILocalGptApplicationPathService, LocalGptApplicationPathService'):
    if marker_text not in service_registration: errors.append(f'LocalGPT default database/path registration missing: {marker_text}')
initial=read('src/LocalGPT/Services/Persistence/InitialDataCatalog.cs')
for marker_text in ('docs/reference/runtime-path-layout.md','LocalGPT.Knowledge.runtime-path-layout.md','runtime/application-path-layout','applicationPaths.BuildKnowledgeSummary()'):
    if marker_text not in initial: errors.append(f'LocalGPT path knowledge seed missing: {marker_text}')
install_page=read('src/LocalGPT/Components/Pages/Install.razor')
for marker_text in ('data-testid="runtime-path-layout"','ApplicationPathLayout.UserDataRoot','ApplicationPathLayout.ConfigurationFile','ApplicationPathLayout.DatabaseFile','ApplicationPathLayout.LayoutReportFile'):
    if marker_text not in install_page: errors.append(f'/install path diagnostics missing: {marker_text}')
csproj=read('src/LocalGPT/LocalGPT.csproj')
for marker_text in (r'docs\reference\runtime-path-layout.md','LocalGPT.Knowledge.runtime-path-layout.md'):
    if marker_text not in csproj: errors.append(f'LocalGPT packaged path knowledge missing: {marker_text}')
# Application-owned services must not reintroduce their own LocalApplicationData roots.
allowed_local_appdata={
    'src/LocalGPT/Services/PlatformRuntimeServices.cs',
    'src/LocalGPT/Services/OllamaPlatformServices.cs',
    'src/LocalGPTInstallerConsole/Program.cs',
    'src/LocalGPT/Program.ServiceRegistration.cs',
}
for p in (ROOT/'src').rglob('*.cs'):
    rel=p.relative_to(ROOT).as_posix()
    if 'SpecialFolder.LocalApplicationData' in p.read_text(encoding='utf-8-sig', errors='replace') and rel not in allowed_local_appdata:
        errors.append(f'application-owned code bypasses LocalGptApplicationDataPaths: {rel}')

# 3.8.6 host-start invariant: only a lightweight post-listen coordinator may participate in
# ASP.NET Core host startup. Heavy application workers are resolved only after ApplicationStarted.
registration=read('src/LocalGPT/Program.ServiceRegistration.cs')
hosted_calls=re.findall(r'AddHostedService<([^>]+)>\(\)', registration)
if hosted_calls != ['LocalGptPostListenHostedServiceCoordinator']:
    errors.append(f'expected only LocalGptPostListenHostedServiceCoordinator as direct hosted service, found {hosted_calls}')
for worker in (
    'DatabaseInitializationHostedService', 'RemoteControlPollingHostedService',
    'LocalGPT.Services.Council.RuntimeCapabilityDirectoryHostedService', 'DxAiFunctionCatalogHostedService',
    'OneWireTcpHostedService', 'OneWireDiscoveryHostedService',
    'OneWireCouncilApprovalProcessorHostedService', 'OneWireWorkProcessorHostedService'):
    marker=f'AddSingleton<{worker}>();'
    if marker not in registration:
        errors.append(f'post-listen worker is not registered as a concrete singleton: {worker}')
coordinator=read('src/LocalGPT/Services/LocalGptPostListenHostedServiceCoordinator.cs')
for marker_text in (
    'IHostApplicationLifetime applicationLifetime',
    'applicationLifetime.ApplicationStarted.IsCancellationRequested',
    'CancellationTokenSource.CreateLinkedTokenSource',
    'LocalGPT web listener is online; starting post-listen application workers.',
    'services.GetRequiredService<DatabaseInitializationHostedService>()',
    'services.GetRequiredService<RemoteControlPollingHostedService>()',
    'services.GetRequiredService<RuntimeCapabilityDirectoryHostedService>()',
    'services.GetRequiredService<DxAiFunctionCatalogHostedService>()',
    'services.GetRequiredService<OneWireTcpHostedService>()',
    'services.GetRequiredService<OneWireDiscoveryHostedService>()',
    'services.GetRequiredService<OneWireCouncilApprovalProcessorHostedService>()',
    'services.GetRequiredService<OneWireWorkProcessorHostedService>()',
    'await worker.StartAsync(stoppingToken).ConfigureAwait(false)',
    'await worker.StopAsync(stopBudget.Token).ConfigureAwait(false)'):
    if marker_text not in coordinator:
        errors.append(f'post-listen coordinator missing startup invariant marker: {marker_text}')
# Worker instances must not be resolved in Program/BuildWebApp before ApplicationStarted.
program=read('src/LocalGPT/Program.cs')
for worker in ('DatabaseInitializationHostedService','RemoteControlPollingHostedService','RuntimeCapabilityDirectoryHostedService','DxAiFunctionCatalogHostedService','OneWireTcpHostedService','OneWireDiscoveryHostedService','OneWireCouncilApprovalProcessorHostedService','OneWireWorkProcessorHostedService'):
    if f'GetRequiredService<{worker}>' in program:
        errors.append(f'Program resolves post-listen worker before host startup: {worker}')
if 'app.Lifetime.ApplicationStarted.Register' not in program:
    errors.append('runtime endpoint publication is no longer bound to ApplicationStarted')



# 3.8.6: authoritative PowerShell build guard must assert the same post-listen lifecycle
operational=read('build/Assert-OperationalDiagnostics.ps1')
for marker in (
    'AddSingleton<DatabaseInitializationHostedService>',
    'AddHostedService<LocalGptPostListenHostedServiceCoordinator>',
    'LocalGptPostListenHostedServiceCoordinator.cs',
    'ApplicationStarted',
    'services\\.GetRequiredService<DatabaseInitializationHostedService>',
    "Worker '$workerType' must remain behind LocalGptPostListenHostedServiceCoordinator",
    "Post-listen worker '$workerType' must remain registered as a concrete singleton",
    "Write-Output 'Operational diagnostics validation failed:'",
):
    if marker not in operational:
        errors.append(f'operational diagnostics guard missing 3.8.6 lifecycle marker: {marker}')
if "'AddHostedService<DatabaseInitializationHostedService>'" in operational:
    errors.append('operational diagnostics guard still requires direct DatabaseInitializationHostedService startup registration')
release=read('Build-Release.ps1')
if "build/Assert-OperationalDiagnostics.ps1" not in release:
    errors.append('Build-Release.ps1 does not fail-fast on operational diagnostics before expensive release work')
startup_smoke=read('build/Test-LocalGptStartupHealth.ps1')
for marker in ('LocalGptDatabase__Path', '/health', 'TimeoutSeconds = 45', '$process.Kill()', 'XDG_DATA_HOME', 'LOCALAPPDATA', 'OneWire__Enabled'):
    if marker not in startup_smoke:
        errors.append(f'startup health smoke test missing marker: {marker}')
if "build/Test-LocalGptStartupHealth.ps1" not in release:
    errors.append('Build-Release.ps1 does not run the fail-fast startup health smoke test')
try:
    build_index=release.index('RID-neutral LocalGPT build for documentation failed.')
    smoke_index=release.index('build/Test-LocalGptStartupHealth.ps1')
    docs_index=release.index('Generating the complete LocalGPT documentation once for all runtime packages...')
    if not (build_index < smoke_index < docs_index):
        errors.append('startup health smoke test is not ordered between RID-neutral build and documentation generation')
except ValueError:
    errors.append('could not verify startup health smoke-test ordering in Build-Release.ps1')

if errors:
    print('LocalGPT 3.8.6 static release audit FAILED:')
    for e in errors: print(' -',e)
    sys.exit(1)
print('LocalGPT 3.8.6 source audit passed.')
