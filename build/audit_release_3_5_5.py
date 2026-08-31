#!/usr/bin/env python3
from pathlib import Path
import re
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
    req(rel,'<Version>3.5.5</Version>')
req('docs/docfx.json','"localgptVersion": "3.5.5"')
req('docs/pdf/toc.yml','LocalGPT-3.5.5.pdf')
req('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=3.5.5')
req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/3.5.5')
req('RELEASE.md','CHANGELOG-v3.5.5-MACOS-NATIVE-BUNDLE-PERMISSIONS.md')
req('RELEASE.md','VALIDATION-v3.5.5-source.md')
text('CHANGELOG-v3.5.5-MACOS-NATIVE-BUNDLE-PERMISSIONS.md'); text('VALIDATION-v3.5.5-source.md')

# Existing compiler-regression repairs remain present.
req('src/LocalGPT/Services/Localization/LocalGptLocalizationService.cs','using LocalGPT.Interfaces;')
req('src/LocalGPT/Services/Formatting/ChatContentRenderer.cs','using LocalGPT.BusinessObjects;')
req('src/LocalGPT/Services/ThemeService.cs','public int MaxFusionRouteSteps')
req('src/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs','Themes.MaxFusionRouteSteps')
forbid('src/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs','ThemeService.MaxFusionRouteSteps')
req('src/LocalGPT/Services/HumanCollaborationService.cs','private int MaxTextLength =>')

build=text('Build-Release.ps1')
for marker in ('"all-rids"','function Get-ReleaseHostFamily','function Get-HostDefaultRuntimes',"return @('win-x64', 'win-x86', 'win-arm64')", "return @('linux-x64', 'linux-arm64')", "return @('osx-x64', 'osx-arm64')", '[switch]$UseContainerPackaging'):
    if marker not in build: errors.append(f'Build-Release.ps1 missing host-aware marker: {marker}')

native=text('build/NativeReleasePackaging.ps1')
for marker in ('function Set-UnixExecutable',"& $chmod.Source '0755' $Path",'Set-UnixExecutable (Join-Path $resources $ExecutableName)',"Set-UnixExecutable $Destination","Write-Utf8NoBom (Join-Path $app 'Contents/Info.plist') $infoPlist",'Set-UnixExecutable $appRun','Set-UnixExecutable (Join-Path $appDir $ExecutableName)','Skipping RPM for $Rid','Skipping AppImage for $Rid'):
    if marker not in native: errors.append(f'NativeReleasePackaging.ps1 missing native-mode marker: {marker}')
for forbidden in ('RPM packaging needs rpmbuild, Docker, or Podman.','AppImage needs appimagetool, Docker, or Podman.'):
    if forbidden in native: errors.append(f'optional native packaging still hard-fails: {forbidden}')
# Mac branch must be ahead of Linux branch and not call RPM/AppImage.
mac = native.split("if ($Rid.StartsWith('osx-')) {",1)[1].split("elseif ($Rid.StartsWith('linux-'))",1)[0]
for forbidden in ('New-Rpm','New-AppImage','rpmbuild','appimagetool'):
    if forbidden in mac: errors.append(f'macOS packaging branch unexpectedly contains Linux finisher: {forbidden}')

# Keep managed packaging handle-lifetime repair and package version stable.
prog='src/LocalGPT.ReleasePackaging/Program.cs'
req(prog,'CommitTemporaryFile(temp, outputPath);'); req(prog,'CommitTemporaryFile(temp, output);')
forbid(prog,'using var file = new FileStream(temp, FileMode.CreateNew')
forbid(prog,'using var stream = new FileStream(temp, FileMode.CreateNew')
req('src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj','<Version>1.0.1</Version>')

# InteractiveServer boundaries stay explicit.
for rel in ('Components/Pages/Chat.razor','Components/Pages/Database.razor','Components/Pages/Help.razor','Components/Pages/ModelCouncil.razor'):
    req('src/LocalGPT/'+rel,'@rendermode InteractiveServer',f'InteractiveServer boundary missing: {rel}')

# Version slot policy.
if any(x>9 for x in (5,5)): errors.append('version violates one-digit minor/patch policy')
if errors:
    print('LocalGPT 3.5.5 static release audit FAILED:')
    for e in errors: print(' -',e)
    raise SystemExit(1)
print('LocalGPT 3.5.5 static release audit passed.')
