#!/usr/bin/env python3
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
errors = []

def text(rel):
    p = ROOT / rel
    if not p.is_file():
        errors.append(f'missing file: {rel}')
        return ''
    return p.read_text(encoding='utf-8-sig', errors='replace')

def req(rel, needle, msg=None):
    if needle not in text(rel):
        errors.append(msg or f'{rel} missing: {needle}')

def forbid(rel, needle, msg=None):
    if needle in text(rel):
        errors.append(msg or f'{rel} contains forbidden: {needle}')

for rel in (
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
):
    req(rel, '<Version>3.5.8</Version>')
req('docs/docfx.json', '"localgptVersion": "3.5.8"')
req('docs/pdf/toc.yml', 'LocalGPT-3.5.8.pdf')
req('src/LocalGPT/Components/App.razor', 'localgpt-chat-ui.js?v=3.5.8')
req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs', 'LocalGPT/3.5.8')
req('RELEASE.md', 'CHANGELOG-v3.5.8-MACOS-DOCUMENTATION-PDF-RENDER-RECOVERY.md')
req('RELEASE.md', 'VALIDATION-v3.5.8-source.md')
text('CHANGELOG-v3.5.8-MACOS-DOCUMENTATION-PDF-RENDER-RECOVERY.md')
text('VALIDATION-v3.5.8-source.md')


# macOS PDF recovery: validated browser candidates, compatibility renderer, and bounded fallback timeout.
doc = text('build/Build-Documentation.ps1')
for marker in (
    '$isMacOsHost',
    '$pdfTimeoutMilliseconds = if ($isMacOsHost) { 300000 } else { 1800000 }',
    'Name = "tagged"',
    'Name = "compatibility"',
    'html-browser-print-compatibility',
    'html-accessibility-fallback',
    'Test-LocalGptCompletePdf -Path $PdfPath -MinimumBytes $MinimumBytes',
    '$configuredPdfTimeout -gt 0',
    'pdfAccessibilityMode = $pdfAccessibilityMode',
):
    if marker not in doc:
        errors.append(f'build/Build-Documentation.ps1 missing macOS PDF recovery marker: {marker}')
release = text('Build-Release.ps1')
for marker in ('html-browser-print-compatibility', '$status.pdfMode -like "html-browser-print*"'):
    if marker not in release:
        errors.append(f'Build-Release.ps1 missing compatibility PDF validation marker: {marker}')

# Existing compiler-regression repairs remain present.
req('src/LocalGPT/Services/Localization/LocalGptLocalizationService.cs', 'using LocalGPT.Interfaces;')
req('src/LocalGPT/Services/Formatting/ChatContentRenderer.cs', 'using LocalGPT.BusinessObjects;')
req('src/LocalGPT/Services/ThemeService.cs', 'public int MaxFusionRouteSteps')
req('src/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs', 'Themes.MaxFusionRouteSteps')
forbid('src/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs', 'ThemeService.MaxFusionRouteSteps')
req('src/LocalGPT/Services/HumanCollaborationService.cs', 'private int MaxTextLength =>')

# Windows host coordination is optional, fail-safe, and delegates Linux to WSL only when ready.
build = text('Build-Release.ps1')
for marker in (
    '[ValidateSet("Auto", "Off", "Require")]',
    '[string]$WslLinux = "Auto"',
    '[switch]$ProvisionWslBuildTools',
    '[switch]$WslChildBuild',
    '[switch]$SkipReleaseBundle',
    '[string]$PreparedDocumentationRoot = ""',
    "Ready WSL Linux backend '$wslResolvedDistribution' will build:",
    "if ($Runtime -eq 'all') {",
    "@('linux-x64','linux-arm64')",
    "Continuing with the normal Windows release only.",
    "build/Invoke-WslLinuxRelease.ps1",
    '-PreparedDocumentationRoot $documentationCacheRoot',
    '-SkipDocumentationNodeProvisioning:($WslChildBuild -and -not [string]::IsNullOrWhiteSpace($PreparedDocumentationRoot))',
):
    if marker not in build:
        errors.append(f'Build-Release.ps1 missing WSL release marker: {marker}')

