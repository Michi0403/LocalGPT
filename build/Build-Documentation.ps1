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

$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$AssemblyPath = [IO.Path]::GetFullPath($AssemblyPath)
$XmlDocumentationPath = [IO.Path]::GetFullPath($XmlDocumentationPath)
if (-not [string]::IsNullOrWhiteSpace($OutputWebRoot)) {
    $OutputWebRoot = [IO.Path]::GetFullPath($OutputWebRoot)
}

Write-Host "LocalGPT documentation input: repository=$RepositoryRoot; assembly=$AssemblyPath; xml=$XmlDocumentationPath; version=$Version"

$docsRoot = Join-Path $RepositoryRoot "docs"
$inputRoot = Join-Path $docsRoot "input"
$siteRoot = Join-Path $docsRoot "_site"
$sourceWebRoot = Join-Path $RepositoryRoot "LocalGPTWebviewWrapper\LocalGPT\wwwroot\help-docs"
$configPath = Join-Path $docsRoot "docfx.json"
$manifestPath = Join-Path $RepositoryRoot ".config\dotnet-tools.json"
$fallbackToolRoot = Join-Path $docsRoot ".tools"
$pdfName = "LocalGPT-$Version.pdf"

if (-not (Test-Path -LiteralPath $AssemblyPath)) { throw "Documentation assembly was not found: $AssemblyPath" }
if (-not (Test-Path -LiteralPath $XmlDocumentationPath)) { throw "XML documentation file was not found: $XmlDocumentationPath" }
if (-not (Test-Path -LiteralPath $configPath)) { throw "DocFX configuration was not found: $configPath" }
if (-not (Test-Path -LiteralPath $manifestPath)) { throw "DocFX tool manifest was not found: $manifestPath" }

# Source ZIP downloads can carry the Windows Zone.Identifier alternate stream into every extracted file.
# Unblock only the repository-local documentation inputs that the current build is about to execute/read.
@($manifestPath, $configPath, $PSCommandPath) | ForEach-Object {
    if (Test-Path -LiteralPath $_) {
        Unblock-File -LiteralPath $_ -ErrorAction SilentlyContinue
    }
}
Get-ChildItem -LiteralPath (Split-Path -Parent $manifestPath) -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
    Unblock-File -LiteralPath $_.FullName -ErrorAction SilentlyContinue
}
Get-ChildItem -LiteralPath $docsRoot -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
    Unblock-File -LiteralPath $_.FullName -ErrorAction SilentlyContinue
}

$publishRoots = [System.Collections.Generic.List[string]]::new()
$publishRoots.Add([IO.Path]::GetFullPath($sourceWebRoot))
if (-not [string]::IsNullOrWhiteSpace($OutputWebRoot)) {
    $resolvedOutputWebRoot = [IO.Path]::GetFullPath($OutputWebRoot)
    if (-not $publishRoots.Contains($resolvedOutputWebRoot)) { $publishRoots.Add($resolvedOutputWebRoot) }
}

New-Item -ItemType Directory -Path $inputRoot -Force | Out-Null
Remove-Item -LiteralPath $siteRoot -Recurse -Force -ErrorAction SilentlyContinue
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
Unblock-File -LiteralPath $configPath -ErrorAction SilentlyContinue

$useManifestTool = $false
$docfxExecutable = $null

function Invoke-LocalGptDocfx {
    param([Parameter(Mandatory)][string[]]$Arguments)
    if ($script:useManifestTool) {
        # dotnet tool run resolves the restored local tool from the manifest in repository scope.
        & dotnet tool run docfx @Arguments
    }
    else {
        & $script:docfxExecutable @Arguments
    }
    return $LASTEXITCODE
}

Push-Location $RepositoryRoot
try {
    & dotnet tool restore --tool-manifest $manifestPath
    if ($LASTEXITCODE -eq 0) {
        $useManifestTool = $true
    }
    else {
        Write-Warning "Repository-local DocFX tool restore failed. Trying an isolated tool-path installation."
        New-Item -ItemType Directory -Path $fallbackToolRoot -Force | Out-Null
        Get-ChildItem -LiteralPath $fallbackToolRoot -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
            Unblock-File -LiteralPath $_.FullName -ErrorAction SilentlyContinue
        }
        $docfxExecutable = Get-ChildItem -LiteralPath $fallbackToolRoot -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -in @("docfx", "docfx.exe") } |
            Select-Object -First 1 -ExpandProperty FullName
        if ([string]::IsNullOrWhiteSpace($docfxExecutable)) {
            & dotnet tool install docfx --tool-path $fallbackToolRoot --version 2.78.5
            if ($LASTEXITCODE -eq 0) {
                $docfxExecutable = Get-ChildItem -LiteralPath $fallbackToolRoot -Recurse -File -ErrorAction SilentlyContinue |
                    Where-Object { $_.Name -in @("docfx", "docfx.exe") } |
                    Select-Object -First 1 -ExpandProperty FullName
            }
        }
        if ([string]::IsNullOrWhiteSpace($docfxExecutable)) {
            $message = "DocFX restore failed through both the repository manifest and isolated tool path. The application build remains usable, but documentation was not regenerated."
            if ($RequirePdf) { throw $message }
            Write-Warning $message
            foreach ($publishRoot in $publishRoots) {
                New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
                [ordered]@{
                    version = $Version
                    generatedAtUtc = [DateTime]::UtcNow.ToString("O")
                    htmlAvailable = Test-Path -LiteralPath (Join-Path $publishRoot "index.html")
                    pdfAvailable = Test-Path -LiteralPath (Join-Path $publishRoot $pdfName)
                    pdfFileName = $pdfName
                    xmlDocumentationFileName = "LocalGPT.xml"
                    docfxVersion = "2.78.5"
                    toolSource = "unavailable"
                    warning = $message
                } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $publishRoot "documentation-status.json") -Encoding utf8
            }
            return
        }
    }

    if ((Invoke-LocalGptDocfx -Arguments @("metadata", $configPath)) -ne 0) { throw "DocFX API metadata generation failed." }
    if ((Invoke-LocalGptDocfx -Arguments @("build", $configPath)) -ne 0) { throw "DocFX HTML/API documentation build failed." }

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
    elseif ((Invoke-LocalGptDocfx -Arguments @("pdf", $configPath)) -ne 0) {
        if ($RequirePdf) { throw "DocFX PDF generation failed." }
        Write-Warning "DocFX PDF generation failed; HTML/API documentation remains available."
    }
}
finally {
    Pop-Location
}

$pdf = Get-ChildItem -LiteralPath $siteRoot -Filter "*.pdf" -File -Recurse -ErrorAction SilentlyContinue |
    Sort-Object Length -Descending |
    Select-Object -First 1

# Publish only after HTML generation succeeded, so a failed tool restore never destroys the previous help site.
foreach ($publishRoot in $publishRoots) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
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
        toolSource = if ($useManifestTool) { "manifest" } else { "isolated-tool-path" }
    }
    $status | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $publishRoot "documentation-status.json") -Encoding utf8
}

Write-Host "LocalGPT documentation generated for version $Version." -ForegroundColor Green
