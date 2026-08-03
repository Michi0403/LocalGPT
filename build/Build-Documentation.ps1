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
$tocPath = Join-Path $docsRoot "toc.yml"
$pdfCoverPath = Join-Path $docsRoot "pdf-cover.html"
$manifestPath = Join-Path $RepositoryRoot ".config\dotnet-tools.json"
$fallbackToolRoot = Join-Path $docsRoot ".tools"
$pdfName = "LocalGPT-$Version.pdf"
$warnings = [System.Collections.Generic.List[string]]::new()
$documentationMode = "static-fallback"
$pdfMode = "fallback-index"
$toolSource = "unavailable"
$apiYamlCount = 0
$apiHtmlCount = 0
$articleSourceCount = @(
    Get-ChildItem -LiteralPath $docsRoot -Filter "*.md" -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object {
            $_.FullName -notlike "$apiRoot\*" -and
            $_.FullName -notlike "$inputRoot\*" -and
            $_.FullName -notlike "$siteRoot\*" -and
            $_.FullName -notlike "$fallbackToolRoot\*"
        }
).Count
$pdfFileSize = 0

if (-not (Test-Path -LiteralPath $AssemblyPath)) { throw "Documentation assembly was not found: $AssemblyPath" }
if (-not (Test-Path -LiteralPath $XmlDocumentationPath)) { throw "XML documentation file was not found: $XmlDocumentationPath" }
if (-not (Test-Path -LiteralPath $docsRoot)) { throw "Documentation source directory was not found: $docsRoot" }

function ConvertTo-LocalGptXmlText {
    param([AllowNull()][object]$Node)
    if ($null -eq $Node) { return "" }
    $text = [string]$Node.InnerText
    return ([regex]::Replace($text, '\s+', ' ')).Trim()
}

function ConvertTo-LocalGptHtml {
    param([AllowEmptyString()][string]$Text)
    return [System.Net.WebUtility]::HtmlEncode($Text)
}

function Get-LocalGptHtmlPage {
    param(
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Body,
        [string]$RelativePrefix = ""
    )
    $safeTitle = ConvertTo-LocalGptHtml $Title
    return @"
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>$safeTitle</title>
<link rel="stylesheet" href="${RelativePrefix}styles.css" />
</head>
<body>
<header><a href="${RelativePrefix}index.html">LocalGPT $Version documentation</a></header>
<main>$Body</main>
<footer>Generated $([DateTime]::UtcNow.ToString("u")) · LocalGPT $Version</footer>
</body>
</html>
"@
}

function Convert-LocalGptMarkdownToHtml {
    param([Parameter(Mandatory)][string]$Markdown)
    $lines = @($Markdown -split '\r?\n')
    $html = [System.Collections.Generic.List[string]]::new()
    $inCode = $false
    $inList = $false
    foreach ($line in $lines) {
        if ($line.Trim().StartsWith('```')) {
            if ($inList) { $html.Add("</ul>"); $inList = $false }
            if ($inCode) { $html.Add("</code></pre>"); $inCode = $false }
            else { $html.Add("<pre><code>"); $inCode = $true }
            continue
        }
        $encoded = ConvertTo-LocalGptHtml $line
        if ($inCode) { $html.Add($encoded); continue }
        if ($line -match '^(#{1,4})\s+(.+)$') {
            if ($inList) { $html.Add("</ul>"); $inList = $false }
            $level = $matches[1].Length
            $html.Add("<h$level>$(ConvertTo-LocalGptHtml $matches[2])</h$level>")
        }
        elseif ($line -match '^\s*[-*]\s+(.+)$') {
            if (-not $inList) { $html.Add("<ul>"); $inList = $true }
            $html.Add("<li>$(ConvertTo-LocalGptHtml $matches[1])</li>")
        }
        elseif ([string]::IsNullOrWhiteSpace($line)) {
            if ($inList) { $html.Add("</ul>"); $inList = $false }
        }
        else {
            if ($inList) { $html.Add("</ul>"); $inList = $false }
            $html.Add("<p>$encoded</p>")
        }
    }
    if ($inList) { $html.Add("</ul>") }
    if ($inCode) { $html.Add("</code></pre>") }
    return ($html -join [Environment]::NewLine)
}

function New-LocalGptStaticDocumentation {
    param(
        [Parameter(Mandatory)][string]$XmlPath,
        [Parameter(Mandatory)][string]$Destination
    )

    Remove-Item -LiteralPath $Destination -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $Destination "articles") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $Destination "api") -Force | Out-Null

    $styles = @"
