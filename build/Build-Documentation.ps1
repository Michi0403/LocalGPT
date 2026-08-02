param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$AssemblyPath,
    [Parameter(Mandatory)][string]$XmlDocumentationPath,
    [Parameter(Mandatory)][string]$Version,
    [string]$OutputWebRoot = "",
    [switch]$RequirePdf
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$docsRoot = Join-Path $RepositoryRoot "docs"
$inputRoot = Join-Path $docsRoot "input"
$siteRoot = Join-Path $docsRoot "_site"
$sourceWebRoot = Join-Path $RepositoryRoot "LocalGPTWebviewWrapper\LocalGPT\wwwroot\help-docs"
$configPath = Join-Path $docsRoot "docfx.json"
$pdfName = "LocalGPT-$Version.pdf"

if (-not (Test-Path -LiteralPath $AssemblyPath)) {
    throw "Documentation assembly was not found: $AssemblyPath"
}
if (-not (Test-Path -LiteralPath $XmlDocumentationPath)) {
    throw "XML documentation file was not found: $XmlDocumentationPath"
}
if (-not (Test-Path -LiteralPath $configPath)) {
    throw "DocFX configuration was not found: $configPath"
}

$publishRoots = [System.Collections.Generic.List[string]]::new()
$publishRoots.Add([IO.Path]::GetFullPath($sourceWebRoot))
if (-not [string]::IsNullOrWhiteSpace($OutputWebRoot)) {
    $resolvedOutputWebRoot = [IO.Path]::GetFullPath($OutputWebRoot)
    if (-not $publishRoots.Contains($resolvedOutputWebRoot)) {
        $publishRoots.Add($resolvedOutputWebRoot)
    }
}

New-Item -ItemType Directory -Path $inputRoot -Force | Out-Null
Remove-Item -LiteralPath $siteRoot -Recurse -Force -ErrorAction SilentlyContinue
foreach ($publishRoot in $publishRoots) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
}

Copy-Item -LiteralPath $AssemblyPath -Destination (Join-Path $inputRoot "LocalGPT.dll") -Force
Copy-Item -LiteralPath $XmlDocumentationPath -Destination (Join-Path $inputRoot "LocalGPT.xml") -Force

$indexPath = Join-Path $docsRoot "index.md"
$index = Get-Content -LiteralPath $indexPath -Raw
$index = [regex]::Replace($index, '\*\*Version [^*]+\*\*', "**Version $Version**")
$index = [regex]::Replace($index, 'LocalGPT-[0-9]+\.[0-9]+\.[0-9]+\.pdf', $pdfName)
Set-Content -LiteralPath $indexPath -Value $index -Encoding utf8

$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$config.build.globalMetadata.localgptVersion = $Version
$config.build.globalMetadata._appTitle = "LocalGPT $Version Documentation"
$config | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $configPath -Encoding utf8

$pdfGenerated = $false
Push-Location $RepositoryRoot
try {
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed for DocFX." }

    & dotnet tool run docfx metadata $configPath
    if ($LASTEXITCODE -ne 0) { throw "DocFX API metadata generation failed." }

    & dotnet tool run docfx build $configPath
    if ($LASTEXITCODE -ne 0) { throw "DocFX HTML/API documentation build failed." }

    $node = Get-Command node -ErrorAction SilentlyContinue
    $nodeMajor = 0
    if ($null -ne $node) {
        $nodeVersionText = (& node --version 2>$null) -replace '^v', ''
        $nodeMajorText = ($nodeVersionText -split '\.')[0]
        [void][int]::TryParse($nodeMajorText, [ref]$nodeMajor)
    }

    if ($null -eq $node -or $nodeMajor -lt 20) {
        $message = "Node.js 20 or later is required by DocFX PDF generation. HTML/API documentation was generated, but PDF generation was skipped."
        if ($RequirePdf) { throw $message }
        Write-Warning $message
    }
    else {
        & dotnet tool run docfx pdf $configPath
        if ($LASTEXITCODE -ne 0) {
            if ($RequirePdf) { throw "DocFX PDF generation failed." }
            Write-Warning "DocFX PDF generation failed; HTML/API documentation remains available."
        }
        else {
            $pdfGenerated = $true
        }
    }
}
finally {
    Pop-Location
}

$pdf = Get-ChildItem -LiteralPath $siteRoot -Filter "*.pdf" -File -Recurse -ErrorAction SilentlyContinue |
    Sort-Object Length -Descending |
    Select-Object -First 1
if ($null -ne $pdf) {
    $pdfGenerated = $true
}

foreach ($publishRoot in $publishRoots) {
    if (Test-Path -LiteralPath $siteRoot) {
        Copy-Item -Path (Join-Path $siteRoot "*") -Destination $publishRoot -Recurse -Force
    }
    if ($null -ne $pdf) {
        Copy-Item -LiteralPath $pdf.FullName -Destination (Join-Path $publishRoot $pdfName) -Force
    }

    $status = [ordered]@{
        version = $Version
        generatedAtUtc = [DateTime]::UtcNow.ToString("O")
        htmlAvailable = Test-Path -LiteralPath (Join-Path $publishRoot "index.html")
        pdfAvailable = Test-Path -LiteralPath (Join-Path $publishRoot $pdfName)
        pdfFileName = $pdfName
        xmlDocumentationFileName = "LocalGPT.xml"
        docfxVersion = "2.78.5"
        pdfCommandSucceeded = $pdfGenerated
    }
    $status | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $publishRoot "documentation-status.json") -Encoding utf8
}

Write-Host "LocalGPT documentation generated for version $Version." -ForegroundColor Green
