#!/usr/bin/env python3
from pathlib import Path
import json, re, subprocess, sys, xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
errors=[]

def text(rel):
    p=ROOT/rel
    if not p.is_file():
        errors.append(f'missing file: {rel}')
        return ''
    return p.read_text(encoding='utf-8-sig',errors='replace')

def req(rel,needle):
    if needle not in text(rel): errors.append(f'{rel} missing: {needle}')

def forbid(rel,needle):
    if needle in text(rel): errors.append(f'{rel} contains forbidden: {needle}')

version=(3,7,5)
if any(x>9 for x in version[1:]): errors.append('version violates one-digit minor/patch policy')
for rel in ('src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj'):
    req(rel,'<Version>3.7.5</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
try:
    data=json.loads(text('docs/docfx.json'))
    if data.get('build',{}).get('globalMetadata',{}).get('localgptVersion')!='3.7.5':
        errors.append('docs/docfx.json localgptVersion is not 3.7.5')
except Exception as exc: errors.append(f'docs/docfx.json JSON parse failed: {exc}')
req('docs/pdf/toc.yml','LocalGPT-3.7.5.pdf')
req('docs/index.md','**Version 3.7.5**')
req('RELEASE.md','# LocalGPT 3.7.5')
req('CHANGELOG-v3.7.5-SHARED-RELEASE-PACKAGING-OWNERSHIP-RESTORE.md','LocalGPT.ReleasePackaging')
req('VALIDATION-v3.7.5-source.md','# LocalGPT 3.7.5 source validation')
req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/3.7.5')
req('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=3.7.5')
req('src/LocalGPTInstallerConsole/Program.cs','if (!OperatingSystem.IsWindows())')

build=text('Build-Release.ps1')
for marker in (
    '[switch]$AllowUnsignedMacPackages',
    "build/Initialize-MacReleaseTrust.ps1",
    "-ProductName 'LocalGPT'",
    '-AllowUnsignedMacPackages:$AllowUnsignedMacPackages',
):
    if marker not in build: errors.append(f'Build-Release.ps1 missing: {marker}')

trust=text('build/Initialize-MacReleaseTrust.ps1')
for marker in (
    "$env:MACOS_REQUIRE_NOTARIZATION = '1'",
    "'future2-notary'",
    'notarytool history --keychain-profile',
    'Developer ID Application:',
    'Developer ID Installer:',
    'MACOS_DEVELOPER_ID_APPLICATION',
    'MACOS_DEVELOPER_ID_INSTALLER',
    'MACOS_NOTARY_KEYCHAIN_PROFILE',
    'Apple Developer ID certificates are not a Windows trust identity',
):
    if marker not in trust: errors.append(f'build/Initialize-MacReleaseTrust.ps1 missing: {marker}')

native=text('build/NativeReleasePackaging.ps1')
for marker in (
    'sysctl -n hw.optional.arm64',
    'sysctl -n sysctl.proc_translated',
    'LOCALGPT_NATIVE_REEXEC',
    'native-architecture-manifest.txt',
    'Assert-MacBundleArchitecture $app $Rid',
    'com.apple.security.cs.allow-jit',
    '$nestedCodeBundles = @(',
    'Developer ID signed and verified',
    'function Sign-MacDiskImage',
    'Sign-MacDiskImage $Destination',
    '--type open',
    "'context:primary-signature'",
    '--type install',
    'notarytool submit',
    'stapler staple',
    'stapler validate',
    '--check-signature',
    'Signed, notarized, stapled, and validated macOS',
):
    if marker not in native: errors.append(f'build/NativeReleasePackaging.ps1 missing: {marker}')


# Release-size and resume invariants preserved from the prior release.
build=text('Build-Release.ps1')
for marker in (
    '[string]$DocumentationCacheRoot', '[string]$ReleaseOutputRoot', '[switch]$ForceRebuildArtifacts',
    "$mode = 'Full'", "$selfContained = 'true'", 'FUTURE2_DOCUMENTATION_CACHE_ROOT', 'FUTURE2_RELEASE_OUTPUT_ROOT', 'Clear-RepositoryReleaseBuildState', '-ProbeExistingArtifactsOnly', 'Test-ExistingReleaseBundleComplete', 'Move-OrReuseReleaseFile'
):
    if marker not in build: errors.append(f'Build-Release.ps1 missing release-size/resume marker: {marker}')
if "foreach ($mode in @('Full','Light'))" in build: errors.append('Build-Release.ps1 still emits the removed Light Unix/macOS lane')
docs=text('build/Build-Documentation.ps1')
for marker in ('Resolve-LocalGptHomebrew','/opt/homebrew/bin/brew','/usr/local/bin/brew','DisablePdfToolProvisioning','brewPath install ghostscript','FUTURE2_DOCUMENTATION_PDF_MAX_BYTES','Ensure-', 'ghostscript-screen-optimized','browserPdfTimeoutMilliseconds','maximumSanePdfBytes','268435456L','cached-validated-pdf','brewPath install ghostscript','runtimePdfPublished = Test-Path -LiteralPath (Join-Path $publishRoot $pdfName)'):
    if marker not in docs: errors.append(f'build/Build-Documentation.ps1 missing PDF-size marker: {marker}')
native=text('build/NativeReleasePackaging.ps1')
if 'notarytool wait' in native: errors.append('build/NativeReleasePackaging.ps1 uses unsupported notarytool wait subcommand; resume must poll notarytool info')
for marker in ('Test-MacDistributionArtifactReady','Reusing already signed, notarized, stapled','brew install rpm','[switch]$ForceRebuildArtifacts','[switch]$ProbeExistingArtifactsOnly','The complete $Rid Full macOS release already exists','notary-state.json','MACOS_NOTARY_WAIT_TIMEOUT','MACOS_NOTARY_POLL_INTERVAL_SECONDS','Start-Sleep -Seconds $sleepSeconds','Resuming Apple notarization submission'):
    if marker not in native: errors.append(f'build/NativeReleasePackaging.ps1 missing resume/RPM marker: {marker}')


# The compressed handbook must remain embedded in runtime help; do not regress to the 3.7.0 HTML-only copy policy.
for marker in (
    'Embedded LocalGPT documentation PDF is missing after runtime documentation copy',
    'runtimePdfPublished -NotePropertyValue $true',
    'pdfAvailable -NotePropertyValue $true',
    'compressed embedded PDF documentation',
):
    if marker not in build: errors.append(f'Build-Release.ps1 missing embedded-PDF marker: {marker}')
for forbidden in ('without duplicating the standalone release PDF','Runtime HTML documentation must not duplicate the standalone release PDF','pdfAvailable=false when the standalone PDF is not embedded'):
    if forbidden in build: errors.append(f'Build-Release.ps1 contains obsolete HTML-only runtime-PDF policy: {forbidden}')
if 'Remove-Item -LiteralPath $sourcePdfPath -Force' in docs:
    errors.append('build/Build-Documentation.ps1 still deletes the generated PDF from the runtime help-docs tree')
for marker in ('Embedded LocalGPT documentation PDF was not published into the runtime help-docs tree','runtimePdfPublished -NotePropertyValue $true','pdfAvailable -NotePropertyValue $true'):
    if marker not in docs: errors.append(f'build/Build-Documentation.ps1 missing embedded-PDF marker: {marker}')


# Adaptive managed PDF pipeline: PDFsharp merge is the normal large-doc path; native compressors are optional.
req('src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj','<Version>1.0.2</Version>')
req('src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj','<PackageReference Include="PDFsharp" Version="6.2.4" />')
for marker in ('case "pdf-merge"','case "pdf-optimize"','PdfReader.Open','PdfDocumentOpenMode.Import','--optimize-images'):
    if marker not in text('src/LocalGPT.ReleasePackaging/Program.cs'): errors.append(f'shared packaging helper missing PDF marker: {marker}')
for marker in ('[string]$PackagingTool','FUTURE2_DOCUMENTATION_BROWSER_PDF_CHUNK_PAGES','Invoke-LocalGptChunkedBrowserPdf','-FrontMatterOnly','-SuppressFrontMatter',"$mergeArguments.Add('pdf-merge')",'Find-LocalGptQpdf',"$managedArguments.Add('pdf-optimize')",'release-packaging-adaptive','html-browser-chunked'):
    if marker not in docs: errors.append(f'build/Build-Documentation.ps1 missing adaptive-PDF marker: {marker}')
if 'Remove-LocalGptTemporaryPath -Path $destinationDirectory' in docs:
    errors.append('build/Build-Documentation.ps1 deletes the shared chunk directory while constructing a chunk; earlier browser PDF chunks would be lost before merge')
for marker in ("$releasePackagingVersion = '1.0.2'",'-PackagingTool $releasePackagingTool'):
    if marker not in build: errors.append(f'Build-Release.ps1 missing managed-PDF packaging marker: {marker}')
req('build/Ensure-ReleasePackagingPackage.ps1','[string]$Version = "1.0.2"')
req('build/Publish-ReleasePackagingPackage.ps1','[string]$Version = "1.0.2"')
req('packages/README.md','LocalGPT is the authoritative source owner')
for marker in (
    '[Parameter(Mandatory)][string]$ReleasePackagingPackagePath',
    '$WireProtocolPackagePath, $ReleasePackagingPackagePath, $SetupIconPath',
    '$requiredBundleNames = @($wirePackageName, $releasePackagingPackageName, "LocalGPT-$Version.pdf")',
    '-ReleasePackagingPackagePath $releasePackagingPackage',
    'Copy-Item $releasePackagingPackage (Join-Path $sharedWirePackageDirectory $releasePackagingPackageName) -Force',
    'Release-packaging package cache:',
):
    if marker not in build: errors.append(f'Build-Release.ps1 missing authoritative release-packaging ownership/bundle marker: {marker}')

# PowerShell parser-preflight regression guard for interpolation before a literal colon.
req('Build-Release.ps1', "build/Assert-PowerShellCompatibility.ps1")
compat=text('build/Assert-PowerShellCompatibility.ps1')
for marker in ('System.Management.Automation.Language.Parser','has a PowerShell parser error'):
    if marker not in compat: errors.append(f'build/Assert-PowerShellCompatibility.ps1 missing parser-preflight marker: {marker}')
for marker in ('${SubmissionId}:','${ArtifactPath}:'):
    if marker not in native: errors.append(f'build/NativeReleasePackaging.ps1 missing delimited interpolation marker: {marker}')
suspicious_colon = re.compile(r'\$([A-Za-z_][A-Za-z0-9_]*):')
allowed_scopes = {'script','env','global','local','private','using'}
for ps_path in list(ROOT.rglob('*.ps1')) + list(ROOT.rglob('*.psm1')):
    rel = ps_path.relative_to(ROOT).as_posix()
    if re.search(r'(^|/)(\.git|\.vs|artifacts|bin|obj|packages|node_modules)(/|$)', rel):
        continue
    value = ps_path.read_text(encoding='utf-8-sig', errors='replace')
    for line_number, line in enumerate(value.splitlines(), 1):
        for match in suspicious_colon.finditer(line):
            if match.group(1).lower() not in allowed_scopes:
                errors.append(f'{rel}:{line_number} has suspicious unbraced variable-plus-colon interpolation: {match.group(0)}')

# Windows output must not be falsely signed with an Apple identity.
forbid('Build-Release.ps1','signtool')
forbid('build/NativeReleasePackaging.ps1','osslsigncode')

# Preserve the three startup background-service fixes and continuation policy.
for rel,cls in (
    ('src/LocalGPT/Services/Persistence/DatabaseInitializationService.cs','DatabaseInitializationHostedService'),
    ('src/LocalGPT/Services/Council/RuntimeCapabilityDirectoryService.cs','RuntimeCapabilityDirectoryHostedService'),
    ('src/LocalGPT/Services/DxAiFunctionCatalogService.cs','DxAiFunctionCatalogHostedService'),
):
    value=text(rel)
    if not re.search(rf'class\s+{re.escape(cls)}[\s\S]*?\)\s*:\s*BackgroundService',value): errors.append(f'{rel}: {cls} is not a BackgroundService')
    if 'await Task.Delay(1, stoppingToken).ConfigureAwait(false);' not in value: errors.append(f'{rel}: missing policy-compliant startup handoff')

async_audit=subprocess.run([sys.executable,str(ROOT/'build/audit_async_continuations.py'),'--source-root',str(ROOT/'src/LocalGPT')],cwd=ROOT,text=True,capture_output=True)
if async_audit.returncode!=0: errors.append('async audit failed:\n'+async_audit.stdout+async_audit.stderr)

count=0
for p in (ROOT/'src/LocalGPT').rglob('*.razor'):
    count += p.read_text(encoding='utf-8-sig',errors='replace').count('@rendermode InteractiveServer')
if count != 15: errors.append(f'InteractiveServer occurrence count changed: expected 15, found {count}')

for p in ROOT.rglob('*'):
    if p.is_dir() and p.name in ('bin','obj') and 'src' in p.parts:
        errors.append(f'repository-local build state present: {p.relative_to(ROOT)}')

if errors:
    print('LocalGPT 3.7.5 static release audit FAILED:')
    for e in errors: print(' -',e)
    raise SystemExit(1)
print('LocalGPT 3.7.5 source audit passed.')
