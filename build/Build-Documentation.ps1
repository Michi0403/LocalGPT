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
$guideTocPath = Join-Path $docsRoot "guide\toc.yml"
$pdfTocPath = Join-Path $docsRoot "pdf\toc.yml"
$pdfCoverPath = Join-Path $docsRoot "pdf-cover.html"
$manifestPath = Join-Path $RepositoryRoot ".config\dotnet-tools.json"
$fallbackToolRoot = Join-Path $docsRoot ".tools"
$pdfName = "LocalGPT-$Version.pdf"
$warnings = [System.Collections.Generic.List[string]]::new()
$documentationMode = "static-fallback"
$pdfMode = "unavailable"
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
$pdfGeneratedSourcePath = ""
$pdfCandidateCount = 0
$minimumCompletePdfBytes = 65536
$minimumNodeMajor = 20
$provisionedNodeVersion = "22.23.2"
$provisionedNodeArchiveName = "node-v$provisionedNodeVersion-win-x64.zip"
$provisionedNodeArchiveSha256 = "1177b4137ba5adaa56354ae40f1080c7450e8ae09cecb47da459d1c52ac99f97"
$localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
$documentationToolCacheRoot = if ([string]::IsNullOrWhiteSpace($localApplicationData)) {
    Join-Path $fallbackToolRoot "runtime"
}
else {
    Join-Path $localApplicationData "LocalGPT\DocumentationTools"
}
$provisionedNodeRoot = Join-Path $documentationToolCacheRoot "node-v$provisionedNodeVersion-win-x64"
$provisionedNodeExecutable = Join-Path $provisionedNodeRoot "node.exe"
$playwrightBrowserRoot = Join-Path $documentationToolCacheRoot "ms-playwright-docfx-2.78.5"
$nodeVersionUsed = ""
$nodeProvisioned = $false
$pdfTimeoutMilliseconds = 1800000

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

function Get-LocalGptRelativePath {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Path
    )

    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd([char[]]@('\', '/'))
    $pathFull = [IO.Path]::GetFullPath($Path)
    $prefix = $rootFull + [IO.Path]::DirectorySeparatorChar
    if ($pathFull.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        return $pathFull.Substring($prefix.Length)
    }
    return $pathFull
}

function Test-LocalGptCompletePdf {
    param(
        [Parameter(Mandatory)][string]$Path,
        [long]$MinimumBytes = 65536
    )

    $file = Get-Item -LiteralPath $Path -ErrorAction SilentlyContinue
    if ($null -eq $file -or $file.Length -lt $MinimumBytes) { return $false }

    $stream = [IO.File]::OpenRead($file.FullName)
    try {
        $buffer = New-Object byte[] 16384
        $read = $stream.Read($buffer, 0, $buffer.Length)
        if ($read -le 0) { return $false }
        $prefix = [Text.Encoding]::ASCII.GetString($buffer, 0, $read)
        return $prefix.StartsWith("%PDF-", [StringComparison]::Ordinal) -and
            $prefix -notmatch 'Deterministic fallback documentation index'
    }
    finally {
        $stream.Dispose()
    }
}

function Get-LocalGptNodeInfo {
    param(
        [Parameter(Mandatory)][string]$Path,
        [bool]$Provisioned = $false
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    try {
        $versionOutput = @(& $Path --version 2>$null)
        $exitCode = [int]$LASTEXITCODE
        if ($exitCode -ne 0 -or $versionOutput.Count -eq 0) { return $null }

        $versionText = ([string]($versionOutput | Select-Object -First 1)).Trim()
        $versionMatch = [regex]::Match($versionText, '^v?(?<major>\d+)\.')
        if (-not $versionMatch.Success) { return $null }

        $major = [int]$versionMatch.Groups['major'].Value
        if ($major -lt $minimumNodeMajor) { return $null }

        return [pscustomobject]@{
            Path = [IO.Path]::GetFullPath($Path)
            Version = $versionText
            Major = $major
            Provisioned = $Provisioned
        }
    }
    catch {
        return $null
    }
}

function Find-LocalGptNode {
    $candidates = [System.Collections.Generic.List[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($env:PLAYWRIGHT_NODEJS_PATH)) {
        $candidates.Add($env:PLAYWRIGHT_NODEJS_PATH)
    }

    $nodeCommand = Get-Command node -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $nodeCommand) {
        $commandPath = if (-not [string]::IsNullOrWhiteSpace([string]$nodeCommand.Source)) {
            [string]$nodeCommand.Source
        }
        else {
            [string]$nodeCommand.Path
        }
        if (-not [string]::IsNullOrWhiteSpace($commandPath)) { $candidates.Add($commandPath) }
    }

    if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles)) {
        $candidates.Add((Join-Path $env:ProgramFiles "nodejs\node.exe"))
    }
    $programFilesX86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    if (-not [string]::IsNullOrWhiteSpace($programFilesX86)) {
        $candidates.Add((Join-Path $programFilesX86 "nodejs\node.exe"))
    }
    if (-not [string]::IsNullOrWhiteSpace($localApplicationData)) {
        $candidates.Add((Join-Path $localApplicationData "Programs\nodejs\node.exe"))
    }
    $candidates.Add($provisionedNodeExecutable)

    $visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        try {
            $fullPath = [IO.Path]::GetFullPath($candidate)
        }
        catch {
            continue
        }
        if (-not $visited.Add($fullPath)) { continue }
        $isProvisioned = [string]::Equals($fullPath, $provisionedNodeExecutable, [StringComparison]::OrdinalIgnoreCase)
        $nodeInfo = Get-LocalGptNodeInfo -Path $fullPath -Provisioned $isProvisioned
        if ($null -ne $nodeInfo) { return $nodeInfo }
    }

    return $null
}

