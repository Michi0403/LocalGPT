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
$apiRoot = Join-Path $docsRoot "api"
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

Remove-Item -LiteralPath $inputRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $inputRoot -Force | Out-Null
Remove-Item -LiteralPath $siteRoot -Recurse -Force -ErrorAction SilentlyContinue
$assemblyDirectory = Split-Path -Parent $AssemblyPath
Get-ChildItem -LiteralPath $assemblyDirectory -Filter "*.dll" -File -ErrorAction SilentlyContinue | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $inputRoot $_.Name) -Force
}
Get-ChildItem -LiteralPath $assemblyDirectory -Filter "*.xml" -File -ErrorAction SilentlyContinue | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $inputRoot $_.Name) -Force
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

function ConvertTo-LocalGptMarkdownText {
    param([AllowNull()][object]$Node)
    if ($null -eq $Node) { return "" }
    $text = [string]$Node.InnerText
    return ([regex]::Replace($text, '\s+', ' ')).Trim()
}

function New-LocalGptXmlFallbackApi {
    param(
        [Parameter(Mandatory)][string]$XmlPath,
        [Parameter(Mandatory)][string]$Destination,
        [Parameter(Mandatory)][string]$DocumentationVersion
    )

    Remove-Item -LiteralPath $Destination -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    [xml]$xml = Get-Content -LiteralPath $XmlPath -Raw
    $members = @($xml.SelectNodes("/doc/members/member"))
    $types = @($members | Where-Object { $_.GetAttribute("name") -like 'T:*' } | Sort-Object { $_.GetAttribute("name") })
    $indexLines = [System.Collections.Generic.List[string]]::new()
    $indexLines.Add("# LocalGPT API reference")
    $indexLines.Add("")
    $indexLines.Add("Generated from compiler XML comments for LocalGPT $DocumentationVersion.")
    $indexLines.Add("")

    $typeIndex = 0
    foreach ($type in $types) {
        $typeIndex++
        $typeName = $type.GetAttribute("name").Substring(2)
        $safeName = "type-{0:D5}" -f $typeIndex
        $summary = ConvertTo-LocalGptMarkdownText ($type.SelectSingleNode("summary"))
        $typeLines = [System.Collections.Generic.List[string]]::new()
        $typeLines.Add("# ``$typeName``")
        $typeLines.Add("")
        if (-not [string]::IsNullOrWhiteSpace($summary)) { $typeLines.Add($summary); $typeLines.Add("") }
        $typeLines.Add("## Members")
        $typeLines.Add("")
        $prefixes = @("M:$typeName.", "P:$typeName.", "F:$typeName.", "E:$typeName.")
        $owned = @($members | Where-Object {
            $memberName = $_.GetAttribute("name")
            ($prefixes | Where-Object { $memberName.StartsWith($_, [System.StringComparison]::Ordinal) }).Count -gt 0
        } | Sort-Object { $_.GetAttribute("name") })
        if ($owned.Count -eq 0) {
            $typeLines.Add("No compiler XML member entries were emitted for this type.")
        }
        else {
            foreach ($member in $owned) {
                $memberName = $member.GetAttribute("name")
                $memberSummary = ConvertTo-LocalGptMarkdownText ($member.SelectSingleNode("summary"))
                $typeLines.Add("### ``$memberName``")
                $typeLines.Add("")
                if (-not [string]::IsNullOrWhiteSpace($memberSummary)) { $typeLines.Add($memberSummary) } else { $typeLines.Add("No summary was emitted.") }
                $parameters = @($member.SelectNodes("param"))
                if ($parameters.Count -gt 0) {
                    $typeLines.Add("")
                    $typeLines.Add("**Parameters**")
                    foreach ($parameter in $parameters) {
                        $parameterText = ConvertTo-LocalGptMarkdownText $parameter
                        $typeLines.Add("- ``$($parameter.GetAttribute("name"))`` - $parameterText")
                    }
                }
                $returns = ConvertTo-LocalGptMarkdownText ($member.SelectSingleNode("returns"))
                if (-not [string]::IsNullOrWhiteSpace($returns)) {
                    $typeLines.Add("")
                    $typeLines.Add("**Returns:** $returns")
                }
                $typeLines.Add("")
            }
        }
        Set-Content -LiteralPath (Join-Path $Destination ($safeName + ".md")) -Value $typeLines -Encoding utf8
        $indexLines.Add("- [$typeName]($safeName.md)")
    }

    if ($types.Count -eq 0) {
        $indexLines.Add("No type documentation entries were emitted by the compiler.")
    }
    Set-Content -LiteralPath (Join-Path $Destination "index.md") -Value $indexLines -Encoding utf8
    Write-Warning "DocFX metadata extraction failed; generated a compiler-XML API reference fallback instead."
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

    Remove-Item -LiteralPath $apiRoot -Recurse -Force -ErrorAction SilentlyContinue
    $metadataExitCode = Invoke-LocalGptDocfx -Arguments @("metadata", $configPath)
    if ($metadataExitCode -ne 0) {
        New-LocalGptXmlFallbackApi -XmlPath $XmlDocumentationPath -Destination $apiRoot -DocumentationVersion $Version
    }

    $buildExitCode = Invoke-LocalGptDocfx -Arguments @("build", $configPath)
    if ($buildExitCode -ne 0) {
        $message = "DocFX HTML/API documentation build failed. The application assembly remains usable and the previous published documentation is preserved."
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
                toolSource = if ($useManifestTool) { "manifest" } else { "isolated-tool-path" }
                warning = $message
            } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $publishRoot "documentation-status.json") -Encoding utf8
        }
        return
    }

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
    Remove-Item -LiteralPath $inputRoot -Recurse -Force -ErrorAction SilentlyContinue
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
