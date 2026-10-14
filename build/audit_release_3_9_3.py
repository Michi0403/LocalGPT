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
version=(3, 9, 3)
if version[1]>9 or version[2]>9: errors.append('version violates one-digit minor/patch policy')
for rel in ('src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj'):
    req(rel,'<Version>3.9.3</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
try:
    meta=json.loads(read('docs/docfx.json')).get('build',{}).get('globalMetadata',{})
    if meta.get('localgptVersion')!='3.9.3': errors.append('docs/docfx.json localgptVersion != 3.9.3')
except Exception as exc: errors.append(f'docfx json parse failed: {exc}')
for rel,mark in (
    ('docs/index.md','**Version 3.9.3**'),('docs/pdf/toc.yml','LocalGPT-3.9.3.pdf'),
    ('RELEASE.md','# LocalGPT 3.9.3'),('CHANGELOG-v3.9.3-SETUP-CANIRUN-RELEASE-IDENTITY-REPAIR.md','Setup, CanIRun, and release-identity repair'),
    ('VALIDATION-v3.9.3-source.md','# LocalGPT 3.9.3 source validation'),
    ('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=3.9.3'),
    ('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/3.9.3')): req(rel,mark)

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
# 3.9.0 application-owned storage contract. Mutable LocalGPT state remains per-user by default;
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

# 3.9.0 boot dependency-cycle repair: preserve ordinary hosted services while preventing
# database-backed runtime policy and service-activity diagnostics from re-entering the
# database initializer during hosted-service construction.
db_init=read('src/LocalGPT/Services/Persistence/DatabaseInitializationService.cs')
migration_compat=read('src/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs')
runtime_policy=read('src/LocalGPT/Services/Persistence/LocalGptRuntimePolicyDataService.cs')
program_registration=read('src/LocalGPT/Program.ServiceRegistration.cs')
for rel,text in (
    ('DatabaseInitializationService.cs', db_init),
    ('DatabaseMigrationCompatibilityService.cs', migration_compat),
):
    if 'IServiceActivityService' in text:
        errors.append(f'{rel} reintroduced IServiceActivityService into the boot-critical database graph')
for marker_text in (
    'CreateSeedDefinition(seedData.GetSeed())',
    'Initialized LocalGPT runtime policy from the built-in seed',
    'private LocalGptRuntimePolicyDefinition CreateSeedDefinition',
    'private LocalGptRuntimePolicyState BuildState',
):
    if marker_text not in runtime_policy:
        errors.append(f'runtime-policy constructor/bootstrap repair missing: {marker_text}')
ctor_start=runtime_policy.find('public LocalGptRuntimePolicyDataService(')
ctor_end=runtime_policy.find('public string GetString', ctor_start)
ctor_block=runtime_policy[ctor_start:ctor_end if ctor_end >= 0 else len(runtime_policy)]
if 'Reload();' in ctor_block:
    errors.append('LocalGptRuntimePolicyDataService constructor still performs synchronous database reload')
for marker_text in (
    'ILocalGptRuntimePolicyDataService runtimePolicy',
    'await initializer.InitializeAsync(stoppingToken).ConfigureAwait(false);',
    'runtimePolicy.Reload();',
    'the built-in seed remains active',
):
    if marker_text not in db_init:
        errors.append(f'database hosted-service post-initialization policy reload missing: {marker_text}')
init_pos=db_init.find('await initializer.InitializeAsync(stoppingToken).ConfigureAwait(false);')
reload_pos=db_init.find('runtimePolicy.Reload();')
if init_pos < 0 or reload_pos < 0 or reload_pos < init_pos:
    errors.append('persisted runtime policy must reload only after database initialization completes')
expected_hosted=(
    'DatabaseInitializationHostedService',
    'RemoteControlPollingHostedService',
    'RuntimeCapabilityDirectoryHostedService',
    'DxAiFunctionCatalogHostedService',
    'OneWireTcpHostedService',
    'OneWireDiscoveryHostedService',
    'OneWireCouncilApprovalProcessorHostedService',
    'OneWireWorkProcessorHostedService',
)
for service in expected_hosted:
    if not re.search(rf'AddHostedService<[^>]*{re.escape(service)}>\(\)', program_registration):
        errors.append(f'normal ASP.NET hosted-service registration missing: {service}')
if program_registration.count('AddHostedService<') != len(expected_hosted):
    errors.append(f'expected exactly {len(expected_hosted)} LocalGPT hosted-service registrations, found {program_registration.count("AddHostedService<")}')
for rel in ('src/LocalGPT/Program.ServiceRegistration.cs','src/LocalGPT/Program.cs'):
    if 'LocalGptPostListenHostedServiceCoordinator' in read(rel):
        errors.append(f'{rel} reintroduced the rejected manual post-listen coordinator')

# 3.9.0 executable Council SQL seed contract.
# The file is a user-facing repair/backup asset and must remain real idempotent SQL.
import hashlib, sqlite3
_seed = ROOT / "docs" / "COUNCIL_KNOWLEDGE_SEED.sql"
if not _seed.is_file():
    errors.append("docs/COUNCIL_KNOWLEDGE_SEED.sql is required by LocalGPT.csproj and the KnowledgeFiles compatibility contract")
else:
    _seed_text = _seed.read_text(encoding="utf-8-sig", errors="replace")
    _insert_marker = 'INSERT OR IGNORE INTO "CouncilKnowledgeEntries"'
    if _seed_text.count(_insert_marker) < 1:
        errors.append("Council seed must contain executable idempotent CouncilKnowledgeEntries INSERT statements")
    for _forbidden in ("UPDATE", "DELETE", "DROP", "ALTER", "REPLACE"):
        if re.search(rf"(?im)^\s*{_forbidden}\b", _seed_text):
            errors.append(f"Council seed contains destructive/non-seed statement: {_forbidden}")
    for _column in ("VerificationStatus", "ReviewStatus", "LastVerifiedAtUtc", "StalenessReason", "StalenessDetectedBy", "SourceHash"):
        if f'"{_column}"' not in _seed_text:
            errors.append(f"Council seed does not supply current required knowledge column: {_column}")
    try:
        _db = sqlite3.connect(":memory:")
        _db.executescript("""
        CREATE TABLE "CouncilKnowledgeEntries" (
            "Id" TEXT NOT NULL PRIMARY KEY,
            "CreatedAtUtc" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL,
            "Topic" TEXT NOT NULL,
            "Scope" TEXT NOT NULL,
            "Content" TEXT NOT NULL,
            "Source" TEXT NOT NULL,
            "HelpfulSources" TEXT NOT NULL,
            "Tags" TEXT NOT NULL,
            "Confidence" INTEGER NOT NULL,
            "VerificationStatus" TEXT NOT NULL,
            "ReviewStatus" TEXT NOT NULL,
            "ExpiresAtUtc" TEXT NULL,
            "LastVerifiedAtUtc" TEXT NULL,
            "LastUsedAtUtc" TEXT NULL,
            "SupersededByKnowledgeId" TEXT NULL,
            "StalenessReason" TEXT NOT NULL,
            "StalenessDetectedAtUtc" TEXT NULL,
            "StalenessDetectedBy" TEXT NOT NULL,
            "SourceHash" TEXT NOT NULL,
            "SourceDateUtc" TEXT NULL,
            "IsUserApproved" INTEGER NOT NULL,
            "IsPinned" INTEGER NOT NULL,
            "IsArchived" INTEGER NOT NULL
        );
        """)
        _db.executescript(_seed_text)
        _rows = _db.execute('SELECT "Id", "Topic", "Scope", "Content", "Source", "HelpfulSources", "SourceHash" FROM "CouncilKnowledgeEntries" ORDER BY rowid').fetchall()
        if len(_rows) != 60:
            errors.append(f"Council seed expected 60 supplied historical rows, inserted {len(_rows)}")
        for _id, _topic, _scope, _content, _source, _helpful, _hash in _rows:
            _expected = hashlib.sha256(f"{_topic}\n{_scope}\n{_source}\n{_helpful}\n{_content}".encode("utf-8")).hexdigest().upper()
            if _hash != _expected:
                errors.append(f"Council seed SourceHash mismatch for {_id}")
                break
        _db.executescript(_seed_text)
        _rows_after_second_run = _db.execute('SELECT COUNT(*) FROM "CouncilKnowledgeEntries"').fetchone()[0]
        if _rows_after_second_run != len(_rows):
            errors.append("Council seed is not idempotent when executed twice")
        _db.close()
    except Exception as exc:
        errors.append(f"Council seed failed executable SQLite validation: {exc}")
_prereq = read("build/Initialize-BuildPrerequisites.ps1")
for _marker in (
    "docs/COUNCIL_KNOWLEDGE_SEED.sql",
    'INSERT OR IGNORE INTO "CouncilKnowledgeEntries"',
    "Council knowledge SQL seed preflight",
    "audit_council_sql_seed.py",
):
    if _marker not in _prereq:
        errors.append(f"clean-source preflight missing executable Council seed validation marker: {_marker}")

if not (ROOT / 'build/audit_council_sql_seed.py').is_file():
    errors.append('generic Council SQL executable audit is missing')


# 3.9.0 local runtime setup completion: provider profiles must parse complete nested JSON,
# Ollama setup must use the existing lifecycle service, CanIRun uses the opt-in JSON API,
# and total system/unified memory must flow through the existing platform hardware boundary.
provider_article = read('docs/reference/ai-provider-installation.md')
profile_blocks = re.findall(r'```localgpt-provider-profile[^\r\n]*\r?\n(?P<json>.*?)\r?\n```', provider_article, re.I | re.S)
if len(profile_blocks) != 6:
    errors.append(f'expected exactly six tagged provider bootstrap blocks, found {len(profile_blocks)}')
else:
    parsed_profiles=[]
    for index, block in enumerate(profile_blocks, 1):
        try:
            parsed_profiles.append(json.loads(block))
        except Exception as exc:
            errors.append(f'provider bootstrap block {index} JSON parse failed: {exc}')
    keys=[str(item.get('key','')).strip().lower() for item in parsed_profiles]
    expected_keys={'ollama-windows','ollama-linux','ollama-macos','lmstudio-windows','lmstudio-linux','lmstudio-macos'}
    if set(keys) != expected_keys:
        errors.append(f'provider bootstrap keys mismatch: {sorted(set(keys))}')
    if len(keys) != len(set(keys)):
        errors.append('provider bootstrap blocks contain duplicate keys')
    for item in parsed_profiles:
        aliases=item.get('modelAliases')
        if not isinstance(aliases, dict):
            errors.append(f"provider bootstrap profile {item.get('key','?')} is missing nested modelAliases object")

initial = read('src/LocalGPT/Services/Persistence/InitialDataCatalog.cs')
for marker_text in (
    'builtin.ai-provider-bootstrap-block-v2',
    '```localgpt-provider-profile[^\\r\\n]*\\r?\\n(?<json>.*?)\\r?\\n```',
    'builtin.ai-provider-bootstrap-block',
):
    if marker_text not in initial:
        errors.append(f'3.9.0 provider parser seed missing: {marker_text}')

bootstrap = read('src/LocalGPT/Services/AiProviderBootstrapService.cs')
for marker_text in (
    'GetRegexAsync("builtin.ai-provider-bootstrap-block-v2")',
    'GetRegexAsync("builtin.ai-provider-bootstrap-block")',
    'v2Matches is { Count: > 0 } ? v2Matches : blockRegexV1?.Matches(content)',
    'IOllamaProcessService ollamaProcesses',
    'await ollamaProcesses.StartAsync(cancellationToken)',
    'IsOllamaProfile(profile)',
):
    if marker_text not in bootstrap:
        errors.append(f'3.9.0 provider bootstrap repair missing: {marker_text}')
if bootstrap.find('await ollamaProcesses.StartAsync(cancellationToken)') > bootstrap.find('ExecuteProfileCommandAsync(profile, "Start provider"'):
    errors.append('Ollama lifecycle routing must occur before the generic provider foreground start command')

panel = read('src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor')
for marker_text in (
    '@inject IOllamaProcessService OllamaProcesses',
    'Start Ollama', 'Stop Ollama', 'Restart Ollama', 'Refresh Ollama / models',
    'ollamaStatus.ExecutablePath',
    'SystemMemoryGiB',
    'Model ID',
    'InstallManualModelAsync',
    'ResolveModelIdAsync(selectedProviderKey, manualModelId)',
    'InstallModelAsync(selectedProviderKey, resolved, true)',
):
    if marker_text not in panel:
        errors.append(f'3.9.0 Setup UI completion missing: {marker_text}')
if 'gpt-oss:20b' in panel:
    errors.append('Setup UI reintroduced a hard-coded default model')

canirun = read('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs')
for marker_text in (
    'https://www.canirun.ai/api/recommend',
    'HttpMethod.Post',
    '"application/json"',
    '["ramGb"]',
    '["gpu"]',
    '["name"]',
    '["vramGb"]',
    'if (!userConfirmedWebLookup)',
    'ValidateCanIRunUri(recommendationUri)',
):
    if marker_text not in canirun:
        errors.append(f'3.9.0 CanIRun JSON integration missing: {marker_text}')
for forbidden in ('/device/', 'data-model-id', 'IRegexPatternService regexPatterns', 'HttpMethod.Get'):
    if forbidden in canirun:
        errors.append(f'3.9.0 CanIRun service still contains retired HTML-scrape path: {forbidden}')

controller = read('src/LocalGPT/Controller/InitialSetupAssistantController.cs')
for marker_text in ('[HttpPost("canirun")]', '[HttpGet("canirun/{deviceSlug}")]', 'GetRecommendationsAsync(device, userConfirmed'):
    if marker_text not in controller:
        errors.append(f'CanIRun controller compatibility wiring missing: {marker_text}')

dx = read('src/LocalGPT/Services/InitialSetupDxAiFunctions.cs')
for marker_text in ('hardwareName', 'systemMemoryGiB', 'dedicatedVramGiB', 'deviceSlug', 'GetRecommendationsAsync(device, userConfirmedWebLookup: true'):
    if marker_text not in dx:
        errors.append(f'CanIRun DXFunction hardware-facts wiring missing: {marker_text}')

hardware_interface = read('src/LocalGPT/Interfaces/IHardwarePlatformProbeService.cs')
inventory_interface = read('src/LocalGPT/Interfaces/IHardwareInventoryService.cs')
platform_probe = read('src/LocalGPT/Services/HardwarePlatformProbeServices.cs')
inventory = read('src/LocalGPT/Services/HardwareInventoryService.cs')
configured = read('src/LocalGPT/Services/ConfiguredAiHostHardwareService.cs')
setup_service = read('src/LocalGPT/Services/InitialSetupAssistantService.cs')
for rel,text,markers in (
    ('IHardwarePlatformProbeService.cs', hardware_interface, ('ProbeSystemMemoryBytesAsync',)),
    ('IHardwareInventoryService.cs', inventory_interface, ('GetSystemMemoryBytesAsync',)),
    ('HardwarePlatformProbeServices.cs', platform_probe, ("Win32_ComputerSystem).TotalPhysicalMemory", '/usr/sbin/sysctl', 'hw.memsize', '/proc/meminfo', 'MemTotal:')),
    ('HardwareInventoryService.cs', inventory, ('ProbeSystemMemoryBytesAsync',)),
    ('ConfiguredAiHostHardwareService.cs', configured, ('GetSystemMemoryBytesAsync', 'draft.SystemMemoryGiB', 'BackfillDetectedSystemMemoryAsync', 'entity.SystemMemoryBytes = systemMemoryBytes')),
    ('InitialSetupAssistantService.cs', setup_service, ('SystemMemoryGiB', 'GetSystemMemoryBytesAsync', 'draft.SystemMemoryGiB', 'localSystemMemoryBytes')),
):
    for marker_text in markers:
        if marker_text not in text:
            errors.append(f'{rel} missing system/unified-memory flow marker: {marker_text}')

components = ROOT / 'src' / 'LocalGPT' / 'Components'
interactive_count = 0
for razor in components.rglob('*.razor'):
    interactive_count += razor.read_text(encoding='utf-8-sig', errors='replace').count('@rendermode InteractiveServer')
if interactive_count != 15:
    errors.append(f'expected the established 15 InteractiveServer boundaries, found {interactive_count}')

for cache_name in ('__pycache__', '.pytest_cache', 'bin', 'obj'):
    for path in ROOT.rglob(cache_name):
        if path.is_dir():
            errors.append(f'repository-local generated/cache directory present: {path.relative_to(ROOT)}')
for path in ROOT.rglob('*'):
    if path.is_file() and path.suffix.lower() in ('.pyc','.pyo'):
        errors.append(f'repository-local compiled Python file present: {path.relative_to(ROOT)}')

# 3.9.3 build-guard repair. Reproduce the maintained PowerShell guard predicates in
# Python so source-only release preparation can catch the same policy failures without pwsh.
text_baseline_path = ROOT / 'build' / 'text-service-ownership-baseline.json'
try:
    text_known = set(json.loads(text_baseline_path.read_text(encoding='utf-8-sig')))
except Exception as exc:
    errors.append(f'text-service ownership baseline could not be read: {exc}')
    text_known = set()
text_pattern = re.compile(r'(?:\bRegex\s*\.|\bnew\s+Regex\s*\(|\.Replace\s*\(|\.Split\s*\(|\bstring\.Join\s*\(|\bWebUtility\.HtmlDecode\s*\(|\.StartsWith\s*\(|\.EndsWith\s*\(|\.IndexOf\s*\(|\.Substring\s*\(|\.Contains\s*\([^\r\n;]*StringComparison\.)')
for folder in ('Components', 'Controllers', 'Controller'):
    folder_path = ROOT / 'src' / 'LocalGPT' / folder
    if not folder_path.is_dir():
        continue
    for path in folder_path.rglob('*'):
        if not path.is_file() or path.suffix not in ('.cs', '.razor'):
            continue
        rel = path.relative_to(ROOT).as_posix()
        for raw_line in path.read_text(encoding='utf-8-sig', errors='replace').splitlines():
            if not text_pattern.search(raw_line):
                continue
            line = ' '.join(raw_line.strip().split())
            if re.search(r'(?:CouncilText|PanelText|TextService|RegexService|StringService|ReviewerPolicy)\.', line):
                continue
            identity = f'{rel}|{line}'
            if identity not in text_known:
                errors.append(f'text-service ownership parity failure: {identity}')

system_baseline_path = ROOT / 'build' / 'system-variable-initialization-baseline.json'
try:
    system_known = set(json.loads(system_baseline_path.read_text(encoding='utf-8-sig')))
except Exception as exc:
    errors.append(f'system-variable initialization baseline could not be read: {exc}')
    system_known = set()
system_allowed = {
    'src/LocalGPT/Program.cs',
    'src/LocalGPT/Services/Persistence/InitialDataCatalog.cs',
    'src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs',
    'src/LocalGPT/Services/Persistence/SystemVariableDefinitionService.cs',
}
constructor_pattern = re.compile(r'^(?P<line>[^\r\n]*(?:=\s*new\s+|Add\w*\s*\(\s*new\s+)[A-Za-z_][\w<>,.?\[\]]*\s*\([^\r\n;]*"(?:[^"\\\r\n]|\\.)*"[^\r\n;]*)$', re.M)
direct_variable_pattern = re.compile(r'(?:VariableStore|variableStoreService|_variableStoreService)\s*\.\s*(?:GetAsync<[^>]+>|SetAsync)\s*\(\s*"', re.M)
for path in (ROOT / 'src' / 'LocalGPT').rglob('*'):
    if not path.is_file() or path.suffix not in ('.cs', '.razor'):
        continue
    rel = path.relative_to(ROOT).as_posix()
    if re.search(r'(^|/)(bin|obj|Migrations)(/|$)', rel) or path.name.endswith('.Designer.cs'):
        continue
    text = path.read_text(encoding='utf-8-sig', errors='replace')
    if direct_variable_pattern.search(text):
        errors.append(f'system-variable initialization parity failure: {rel}|direct-system-variable-name')
    if rel in system_allowed:
        continue
    baseline_rel = rel
    partial_match = re.match(r'^(?P<prefix>.+?)(?:\.[^/]+)\.cs$', rel)
    if partial_match:
        candidate = partial_match.group('prefix') + '.cs'
        if (ROOT / candidate).is_file():
            baseline_rel = candidate
    for match in constructor_pattern.finditer(text):
        line = ' '.join(match.group('line').strip().split())
        if re.search(r'\bnew\s+[A-Za-z_]*Exception\b', line):
            continue
        identity = f'{baseline_rel}|{line}'
        if identity not in system_known:
            errors.append(f'system-variable initialization parity failure: {identity}')

interface_text = read('src/LocalGPT/Interfaces/IInitialSetupAssistantService.cs')
for marker_text in ('bool IsOllamaProfile(AiProviderBootstrapProfile profile);',):
    if marker_text not in interface_text:
        errors.append(f'3.9.3 provider classification ownership marker missing: {marker_text}')
panel_391 = read('src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor')
for marker_text in ('Providers.IsOllamaProfile(profile)', 'FirstOrDefault(Providers.IsOllamaProfile)'):
    if marker_text not in panel_391:
        errors.append(f'3.9.3 Setup provider-classification delegation missing: {marker_text}')
if '.StartsWith("ollama-", StringComparison.OrdinalIgnoreCase)' in panel_391:
    errors.append('3.9.3 Setup component still owns an Ollama profile-prefix StartsWith check')
canirun_391 = read('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs')
for marker_text in ('private const string JsonMediaType = "application/json";', 'Encoding.UTF8, JsonMediaType'):
    if marker_text not in canirun_391:
        errors.append(f'3.9.3 CanIRun constructor-literal guard repair missing: {marker_text}')

# 3.9.3 packaged-runtime setup repair: the launcher, logger, optional hardware probes,
# and bounded provider command runner must not depend on a potentially deleted current directory.
paths = read('src/LocalGPT/Program.ServiceRegistration.cs')
for marker_text in ('ResolveProcessWorkingDirectory()', 'RepairInvalidCurrentDirectory()', 'Directory.SetCurrentDirectory(ResolveProcessWorkingDirectory())'):
    if marker_text not in paths:
        errors.append(f'3.9.3 process working-directory repair missing: {marker_text}')
program_392 = read('src/LocalGPT/Program.cs')
if 'LocalGptApplicationDataPaths.RepairInvalidCurrentDirectory()' not in program_392:
    errors.append('3.9.3 startup no longer repairs an invalid inherited current directory')
launcher_392 = read('build/NativeReleasePackaging.ps1')
for marker_text in ('cd "$USER_DATA_DIR/runtime"', 'durable per-user runtime working directory'):
    if marker_text not in launcher_392:
        errors.append(f'3.9.3 macOS launcher working-directory repair missing: {marker_text}')
if 'cd "$APP" || { show_failure "The packaged application directory could not be opened:' in launcher_392:
    errors.append('3.9.3 macOS launcher still makes the replaceable application bundle its process working directory')
file_logger_392 = read('src/LocalGPT/Logging/FileLogger.cs')
for marker_text in ('ResolveLogPath(_options)', 'LocalGptApplicationDataPaths.ResolveUserPath("logs", "LocalGPT.log")', 'Path.GetTempPath()', 'AppContext.BaseDirectory'):
    if marker_text not in file_logger_392:
        errors.append(f'3.9.3 file-logger runtime-path repair missing: {marker_text}')
if 'Directory.GetCurrentDirectory()' in file_logger_392:
    errors.append('3.9.3 FileLogger still depends on Directory.GetCurrentDirectory()')
console_392 = read('src/LocalGPT/Services/ConsoleCommandService.cs')
if 'return LocalGptApplicationDataPaths.ResolveProcessWorkingDirectory();' not in console_392:
    errors.append('3.9.3 bounded console still lacks the stable default working directory used by provider/model installs')
if 'return Environment.CurrentDirectory;' in console_392:
    errors.append('3.9.3 bounded console still uses Environment.CurrentDirectory as its blank-request default')
hardware_392 = read('src/LocalGPT/Services/HardwareInventoryService.cs')
for marker_text in ('WorkingDirectory = LocalGptApplicationDataPaths.ResolveProcessWorkingDirectory()', 'Optional NVIDIA discovery was unavailable', 'platformGpus = []'):
    if marker_text not in hardware_392:
        errors.append(f'3.9.3 hardware inventory best-effort repair missing: {marker_text}')
platform_392 = read('src/LocalGPT/Services/HardwarePlatformProbeServices.cs')
if platform_392.count('WorkingDirectory = LocalGptApplicationDataPaths.ResolveProcessWorkingDirectory()') != 2:
    errors.append('3.9.3 platform hardware probes must both use the stable process working directory')
setup_392 = read('src/LocalGPT/Services/InitialSetupAssistantService.cs')
for marker_text in ('Optional hardware discovery was unavailable while building initial setup snapshot', 'provider, Ollama, model, and recommendation controls remain usable.', 'hardware = [];'):
    if marker_text not in setup_392:
        errors.append(f'3.9.3 setup optional-hardware isolation missing: {marker_text}')
panel_392 = read('src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor')
for marker_text in ('Start Ollama', 'Stop Ollama', 'Restart Ollama', 'Fetch attributed recommendations', 'Download / install model', 'Install model', 'Register endpoint in LocalGPT'):
    if marker_text not in panel_392:
        errors.append(f'3.9.3 unified setup workflow regressed required UI marker: {marker_text}')
for forbidden in ('recommendations.Take(24)', 'modelChoices.Take(32)'):
    if forbidden in panel_392:
        errors.append(f'3.9.3 unified setup workflow still hides fetched recommendations behind a Razor-only truncation: {forbidden}')
if '.Take(96)' in setup_392:
    errors.append('3.9.3 model-choice mapping still truncates CanIRun recommendations before the unified Setup form can offer each one')
canirun_392 = read('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs')
if 'limit * 4' in canirun_392:
    errors.append('3.9.3 CanIRun parser retains the overflowing recommendation-limit multiplication that can collapse an unlimited policy to one result')
if 'result.Count >= 512' not in canirun_392:
    errors.append('3.9.3 CanIRun JSON traversal lost its explicit 512-object safety bound')


# 3.9.3 Windows/Mac runtime evidence repairs.
canirun_393 = read('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs')
for marker_text in (
    'https://www.canirun.ai/api/recommend',
    'SendRecommendationRequestAsync',
    'redirectCount <= 2',
    'ValidateCanIRunUri(redirectedUri)',
    'OllamaModelId',
    'BuildModelSourceUrl',
):
    if marker_text not in canirun_393:
        errors.append(f'3.9.3 CanIRun repair missing: {marker_text}')
if 'new("https://canirun.ai/api/recommend"' in canirun_393:
    errors.append('3.9.3 still starts CanIRun lookups at the redirecting non-www endpoint')
if 'AllowAutoRedirect = false' not in read('src/LocalGPT/Program.ServiceRegistration.cs'):
    errors.append('3.9.3 CanIRun HTTP client lost redirect-disabled transport policy')

setup_393 = read('src/LocalGPT/Services/InitialSetupAssistantService.cs')
for marker_text in (
    'Provider bootstrap profiles were unavailable while building initial setup snapshot',
    'First-run review status was unavailable while building initial setup snapshot',
    'One recommendation could not be mapped to the selected provider',
    'TryResolveKnownOllamaRecommendationId',
    '"qwen3.5"',
    'Provider install ID needs review.',
    'Installed-model discovery was unavailable while mapping recommendations',
    'One optional CanIRun.ai hardware lookup failed',
):
    if marker_text not in setup_393:
        errors.append(f'3.9.3 setup isolation/model mapping repair missing: {marker_text}')

panel_393 = read('src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor')
for marker_text in (
    'BuildRecoverySnapshotAsync',
    'Setup loaded in recovery mode',
    'choice.CanInstall',
    'CanIRun.ai model',
):
    if marker_text not in panel_393:
        errors.append(f'3.9.3 setup recovery/recommendation UI missing: {marker_text}')
if 'if (snapshot is null)' not in panel_393:
    errors.append('3.9.3 Setup loading contract unexpectedly changed without retaining the initial placeholder')

repository_393 = read('src/LocalGPT/Services/Persistence/DatabaseInitializationService.RepositoryMaintenance.cs')
for marker_text in (
    'AssemblyInformationalVersionAttribute',
    'LocalGPT source project file is not packaged',
    'NormalizeRunningVersion',
):
    if marker_text not in repository_393:
        errors.append(f'3.9.3 installed-package version fallback missing: {marker_text}')
if 'throw new FileNotFoundException("The LocalGPT project file used for source-backed version reconciliation was not found."' in repository_393:
    errors.append('3.9.3 installed runtime still hard-fails when src/LocalGPT/LocalGPT.csproj is absent')

paths_393 = read('src/LocalGPT/Program.ServiceRegistration.cs')
if 'ResolveSafeCurrentDirectory()' not in paths_393:
    errors.append('3.9.3 safe current-directory resolver is missing')
for rel in (
    'src/LocalGPT/Services/CouncilRuntimeService.GenerationGuidanceRuntime.cs',
    'src/LocalGPT/Services/CouncilRuntimeService.ProviderRuntime.cs',
    'src/LocalGPT/Services/CouncilRuntimeService.cs',
):
    if 'Directory.GetCurrentDirectory()' in read(rel):
        errors.append(f'3.9.3 runtime current-directory dependency remains in {rel}')

release_ps = read('Build-Release.ps1')
for marker_text in (
    'Get-ReleaseSourceFingerprint',
    'Initialize-ReleaseArtifactSourceIdentity',
    'SOURCE-SHA256.txt',
    '$script:releaseSourceFingerprint',
    'source fingerprint changed',
):
    if marker_text not in release_ps:
        errors.append(f'3.9.3 release source-identity contract missing: {marker_text}')
if "Join-Path $versionDirectory 'SOURCE-SHA256.txt'" not in release_ps:
    errors.append('3.9.3 final release bundle does not persist source identity')
if "bundleSourceFingerprint" not in release_ps:
    errors.append('3.9.3 complete-bundle reuse does not validate source identity')

# Current known aliases are additive help for fresh profiles; existing profiles are also handled by runtime inference.
provider_doc_393 = read('docs/reference/ai-provider-installation.md')
for marker_text in ('"llama3.1-8b": "llama3.1:8b"', '"deepseek-r1-32b": "deepseek-r1:32b"', '"qwen2.5-coder-32b": "qwen2.5-coder:32b"', '"qwen3.5-9b": "qwen3.5:9b"'):
    if provider_doc_393.count(marker_text) != 3:
        errors.append(f'3.9.3 expected Ollama knowledge alias is not present in all three platform profiles: {marker_text}')


# Provider variant identity and final CWD/logger defenses must not regress.
if 'var installedBase = installed.Split' in setup_393 or 'var requestedBase = requested.Split' in setup_393:
    errors.append('3.9.3 installed-model matching still collapses distinct provider tag/size variants')
for rel in ('src/LocalGPT/Services/LocalPathExplorerService.cs', 'src/LocalGPT/Services/ProjectMaintenanceService.BuildReview.cs'):
    if 'Environment.CurrentDirectory' in read(rel):
        errors.append(f'3.9.3 runtime service still depends directly on Environment.CurrentDirectory: {rel}')
file_logger_provider_393 = read('src/LocalGPT/Logging/FileLoggerProvider.cs')
for marker_text in ('NullLogger.Instance', 'creationFailureReported', 'file logging was disabled'):
    if marker_text not in file_logger_provider_393:
        errors.append(f'3.9.3 file-logger construction defense missing: {marker_text}')

if errors:
    print('LocalGPT 3.9.3 static release audit FAILED:')
    for e in errors: print(' -',e)
    sys.exit(1)
print('LocalGPT 3.9.3 source audit passed.')
