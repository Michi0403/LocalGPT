param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [ValidateSet('localgpt','publisherstudio')]
    [string]$Product
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Product)) {
    if (Test-Path -LiteralPath (Join-Path $RepositoryRoot 'src/LocalGPT') -PathType Container) {
        $Product = 'localgpt'
    }
    elseif (Test-Path -LiteralPath (Join-Path $RepositoryRoot 'src/PublisherStudio.Web') -PathType Container) {
        $Product = 'publisherstudio'
    }
    else {
        throw "Transient UI-state ownership guard could not determine the product under $RepositoryRoot."
    }
}