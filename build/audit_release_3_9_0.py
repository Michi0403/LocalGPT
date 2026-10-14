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
version=(3, 9, 0)
if version[1]>9 or version[2]>9: errors.append('version violates one-digit minor/patch policy')
for rel in ('src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj'):
    req(rel,'<Version>3.9.0</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
try:
    meta=json.loads(read('docs/docfx.json')).get('build',{}).get('globalMetadata',{})
    if meta.get('localgptVersion')!='3.9.0': errors.append('docs/docfx.json localgptVersion != 3.9.0')
except Exception as exc: errors.append(f'docfx json parse failed: {exc}')
for rel,mark in (
    ('docs/index.md','**Version 3.9.0**'),('docs/pdf/toc.yml','LocalGPT-3.9.0.pdf'),
    ('RELEASE.md','# LocalGPT 3.9.0'),('CHANGELOG-v3.9.0-LOCAL-RUNTIME-SETUP-COMPLETION.md','local runtime setup completion'),
    ('VALIDATION-v3.9.0-source.md','# LocalGPT 3.9.0 source validation'),
    ('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=3.9.0'),
    ('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/3.9.0')): req(rel,mark)

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
    'https://canirun.ai/api/recommend',
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

if errors:
    print('LocalGPT 3.9.0 static release audit FAILED:')
    for e in errors: print(' -',e)
    sys.exit(1)
print('LocalGPT 3.9.0 source audit passed.')