:root{color-scheme:dark;--bg:#1f1f1f;--panel:#303030;--text:#f4f4f4;--muted:#b4b4bd;--accent:#c22cf2;--line:#505058}*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:16px/1.55 system-ui,Segoe UI,sans-serif}header,footer{padding:1rem 3vw;background:#171717;border-bottom:1px solid var(--line)}footer{border-top:1px solid var(--line);border-bottom:0;color:var(--muted)}main{max-width:1500px;margin:auto;padding:2rem 3vw}a{color:#e48cff}section,.card{background:var(--panel);border:1px solid var(--line);border-radius:.8rem;padding:1rem;margin:1rem 0}pre{white-space:pre-wrap;overflow-wrap:anywhere;background:#18181b;padding:1rem;border-radius:.5rem}code{overflow-wrap:anywhere}table{width:100%;border-collapse:collapse}td,th{padding:.55rem;border-bottom:1px solid var(--line);text-align:left}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:1rem}.muted{color:var(--muted)}
"@
    Set-Content -LiteralPath (Join-Path $Destination "styles.css") -Value $styles -Encoding utf8

    [xml]$xml = Get-Content -LiteralPath $XmlPath -Raw
    $members = @($xml.SelectNodes("/doc/members/member"))
    $types = @($members | Where-Object { $_.GetAttribute("name") -like 'T:*' } | Sort-Object { $_.GetAttribute("name") })
    $apiLinks = [System.Collections.Generic.List[string]]::new()
    $typeIndex = 0
    foreach ($type in $types) {
        $typeIndex++
        $typeName = $type.GetAttribute("name").Substring(2)
        $fileName = "type-{0:D5}.html" -f $typeIndex
        $summary = ConvertTo-LocalGptXmlText ($type.SelectSingleNode("summary"))
        $prefixes = @("M:$typeName.", "P:$typeName.", "F:$typeName.", "E:$typeName.")
        $owned = @($members | Where-Object {
            $memberName = $_.GetAttribute("name")
            @($prefixes | Where-Object { $memberName.StartsWith($_, [StringComparison]::Ordinal) }).Count -gt 0
        } | Sort-Object { $_.GetAttribute("name") })
        $rows = [System.Collections.Generic.List[string]]::new()
        foreach ($member in $owned) {
            $memberName = ConvertTo-LocalGptHtml ($member.GetAttribute("name"))
            $memberSummary = ConvertTo-LocalGptHtml (ConvertTo-LocalGptXmlText ($member.SelectSingleNode("summary")))
            $returns = ConvertTo-LocalGptHtml (ConvertTo-LocalGptXmlText ($member.SelectSingleNode("returns")))
            $parameters = @($member.SelectNodes("param"))
            $parameterText = if ($parameters.Count -gt 0) {
                (@($parameters | ForEach-Object { "<li><code>$(ConvertTo-LocalGptHtml ($_.GetAttribute('name')))</code>: $(ConvertTo-LocalGptHtml (ConvertTo-LocalGptXmlText $_))</li>" }) -join "")
            } else { "" }
            $detail = if ($parameterText) { "<ul>$parameterText</ul>" } else { "" }
            if ($returns) { $detail += "<p><strong>Returns:</strong> $returns</p>" }
            $rows.Add("<tr><td><code>$memberName</code></td><td>$memberSummary$detail</td></tr>")
        }
        $body = "<h1><code>$(ConvertTo-LocalGptHtml $typeName)</code></h1><p>$([System.Net.WebUtility]::HtmlEncode($summary))</p><table><thead><tr><th>Member</th><th>Documentation</th></tr></thead><tbody>$($rows -join '')</tbody></table>"
        Set-Content -LiteralPath (Join-Path $Destination "api\$fileName") -Value (Get-LocalGptHtmlPage -Title $typeName -Body $body -RelativePrefix "../") -Encoding utf8
        $apiLinks.Add("<li><a href=`"$fileName`">$(ConvertTo-LocalGptHtml $typeName)</a></li>")
    }
    $apiBody = "<h1>API reference</h1><p class=`"muted`">$($types.Count) documented types and $($members.Count) compiler XML members.</p><ul>$($apiLinks -join '')</ul>"
    Set-Content -LiteralPath (Join-Path $Destination "api\index.html") -Value (Get-LocalGptHtmlPage -Title "API reference" -Body $apiBody -RelativePrefix "../") -Encoding utf8

    $articleLinks = [System.Collections.Generic.List[string]]::new()
    $articleIndex = 0
    $articleFiles = @(
        Get-ChildItem -LiteralPath $docsRoot -Filter "*.md" -File -Recurse -ErrorAction SilentlyContinue |
            Where-Object {
                $_.FullName -ne (Join-Path $docsRoot "index.md") -and
                $_.FullName -notlike "$apiRoot\*" -and
                $_.FullName -notlike "$inputRoot\*" -and
                $_.FullName -notlike "$siteRoot\*" -and
                $_.FullName -notlike "$fallbackToolRoot\*"
            } |
            Sort-Object FullName
    )
    foreach ($article in $articleFiles) {
        $articleIndex++
        $name = [IO.Path]::GetFileNameWithoutExtension($article.Name)
        $target = "article-{0:D3}.html" -f $articleIndex
        $markdown = Get-Content -LiteralPath $article.FullName -Raw
        $body = Convert-LocalGptMarkdownToHtml $markdown
        Set-Content -LiteralPath (Join-Path $Destination "articles\$target") -Value (Get-LocalGptHtmlPage -Title $name -Body $body -RelativePrefix "../") -Encoding utf8
        $articleLinks.Add("<article class=`"card`"><h2><a href=`"articles/$target`">$(ConvertTo-LocalGptHtml $name)</a></h2></article>")
    }

    $indexBody = @"
<h1>LocalGPT $Version documentation</h1>
<p>This site was generated from maintained articles and compiler XML comments. DocFX was unavailable or rejected the current metadata graph, so LocalGPT published its deterministic static fallback instead.</p>
<section><h2>Documentation chapters</h2><div class="grid">$($articleLinks -join '')</div></section>
<section><h2>API reference</h2><p><a href="api/index.html">Browse $($types.Count) documented types and $($members.Count) XML members</a>.</p></section>
<section><h2>Build information</h2><p>Version $Version · generated $([DateTime]::UtcNow.ToString("u"))</p></section>
"@
    Set-Content -LiteralPath (Join-Path $Destination "index.html") -Value (Get-LocalGptHtmlPage -Title "LocalGPT $Version documentation" -Body $indexBody) -Encoding utf8
    Copy-Item -LiteralPath $XmlPath -Destination (Join-Path $Destination "LocalGPT.xml") -Force
}

function Escape-LocalGptPdfText {
    param([Parameter(Mandatory)][string]$Text)
    return $Text.Replace('\', '\\').Replace('(', '\(').Replace(')', '\)')
}

function New-LocalGptFallbackPdf {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][int]$XmlMemberCount
    )
    $lines = @(
        "LocalGPT $Version Documentation",
        "Deterministic fallback documentation index",
        "Generated: $([DateTime]::UtcNow.ToString('u'))",
        "Compiler XML members: $XmlMemberCount",
        "The complete searchable documentation is available in index.html.",
        "This PDF is intentionally dependency-free so release publishing is not blocked by DocFX or Node.js."
    )
    $streamLines = [System.Collections.Generic.List[string]]::new()
    $streamLines.Add("BT")
    $streamLines.Add("/F1 18 Tf")
    $streamLines.Add("72 760 Td")
    $first = $true
    foreach ($line in $lines) {
        if (-not $first) { $streamLines.Add("0 -30 Td") }
        $streamLines.Add("($(Escape-LocalGptPdfText $line)) Tj")
        $first = $false
    }
    $streamLines.Add("ET")
    $stream = ($streamLines -join "`n") + "`n"
    $streamLength = [Text.Encoding]::ASCII.GetByteCount($stream)
    $objects = @(
        "1 0 obj`n<< /Type /Catalog /Pages 2 0 R >>`nendobj",
        "2 0 obj`n<< /Type /Pages /Kids [3 0 R] /Count 1 >>`nendobj",
        "3 0 obj`n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>`nendobj",
        "4 0 obj`n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>`nendobj",
        "5 0 obj`n<< /Length $streamLength >>`nstream`n$stream" + "endstream`nendobj"
    )
    $pdf = "%PDF-1.4`n"
    $offsets = [System.Collections.Generic.List[int]]::new()
    $offsets.Add(0)
    foreach ($object in $objects) {
        $offsets.Add([Text.Encoding]::ASCII.GetByteCount($pdf))
        $pdf += $object + "`n"
    }
    $xrefOffset = [Text.Encoding]::ASCII.GetByteCount($pdf)
    $pdf += "xref`n0 6`n0000000000 65535 f `n"
    for ($i = 1; $i -le 5; $i++) { $pdf += ("{0:D10} 00000 n `n" -f $offsets[$i]) }
    $pdf += "trailer`n<< /Size 6 /Root 1 0 R >>`nstartxref`n$xrefOffset`n%%EOF`n"
    [IO.File]::WriteAllBytes($Path, [Text.Encoding]::ASCII.GetBytes($pdf))
}

@($manifestPath, $configPath, $PSCommandPath) | ForEach-Object {
    if (Test-Path -LiteralPath $_) { Unblock-File -LiteralPath $_ -ErrorAction SilentlyContinue }
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
if (Test-Path -LiteralPath $indexPath) {
    $index = Get-Content -LiteralPath $indexPath -Raw
    $index = [regex]::Replace($index, '\*\*Version [^*]+\*\*', "**Version $Version**")
    $index = [regex]::Replace($index, 'LocalGPT-[0-9]+\.[0-9]+\.[0-9]+\.pdf', $pdfName)
    Set-Content -LiteralPath $indexPath -Value $index -Encoding utf8
}

if (Test-Path -LiteralPath $tocPath) {
    $toc = Get-Content -LiteralPath $tocPath -Raw
    $toc = [regex]::Replace($toc, '(?m)^pdfFileName:\s*LocalGPT-[^\r\n]+\.pdf\s*$', "pdfFileName: $pdfName")
    Set-Content -LiteralPath $tocPath -Value $toc -Encoding utf8
}

if (Test-Path -LiteralPath $pdfCoverPath) {
    $cover = Get-Content -LiteralPath $pdfCoverPath -Raw
    $cover = [regex]::Replace($cover, 'LocalGPT [0-9]+\.[0-9]+\.[0-9]+ Documentation', "LocalGPT $Version Documentation")
    $cover = [regex]::Replace($cover, 'Version [0-9]+\.[0-9]+\.[0-9]+', "Version $Version")
    Set-Content -LiteralPath $pdfCoverPath -Value $cover -Encoding utf8
}

if (Test-Path -LiteralPath $configPath) {
    $configText = Get-Content -LiteralPath $configPath -Raw
    $configText = [regex]::Replace($configText, '"localgptVersion"\s*:\s*"[^"]+"', '"localgptVersion": "' + $Version + '"')
    $configText = [regex]::Replace($configText, '"_appFooter"\s*:\s*"LocalGPT [^"]+"', '"_appFooter": "LocalGPT ' + $Version + ' · generated documentation"')
    Set-Content -LiteralPath $configPath -Value $configText -Encoding utf8

    $docfxConfig = $configText | ConvertFrom-Json
    if (-not ($docfxConfig.metadata -is [System.Array])) {
        throw "DocFX metadata configuration must be an array for the repository-pinned DocFX version."
    }
    if (@($docfxConfig.build.template) -notcontains "modern") {
        throw "DocFX modern template is required for the LocalGPT documentation site."
    }
    $tocText = Get-Content -LiteralPath $tocPath -Raw
    if ($tocText -notmatch '(?m)^\s*href:\s*api/toc\.yml\s*$') {
        throw "The root DocFX TOC must import api/toc.yml so HTML and PDF include compiler XML API pages."
    }
}

$docfxExecutable = $null
$useManifestTool = $false
function Invoke-LocalGptDocfx {
    param([Parameter(Mandatory)][string[]]$Arguments)

    $output = if ($script:useManifestTool) {
        @(& dotnet tool run docfx @Arguments 2>&1)
    }
    else {
        @(& $script:docfxExecutable @Arguments 2>&1)
    }

    $exitCode = [int]$LASTEXITCODE
    $succeeded = $exitCode -eq 0
    foreach ($entry in $output) {
        $line = [string]$entry
        if (-not $succeeded) {
            # ConsoleToMSBuild treats native lines containing "error:" as MSBuild errors even
            # when this script intentionally handles the failure and publishes a diagnostic fallback.
            $line = [regex]::Replace($line, '(?i)\b(?:fatalerror|error)\s*:', 'diagnostic:')
        }
        Write-Host "[DocFX] $line"
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = @($output | ForEach-Object { [string]$_ })
    }
}

Remove-Item -LiteralPath $siteRoot -Recurse -Force -ErrorAction SilentlyContinue

Push-Location $RepositoryRoot
try {
    if ((Test-Path -LiteralPath $manifestPath) -and (Test-Path -LiteralPath $configPath)) {
        & dotnet tool restore --tool-manifest $manifestPath
        if ($LASTEXITCODE -eq 0) {
            $useManifestTool = $true
            $toolSource = "manifest"
        }
        else {
            $warnings.Add("Repository-local DocFX restore failed.")
            New-Item -ItemType Directory -Path $fallbackToolRoot -Force | Out-Null
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
            if (-not [string]::IsNullOrWhiteSpace($docfxExecutable)) { $toolSource = "isolated-tool-path" }
        }
    }

    $docfxAvailable = $useManifestTool -or -not [string]::IsNullOrWhiteSpace($docfxExecutable)
    $metadataSucceeded = $false
    $docfxBuildSucceeded = $false
    if ($docfxAvailable) {
        try {
            Remove-Item -LiteralPath $apiRoot -Recurse -Force -ErrorAction SilentlyContinue
            $metadataResult = Invoke-LocalGptDocfx -Arguments @("metadata", $configPath)
            $apiTocPath = Join-Path $apiRoot "toc.yml"
            $apiYamlCount = @(Get-ChildItem -LiteralPath $apiRoot -Filter "*.yml" -File -Recurse -ErrorAction SilentlyContinue).Count
            $metadataSucceeded = $metadataResult.ExitCode -eq 0 -and (Test-Path -LiteralPath $apiTocPath -PathType Leaf) -and $apiYamlCount -gt 1
            if (-not $metadataSucceeded) {
                $warnings.Add("DocFX metadata extraction did not produce the complete API graph; static XML API pages were generated instead.")
            }
            else {
                $apiIndex = @"
# LocalGPT API reference

This reference is generated from LocalGPT.dll and its side-by-side LocalGPT.xml compiler documentation for version $Version.

The namespace, type, and member pages below are generated by DocFX and are included in the complete versioned PDF.

[Browse all namespaces and types](toc.yml)
"@
                Set-Content -LiteralPath (Join-Path $apiRoot "index.md") -Value $apiIndex -Encoding utf8

                $buildResult = Invoke-LocalGptDocfx -Arguments @("build", $configPath)
                $apiHtmlRoot = Join-Path $siteRoot "api"
                $apiHtmlCount = @(Get-ChildItem -LiteralPath $apiHtmlRoot -Filter "*.html" -File -Recurse -ErrorAction SilentlyContinue).Count
                $docfxBuildSucceeded = $buildResult.ExitCode -eq 0 -and (Test-Path -LiteralPath (Join-Path $siteRoot "index.html") -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $apiHtmlRoot "index.html") -PathType Leaf) -and $apiHtmlCount -gt 1
                if (-not $docfxBuildSucceeded) {
                    $warnings.Add("DocFX site generation did not render the complete API reference; static documentation was generated instead.")
                }
            }
        }
        catch {
            $warnings.Add("DocFX HTML generation raised an exception: $($_.Exception.Message)")
        }
    }
    else {
        $warnings.Add("DocFX was unavailable; deterministic static documentation was generated.")
    }

    if (-not $docfxBuildSucceeded) {
        New-LocalGptStaticDocumentation -XmlPath $XmlDocumentationPath -Destination $siteRoot
        $documentationMode = "static-fallback"
        $apiHtmlCount = @(Get-ChildItem -LiteralPath (Join-Path $siteRoot "api") -Filter "*.html" -File -Recurse -ErrorAction SilentlyContinue).Count
    }
    else {
        $documentationMode = "docfx"
        Copy-Item -LiteralPath $XmlDocumentationPath -Destination (Join-Path $siteRoot "LocalGPT.xml") -Force
    }

    [xml]$xmlForCount = Get-Content -LiteralPath $XmlDocumentationPath -Raw
    $xmlMemberCount = @($xmlForCount.SelectNodes("/doc/members/member")).Count
    $pdfPath = Join-Path $siteRoot $pdfName
    $pdfGenerated = $false
    if ($docfxBuildSucceeded) {
        $node = Get-Command node -ErrorAction SilentlyContinue
        $nodeMajor = 0
        if ($null -ne $node) {
            $nodeVersionText = (& node --version 2>$null) -replace '^v', ''
            [void][int]::TryParse(($nodeVersionText -split '\.')[0], [ref]$nodeMajor)
        }
        if ($null -ne $node -and $nodeMajor -ge 20) {
            try {
                $pdfResult = Invoke-LocalGptDocfx -Arguments @("pdf", $configPath)
                if ($pdfResult.ExitCode -eq 0) {
                    $docfxPdf = Get-Item -LiteralPath $pdfPath -ErrorAction SilentlyContinue
                    if ($null -eq $docfxPdf) {
                        $docfxPdf = Get-Item -LiteralPath (Join-Path $siteRoot "toc.pdf") -ErrorAction SilentlyContinue
                    }
                    if ($null -ne $docfxPdf) {
                        if (-not [string]::Equals($docfxPdf.FullName, $pdfPath, [StringComparison]::OrdinalIgnoreCase)) {
                            Copy-Item -LiteralPath $docfxPdf.FullName -Destination $pdfPath -Force
                        }
                        $resolvedPdf = Get-Item -LiteralPath $pdfPath -ErrorAction SilentlyContinue
                        $pdfGenerated = $null -ne $resolvedPdf -and $resolvedPdf.Length -gt 4096
                        if ($pdfGenerated) {
                            $pdfFileSize = $resolvedPdf.Length
                            $pdfMode = "docfx"
                        }
                    }
                }
                if (-not $pdfGenerated) {
                    $warnings.Add("DocFX PDF generation completed without a usable complete versioned PDF.")
                }
            }
            catch { $warnings.Add("DocFX PDF generation failed: $($_.Exception.Message)") }
        }
        else { $warnings.Add("Node.js 20 or later was unavailable; complete DocFX PDF generation was skipped.") }
    }

    if (-not $pdfGenerated -and -not $RequirePdf) {
        New-LocalGptFallbackPdf -Path $pdfPath -XmlMemberCount $xmlMemberCount
        $resolvedPdf = Get-Item -LiteralPath $pdfPath -ErrorAction SilentlyContinue
        $pdfGenerated = $null -ne $resolvedPdf
        if ($pdfGenerated) { $pdfFileSize = $resolvedPdf.Length }
        $pdfMode = "fallback-index"
    }

    if ($RequirePdf -and (-not $pdfGenerated -or $pdfMode -ne "docfx")) {
        throw "Complete DocFX PDF generation failed. Verify DocFX metadata, the root TOC, and Node.js 20 or later."
    }
}
finally {
    Pop-Location
    Remove-Item -LiteralPath $inputRoot -Recurse -Force -ErrorAction SilentlyContinue
}

foreach ($publishRoot in $publishRoots) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
    Copy-Item -Path (Join-Path $siteRoot "*") -Destination $publishRoot -Recurse -Force
    $status = [ordered]@{
        version = $Version
        generatedAtUtc = [DateTime]::UtcNow.ToString("O")
        htmlAvailable = Test-Path -LiteralPath (Join-Path $publishRoot "index.html")
        pdfAvailable = Test-Path -LiteralPath (Join-Path $publishRoot $pdfName)
        pdfFileName = $pdfName
        xmlDocumentationFileName = "LocalGPT.xml"
        documentationMode = $documentationMode
        pdfMode = $pdfMode
        docfxVersion = "2.78.5"
        toolSource = $toolSource
        articleSourceCount = $articleSourceCount
        xmlMemberCount = $xmlMemberCount
        apiYamlCount = $apiYamlCount
        apiHtmlCount = $apiHtmlCount
        pdfBytes = $pdfFileSize
        completeApiReference = $documentationMode -eq "docfx" -and $apiYamlCount -gt 1 -and $apiHtmlCount -gt 1
        warnings = @($warnings)
    }
    $statusPath = Join-Path $publishRoot "documentation-status.json"
    $status | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statusPath -Encoding utf8

    $requiredArtifacts = @(
        (Join-Path $publishRoot "index.html"),
        (Join-Path $publishRoot "LocalGPT.xml"),
        (Join-Path $publishRoot $pdfName),
        $statusPath
    )
    foreach ($requiredArtifact in $requiredArtifacts) {
        if (-not (Test-Path -LiteralPath $requiredArtifact -PathType Leaf)) {
            throw "Documentation generation did not produce the required artifact: $requiredArtifact"
        }
    }
}

Write-Host "LocalGPT documentation generated for version $Version using $documentationMode; PDF mode: $pdfMode." -ForegroundColor Green
