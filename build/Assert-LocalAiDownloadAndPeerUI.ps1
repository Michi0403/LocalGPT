# Build-blocking contract: owned download handles, immutable archives, UI peer keys and DevExpress search.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$down = Get-Content -LiteralPath (Join-Path $root 'src/LocalGPT/Services/LocalAiAcquisitionService.cs') -Raw
$ui = Get-Content -LiteralPath (Join-Path $root 'src/LocalGPT/Components/Pages/OneWireSecurity.razor') -Raw
$install = Get-Content -LiteralPath (Join-Path $root 'src/LocalGPT/Components/Pages/Install.razor') -Raw
$installCode = Get-Content -LiteralPath (Join-Path $root 'src/LocalGPT/Components/Pages/Install.razor.cs') -Raw
$failures = @()
if ($down -notmatch 'await using \(var output = new FileStream' -or $down -notmatch 'File\.Move\(temporaryPath, archivePath\)' -or $down -notmatch 'sha256-') { $failures += 'LOCALAI001: downloads must publish only closed, content-addressed streams.' }
if ($down.IndexOf('await output.FlushAsync') -gt $down.IndexOf('File.Move(temporaryPath, archivePath)')) { $failures += 'LOCALAI002: archive must not move before flushing/disposal.' }
if ($ui -notmatch 'GroupBy\(item => item.PeerId, StringComparer.OrdinalIgnoreCase\)' -or $ui -notmatch 'GroupBy\(peer => peer.PeerId, StringComparer.OrdinalIgnoreCase\)') { $failures += 'ONEWIRE001: peer lists must deduplicate stable identities before keyed rendering.' }
if ($ui -notmatch '@key="\(peer\.PeerId, true\)"' -or $ui -notmatch '@key="\(peer\.PeerId, false\)"') { $failures += 'ONEWIRE002: live and trusted peer rows must use different sibling render keys even when they have the same PeerId.' }
if ($install -notmatch 'ShowSearchBox="true"' -or $install -notmatch 'ShowFilterRow="true"' -or $install -notmatch 'FieldName="Downloads"' -or $install -notmatch 'FieldName="Likes"') { $failures += 'LOCALAI003: sortable full-text DevExpress model results grid is missing.' }
if ($installCode -notmatch 'ProbeLocalAiRuntimeOnOpen' -or $installCode -notmatch 'RefreshToolchainsOnOpen') { $failures += 'LOCALAI004: opening local AI / toolchain panes must trigger a supervised refresh.' }
if ($failures.Count -gt 0) { throw ($failures -join [Environment]::NewLine) }
Write-Host 'LOCALAI: stream ownership, content-addressed downloads, peer keys, model grid and pane refresh verified.'
