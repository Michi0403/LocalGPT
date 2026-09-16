$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw $Message }

$root = Split-Path -Parent $PSScriptRoot
$jsPath = Join-Path $root 'docs/templates/localgpt/public/main.js'
$cssPath = Join-Path $root 'docs/templates/localgpt/public/main.css'

if (-not (Test-Path -LiteralPath $jsPath) -or -not (Test-Path -LiteralPath $cssPath)) {
    Fail 'LocalGPT documentation pointer-overlay sources are missing.'
}

$js = Get-Content -LiteralPath $jsPath -Raw
$css = Get-Content -LiteralPath $cssPath -Raw

$requiredJs = @(
    'function ensureKawaiiPointerOverlay()',
    'overlay.className = "localgpt-pointer-overlay";',
    'overlay.appendChild(paw);',
    'overlay.appendChild(trail);',
    'overlay.appendChild(sparkle);',
    'overlay.appendChild(scratch);',
    'overlay.appendChild(pop);'
)
foreach ($marker in $requiredJs) {
    if (-not $js.Contains($marker)) { Fail "LocalGPT documentation pointer-overlay marker is missing: $marker" }
}

$forbiddenJs = @(
    'document.body.appendChild(paw);',
    'document.body.appendChild(trail);',
    'document.body.appendChild(sparkle);',
    'document.body.appendChild(scratch);',
    'document.body.appendChild(pop);'
)
foreach ($marker in $forbiddenJs) {
    if ($js.Contains($marker)) { Fail "LocalGPT transient pointer decoration escaped the viewport overlay: $marker" }
}

$requiredCss = @(
    '.localgpt-pointer-overlay {',
    'contain: strict;',
    'overflow: clip;',
    'position: fixed !important;',
    'body > :not(.localgpt-kawaii-sky):not(.localgpt-pointer-overlay) { position: relative; z-index: 2; }'
)
foreach ($marker in $requiredCss) {
    if (-not $css.Contains($marker)) { Fail "LocalGPT documentation pointer-overlay CSS contract is missing: $marker" }
}

Write-Output 'Documentation pointer-overlay validation passed: transient paw/cursor effects are viewport-contained and excluded from document-flow stacking.'
