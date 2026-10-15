Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$components = Join-Path $root 'src/LocalGPT/Components'
$required = @(
    @{ File='Pages/Install.razor'; Tokens=@('DxHtmlEditor','DxComboBox','DxFormLayoutItem') },
    @{ File='Pages/DxFunctionCatalog.razor'; Tokens=@('DxGrid','DxMemo') },
    @{ File='Shared/LocalGptFormLayoutSurface.razor'; Tokens=@('DxFormLayout','DxFormLayoutItem') }
)
foreach ($item in $required) {
    $file = Join-Path $components $item.File
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing protected DevExpress component: $file" }
    $source = [IO.File]::ReadAllText($file)
    foreach ($token in $item.Tokens) { if (-not $source.Contains($token)) { throw "Protected DevExpress control '$token' missing from $file" } }
}
Write-Host 'PowerShell DevExpress component retention validation passed.'
