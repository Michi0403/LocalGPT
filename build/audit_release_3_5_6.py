#!/usr/bin/env python3
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
errors=[]
def text(rel):
    p=ROOT/rel
    if not p.is_file(): errors.append(f'missing file: {rel}'); return ''
    return p.read_text(encoding='utf-8-sig',errors='replace')
def req(rel,needle,msg=None):
    if needle not in text(rel): errors.append(msg or f'{rel} missing: {needle}')
def forbid(rel,needle,msg=None):
    if needle in text(rel): errors.append(msg or f'{rel} contains forbidden: {needle}')

for rel in ('src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj'):
    req(rel,'<Version>3.5.6</Version>')
req('docs/docfx.json','"localgptVersion": "3.5.6"')
req('docs/pdf/toc.yml','LocalGPT-3.5.6.pdf')
req('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=3.5.6')
req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/3.5.6')
req('RELEASE.md','CHANGELOG-v3.5.6-MACOS-LINUX-HOMEBREW-RELEASE.md')
req('RELEASE.md','VALIDATION-v3.5.6-source.md')
text('CHANGELOG-v3.5.6-MACOS-LINUX-HOMEBREW-RELEASE.md'); text('VALIDATION-v3.5.6-source.md')

# Existing compiler-regression repairs remain present.
req('src/LocalGPT/Services/Localization/LocalGptLocalizationService.cs','using LocalGPT.Interfaces;')
req('src/LocalGPT/Services/Formatting/ChatContentRenderer.cs','using LocalGPT.BusinessObjects;')
req('src/LocalGPT/Services/ThemeService.cs','public int MaxFusionRouteSteps')
req('src/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs','Themes.MaxFusionRouteSteps')
forbid('src/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs','ThemeService.MaxFusionRouteSteps')
req('src/LocalGPT/Services/HumanCollaborationService.cs','private int MaxTextLength =>')

build=text('Build-Release.ps1')
for marker in (
    '"all-rids"',
    'function Get-ReleaseHostFamily',
    'function Get-HostDefaultRuntimes',
    "return @('win-x64', 'win-x86', 'win-arm64')",
    "return @('linux-x64', 'linux-arm64')",
    "return @('osx-x64', 'osx-arm64', 'linux-x64', 'linux-arm64')",
    '[switch]$UseContainerPackaging',
    '[switch]$ProvisionNativePackagingTools',
    '[switch]$RequireOptionalNativePackages',
    'macOS host release also includes Linux x64/ARM64 payloads',
    '-ProvisionHomebrewTools:$ProvisionNativePackagingTools',
    '-RequireOptionalPackages:$RequireOptionalNativePackages',
):
    if marker not in build: errors.append(f'Build-Release.ps1 missing macOS/Linux release marker: {marker}')

native=text('build/NativeReleasePackaging.ps1')
for marker in (
    'function Resolve-HomebrewFormulaExecutable',
    "Resolve-HomebrewFormulaExecutable 'rpmbuild' 'rpm'",
    "& $brew install rpm",
    '[switch]$ProvisionHomebrewTools',
    '[switch]$RequireOptionalPackages',
    'function Complete-OptionalPackageFailure',
    '$rpmTarget = "$Architecture-unknown-linux"',
    '& $rpmbuild --target $rpmTarget',
    "'linux/arm64'",
    "'linux/amd64'",
    'AppImage is Linux-only. macOS can cross-publish the Linux payload',
    'Set-UnixExecutable (Join-Path $resources $ExecutableName)',
    "Write-Utf8NoBom (Join-Path $app 'Contents/Info.plist') $infoPlist",
):
    if marker not in native: errors.append(f'NativeReleasePackaging.ps1 missing cross-host packaging marker: {marker}')
for forbidden in (
    "Skipping RPM for $Rid. RPM is a native Linux packaging step and this host is not Linux.",
    "The native RPM step is limited to the current host architecture",
    'RPM packaging needs rpmbuild, Docker, or Podman.',
    'AppImage needs appimagetool, Docker, or Podman.',
):
    if forbidden in native: errors.append(f'NativeReleasePackaging.ps1 retains obsolete hard restriction: {forbidden}')

# Managed packaging handle-lifetime repair and package version stay stable.
prog='src/LocalGPT.ReleasePackaging/Program.cs'
req(prog,'CommitTemporaryFile(temp, outputPath);'); req(prog,'CommitTemporaryFile(temp, output);')
forbid(prog,'using var file = new FileStream(temp, FileMode.CreateNew')
forbid(prog,'using var stream = new FileStream(temp, FileMode.CreateNew')
req('src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj','<Version>1.0.1</Version>')

# InteractiveServer boundaries stay explicit.
for rel in ('Components/Pages/Chat.razor','Components/Pages/Database.razor','Components/Pages/Help.razor','Components/Pages/ModelCouncil.razor'):
    req('src/LocalGPT/'+rel,'@rendermode InteractiveServer',f'InteractiveServer boundary missing: {rel}')

if any(x>9 for x in (5,6)): errors.append('version violates one-digit minor/patch policy')
if errors:
    print('LocalGPT 3.5.6 static release audit FAILED:')
    for e in errors: print(' -',e)
    raise SystemExit(1)
print('LocalGPT 3.5.6 static release audit passed.')