function Install-LocalGptNode {
    New-Item -ItemType Directory -Path $documentationToolCacheRoot -Force | Out-Null

    $existing = Get-LocalGptNodeInfo -Path $provisionedNodeExecutable -Provisioned $true
    if ($null -ne $existing) { return $existing }

    $archivePath = Join-Path $documentationToolCacheRoot $provisionedNodeArchiveName
    $downloadPath = "$archivePath.download"
    $extractRoot = Join-Path $documentationToolCacheRoot ".node-v$provisionedNodeVersion-extract"
    $downloadUri = "https://nodejs.org/download/release/v$provisionedNodeVersion/$provisionedNodeArchiveName"

    if (Test-Path -LiteralPath $archivePath -PathType Leaf) {
        $archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
        if (-not [string]::Equals($archiveHash, $provisionedNodeArchiveSha256, [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $archivePath -Force
        }
    }

    if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
        Write-Host "Node.js $minimumNodeMajor+ was not found. Downloading verified Node.js $provisionedNodeVersion for DocFX PDF generation..." -ForegroundColor Cyan
        Remove-Item -LiteralPath $downloadPath -Force -ErrorAction SilentlyContinue
        try {
            try {
                [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
            }
            catch { }
            Invoke-WebRequest -Uri $downloadUri -OutFile $downloadPath -UseBasicParsing
            $downloadHash = (Get-FileHash -LiteralPath $downloadPath -Algorithm SHA256).Hash.ToLowerInvariant()
            if (-not [string]::Equals($downloadHash, $provisionedNodeArchiveSha256, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Node.js archive checksum mismatch. Expected $provisionedNodeArchiveSha256 but received $downloadHash."
            }
            Move-Item -LiteralPath $downloadPath -Destination $archivePath -Force
        }
        catch {
            Remove-Item -LiteralPath $downloadPath -Force -ErrorAction SilentlyContinue
            throw "Node.js $provisionedNodeVersion could not be provisioned for the complete DocFX PDF: $($_.Exception.Message)"
        }
    }

    Remove-Item -LiteralPath $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null
    try {
        Expand-Archive -LiteralPath $archivePath -DestinationPath $extractRoot -Force
        $expandedRoot = Join-Path $extractRoot "node-v$provisionedNodeVersion-win-x64"
        $expandedNode = Join-Path $expandedRoot "node.exe"
        if (-not (Test-Path -LiteralPath $expandedNode -PathType Leaf)) {
            throw "The verified Node.js archive did not contain node.exe."
        }

        Remove-Item -LiteralPath $provisionedNodeRoot -Recurse -Force -ErrorAction SilentlyContinue
        Move-Item -LiteralPath $expandedRoot -Destination $provisionedNodeRoot -Force
    }
    finally {
        Remove-Item -LiteralPath $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    $installed = Get-LocalGptNodeInfo -Path $provisionedNodeExecutable -Provisioned $true
    if ($null -eq $installed) {
        throw "The provisioned Node.js runtime could not be executed: $provisionedNodeExecutable"
    }

    return $installed
}

function Resolve-LocalGptNode {
    param([switch]$AllowProvisioning)

    $nodeInfo = Find-LocalGptNode
    if ($null -eq $nodeInfo -and $AllowProvisioning) {
        $nodeInfo = Install-LocalGptNode
    }
    return $nodeInfo
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

if (Test-Path -LiteralPath $pdfTocPath) {
    $pdfToc = Get-Content -LiteralPath $pdfTocPath -Raw
    $pdfToc = [regex]::Replace($pdfToc, '(?m)^pdfFileName:\s*LocalGPT-[^\r\n]+\.pdf\s*$', "pdfFileName: $pdfName")
    Set-Content -LiteralPath $pdfTocPath -Value $pdfToc -Encoding utf8
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
    $metadataConfig = @($docfxConfig.metadata) | Select-Object -First 1
    if ([string]$metadataConfig.namespaceLayout -ne "nested") {
        throw "DocFX namespaceLayout must be nested for the Microsoft Learn-style API hierarchy."
    }
    if ([string]$metadataConfig.memberLayout -ne "separatePages") {
        throw "DocFX memberLayout must be separatePages so the complete API has dedicated member pages."
    }
    $tocText = Get-Content -LiteralPath $tocPath -Raw
    if ($tocText -notmatch '(?m)^\s*href:\s*guide/\s*$' -or $tocText -notmatch '(?m)^\s*href:\s*api/\s*$') {
        throw "The root DocFX TOC must remain navbar-only and reference the guide and API TOCs."
    }
    if (-not (Test-Path -LiteralPath $guideTocPath -PathType Leaf)) {
        throw "The Microsoft Learn-style guide TOC is missing: $guideTocPath"
    }
    $pdfTocText = Get-Content -LiteralPath $pdfTocPath -Raw
    if ($pdfTocText -notmatch '(?m)^\s*href:\s*\.\./guide/toc\.yml\s*$' -or $pdfTocText -notmatch '(?m)^\s*href:\s*\.\./api/toc\.yml\s*$') {
        throw "The dedicated PDF TOC must nest both guide/toc.yml and api/toc.yml."
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

                $apiTocText = Get-Content -LiteralPath $apiTocPath -Raw
                if ($apiTocText -notmatch '(?m)^\s*-\s+name:\s+API overview\s*$') {
                    $updatedApiTocText = [regex]::Replace(
                        $apiTocText,
                        '(?s)\A(?:\uFEFF)?items:\s*\r?\n',
                        "items:`r`n- name: API overview`r`n  href: index.md`r`n")
                    if ([string]::Equals($updatedApiTocText, $apiTocText, [StringComparison]::Ordinal)) {
                        throw "The generated API TOC did not contain the expected root items node: $apiTocPath"
                    }
                    Set-Content -LiteralPath $apiTocPath -Value $updatedApiTocText -Encoding utf8
                }

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
    if ($docfxBuildSucceeded -and $RequirePdf) {
        $nodeInfo = Resolve-LocalGptNode -AllowProvisioning
        if ($null -ne $nodeInfo) {
            $nodeVersionUsed = [string]$nodeInfo.Version
            $nodeProvisioned = [bool]$nodeInfo.Provisioned
            New-Item -ItemType Directory -Path $playwrightBrowserRoot -Force | Out-Null
            $env:PLAYWRIGHT_NODEJS_PATH = [string]$nodeInfo.Path
            $env:PLAYWRIGHT_BROWSERS_PATH = $playwrightBrowserRoot
            $configuredPdfTimeout = 0
            if (-not [int]::TryParse([string]$env:DOCFX_PDF_TIMEOUT, [ref]$configuredPdfTimeout) -or $configuredPdfTimeout -lt $pdfTimeoutMilliseconds) {
                $env:DOCFX_PDF_TIMEOUT = [string]$pdfTimeoutMilliseconds
            }
            if ([string]::IsNullOrWhiteSpace($env:NODE_OPTIONS)) {
                $env:NODE_OPTIONS = "--max-old-space-size=4096"
            }
            elseif ($env:NODE_OPTIONS -notmatch '(?i)--max-old-space-size(?:=|\s)') {
                $env:NODE_OPTIONS = "$($env:NODE_OPTIONS) --max-old-space-size=4096"
            }

            Write-Host "Generating the complete DocFX PDF with Node.js $nodeVersionUsed; browser cache=$playwrightBrowserRoot; timeout=$($env:DOCFX_PDF_TIMEOUT) ms." -ForegroundColor Cyan
            try {
                # A previous HTML-only build may have left the deterministic one-page PDF behind.
                # Remove every old PDF before invoking DocFX so stale output can never be accepted.
                Get-ChildItem -LiteralPath $siteRoot -Filter "*.pdf" -File -Recurse -ErrorAction SilentlyContinue |
                    Remove-Item -Force -ErrorAction SilentlyContinue

                $pdfResult = Invoke-LocalGptDocfx -Arguments @("pdf", $configPath)
                $pdfCandidates = @(
                    Get-ChildItem -LiteralPath $siteRoot -Filter "*.pdf" -File -Recurse -ErrorAction SilentlyContinue |
                        Sort-Object @{ Expression = { if ($_.Name -eq $pdfName) { 0 } else { 1 } } },
                            @{ Expression = { $_.Length }; Descending = $true }
                )
                $pdfCandidateCount = $pdfCandidates.Count

                if ($pdfResult.ExitCode -eq 0 -and $pdfCandidateCount -gt 0) {
                    $docfxPdf = $pdfCandidates |
                        Where-Object { $_.Name -eq $pdfName } |
                        Select-Object -First 1
                    if ($null -eq $docfxPdf) { $docfxPdf = $pdfCandidates | Select-Object -First 1 }

                    $pdfGeneratedSourcePath = Get-LocalGptRelativePath -Root $siteRoot -Path $docfxPdf.FullName
                    Write-Host "DocFX produced PDF candidate $pdfGeneratedSourcePath ($($docfxPdf.Length) bytes)." -ForegroundColor Cyan
                    if (-not [string]::Equals($docfxPdf.FullName, $pdfPath, [StringComparison]::OrdinalIgnoreCase)) {
                        Copy-Item -LiteralPath $docfxPdf.FullName -Destination $pdfPath -Force
                    }

                    $pdfGenerated = Test-LocalGptCompletePdf -Path $pdfPath -MinimumBytes $minimumCompletePdfBytes
                    if ($pdfGenerated) {
                        $resolvedPdf = Get-Item -LiteralPath $pdfPath
                        $pdfFileSize = $resolvedPdf.Length
                        $pdfMode = "docfx"
                    }
                    else {
                        $warnings.Add("DocFX produced a PDF candidate, but it was smaller than $minimumCompletePdfBytes bytes or matched the retired fallback marker: $pdfGeneratedSourcePath")
                    }
                }

                if (-not $pdfGenerated) {
                    $candidateSummary = @($pdfCandidates | ForEach-Object { "$(Get-LocalGptRelativePath -Root $siteRoot -Path $_.FullName)=$($_.Length)" }) -join ", "
                    $diagnosticTail = @($pdfResult.Output | Select-Object -Last 20) -join " | "
                    if ([string]::IsNullOrWhiteSpace($diagnosticTail)) {
                        $diagnosticTail = "DocFX returned no PDF diagnostics."
                    }
                    $warnings.Add("DocFX PDF generation exited with code $($pdfResult.ExitCode); candidates=[$candidateSummary]: $diagnosticTail")
                }
            }
            catch {
                $warnings.Add("DocFX PDF generation failed: $($_.Exception.Message)")
            }
        }
        else {
            $warnings.Add("Node.js $minimumNodeMajor or later was unavailable; complete DocFX PDF generation was skipped.")
        }
    }
    elseif ($docfxBuildSucceeded) {
        $warnings.Add("Complete DocFX PDF generation was explicitly disabled; this HTML-only diagnostic build does not emit a fallback PDF.")
    }

    if ($RequirePdf -and (-not $pdfGenerated -or $pdfMode -ne "docfx")) {
        $pdfFailureDetails = @(
            $warnings |
                Where-Object { $_ -match '(?i)(DocFX PDF|Node\.js|Playwright|Chromium)' } |
                Select-Object -Last 4
        ) -join " | "
        if ([string]::IsNullOrWhiteSpace($pdfFailureDetails)) {
            $pdfFailureDetails = "No additional PDF diagnostic was emitted."
        }
        throw "Complete DocFX PDF generation failed. $pdfFailureDetails"
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
        pdfGeneratedSourcePath = $pdfGeneratedSourcePath
        pdfCandidateCount = $pdfCandidateCount
        minimumCompletePdfBytes = $minimumCompletePdfBytes
        nodeVersion = $nodeVersionUsed
        nodeProvisioned = $nodeProvisioned
        pdfTimeoutMilliseconds = $pdfTimeoutMilliseconds
        completeApiReference = $documentationMode -eq "docfx" -and $apiYamlCount -gt 1 -and $apiHtmlCount -gt 1
        warnings = @($warnings)
    }
    $statusPath = Join-Path $publishRoot "documentation-status.json"
    $status | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statusPath -Encoding utf8

    $requiredArtifacts = [System.Collections.Generic.List[string]]::new()
    $requiredArtifacts.Add((Join-Path $publishRoot "index.html"))
    $requiredArtifacts.Add((Join-Path $publishRoot "LocalGPT.xml"))
    $requiredArtifacts.Add($statusPath)
    if ($RequirePdf -or $pdfGenerated) {
        $requiredArtifacts.Add((Join-Path $publishRoot $pdfName))
    }
    foreach ($requiredArtifact in $requiredArtifacts) {
        if (-not (Test-Path -LiteralPath $requiredArtifact -PathType Leaf)) {
            throw "Documentation generation did not produce the required artifact: $requiredArtifact"
        }
    }
}

Write-Host "LocalGPT documentation generated for version $Version using $documentationMode; PDF mode: $pdfMode." -ForegroundColor Green