for rel in (
    'Setup-WslLinuxBuild.ps1', 'Setup-WslLinuxBuild.cmd',
    'build/WslRelease.Common.ps1', 'build/Invoke-WslLinuxRelease.ps1',
    'build/wsl/Invoke-LinuxRelease.sh', 'build/wsl/Provision-WslLinuxBuild.sh',
    'docs/engineering/wsl-linux-release.md',
):
    text(rel)

common = text('build/WslRelease.Common.ps1')
for marker in (
    "Resolve-WslReleaseDistribution",
    "docker-desktop",
    "printf 'wsl2=0", 
    "DevExpress_License/w",
    "DevExpress_LicensePath/pw",
    "WSL2 (convert the distro with wsl.exe --set-version <name> 2)",
):
    if marker not in common:
        errors.append(f'WslRelease.Common.ps1 missing marker: {marker}')
for forbidden in ('DevExpress_License/u', 'DevExpress_LicensePath/pu'):
    if forbidden in common:
        errors.append(f'WslRelease.Common.ps1 contains wrong-direction WSLENV bridge: {forbidden}')

invoke = text('build/Invoke-WslLinuxRelease.ps1')
for marker in (
    "WSL distribution '$distro' is not release-ready",
    "Setup-WslLinuxBuild.ps1 -Provision",
    "foreach ($mode in @('full','light'))",
    "foreach ($extension in @('.tar.gz','.deb'))",
    "wsl.exe is not installed or not available on PATH",
    "--terminate $distro",
):
    if marker not in invoke:
        errors.append(f'Invoke-WslLinuxRelease.ps1 missing marker: {marker}')

child = text('build/wsl/Invoke-LinuxRelease.sh')
for marker in (
    'mktemp -d "$cache_parent/wsl-release-XXXXXXXX"',
    "--exclude='./**/bin'",
    '-WslChildBuild',
    '-SkipReleaseBundle',
    '-PreparedDocumentationRoot "$docs"',
    'APPIMAGE_EXTRACT_AND_RUN=1',
):
    if marker not in child:
        errors.append(f'Invoke-LinuxRelease.sh missing marker: {marker}')

provision = text('build/wsl/Provision-WslLinuxBuild.sh')
for marker in (
    'ubuntu|debian', 'dotnet-sdk-10.0', 'powershell', 'python3', 'rpm',
    'appimagetool-${appimage_arch}.AppImage', '~/.local/bin',
):
    if marker not in provision:
        errors.append(f'Provision-WslLinuxBuild.sh missing marker: {marker}')
forbid('build/wsl/Provision-WslLinuxBuild.sh', 'docker', 'WSL provisioning must not require Docker.')
forbid('build/wsl/Provision-WslLinuxBuild.sh', 'podman', 'WSL provisioning must not require Podman.')

native = text('build/NativeReleasePackaging.ps1')
for marker in (
    '$env:ARCH = $appImageArch',
    "$env:APPIMAGE_EXTRACT_AND_RUN = '1'",
    '$rpmTarget = "$Architecture-unknown-linux"',
    '& $rpmbuild --target $rpmTarget',
    '[switch]$RequireOptionalPackages',
):
    if marker not in native:
        errors.append(f'NativeReleasePackaging.ps1 missing Linux packaging marker: {marker}')

# Managed packaging handle-lifetime repair and package version stay stable.
prog = 'src/LocalGPT.ReleasePackaging/Program.cs'
req(prog, 'CommitTemporaryFile(temp, outputPath);')
req(prog, 'CommitTemporaryFile(temp, output);')
forbid(prog, 'using var file = new FileStream(temp, FileMode.CreateNew')
forbid(prog, 'using var stream = new FileStream(temp, FileMode.CreateNew')
req('src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj', '<Version>1.0.1</Version>')

# Explicit InteractiveServer boundaries are unchanged.
for rel in ('Components/Pages/Chat.razor','Components/Pages/Database.razor','Components/Pages/Help.razor','Components/Pages/ModelCouncil.razor'):
    req('src/LocalGPT/' + rel, '@rendermode InteractiveServer', f'InteractiveServer boundary missing: {rel}')

if any(x > 9 for x in (5, 8)):
    errors.append('version violates one-digit minor/patch policy')
if errors:
    print('LocalGPT 3.5.8 static release audit FAILED:')
    for error in errors:
        print(' -', error)
    raise SystemExit(1)
print('LocalGPT 3.5.8 static release audit passed.')
