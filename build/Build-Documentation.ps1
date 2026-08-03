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
$printBookParentRoot = Join-Path $docsRoot ".print-book"
$printBookRoot = Join-Path $printBookParentRoot ([Guid]::NewGuid().ToString('N'))
$printBookPath = Join-Path $printBookRoot "LocalGPT-$Version-complete.html"
$pdfName = "LocalGPT-$Version.pdf"
$warnings = [System.Collections.Generic.List[string]]::new()
$documentationMode = "static-fallback"
$pdfMode = "unavailable"
$toolSource = "unavailable"
$apiYamlCount = 0
$apiHtmlCount = 0
$apiNavigationGroupCount = 0
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
$pdfRenderer = ""
$pdfSourcePageCount = 0
$minimumCompletePdfBytes = 65536
$minimumNodeMajor = 20
$maximumPreferredNodeMajor = 22
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
$pdfCompressionMode = "none"
$pdfBytesBeforeCompression = 0
$pdfCompressionSavedBytes = 0
$maximumBrowserPrintSourcePages = 1500

if (-not (Test-Path -LiteralPath $AssemblyPath)) { throw "Documentation assembly was not found: $AssemblyPath" }
if (-not (Test-Path -LiteralPath $XmlDocumentationPath)) { throw "XML documentation file was not found: $XmlDocumentationPath" }
if (-not (Test-Path -LiteralPath $docsRoot)) { throw "Documentation source directory was not found: $docsRoot" }

function ConvertTo-LocalGptXmlText {
    param([AllowNull()][object]$Node)
    if ($null -eq $Node) { return "" }
    $text = [string]$Node.InnerText
    return ([regex]::Replace($text, '\s+', ' ')).Trim()
}

function Remove-LocalGptTemporaryPath {
    param(
        [AllowEmptyString()][string]$Path,
        [ValidateRange(1, 20)][int]$Attempts = 5,
        [ValidateRange(0, 5000)][int]$DelayMilliseconds = 200
    )

    if ([string]::IsNullOrWhiteSpace($Path)) { return }

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            if (-not (Test-Path -LiteralPath $Path)) { return }
            Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
            return
        }
        catch {
            # Chromium may continue retiring files in its temporary profile for a moment after the
            # headless parent process exits. A path that disappeared during enumeration is already
            # clean and must never turn an otherwise successful documentation build into a failure.
            if (-not (Test-Path -LiteralPath $Path)) { return }
            if ($attempt -lt $Attempts) {
                Start-Sleep -Milliseconds $DelayMilliseconds
                continue
            }

            Write-Warning "Temporary documentation cleanup could not remove '$Path': $($_.Exception.Message)"
            return
        }
    }
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
:root { color-scheme: dark; --bg: #1f1f1f; --panel: #303030; --text: #f4f4f4; --muted: #b4b4bd; --accent: #c22cf2; --line: #505058; }
* { box-sizing: border-box; }
body { margin: 0; background: var(--bg); color: var(--text); font: 16px/1.55 system-ui, "Segoe UI", sans-serif; }
header, footer { padding: 1rem 3vw; background: #171717; border-bottom: 1px solid var(--line); }
footer { border-top: 1px solid var(--line); border-bottom: 0; color: var(--muted); }
main { max-width: 1500px; margin: auto; padding: 2rem 3vw; }
a { color: #e48cff; }
section, .card { background: var(--panel); border: 1px solid var(--line); border-radius: .8rem; padding: 1rem; margin: 1rem 0; }
pre { white-space: pre-wrap; overflow-wrap: anywhere; background: #18181b; padding: 1rem; border-radius: .5rem; }
code { overflow-wrap: anywhere; }
table { width: 100%; border-collapse: collapse; }
td, th { padding: .55rem; border-bottom: 1px solid var(--line); text-align: left; }
.grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 1rem; }
.muted { color: var(--muted); }
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

function ConvertTo-LocalGptFileUri {
    param([Parameter(Mandatory)][string]$Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    return ([Uri]::new($fullPath)).AbsoluteUri
}

function Find-LocalGptDocumentationBrowser {
    $candidates = [System.Collections.Generic.List[object]]::new()
    if (-not [string]::IsNullOrWhiteSpace($env:LOCALGPT_DOCUMENTATION_BROWSER)) {
        $candidates.Add([pscustomobject]@{ Path = $env:LOCALGPT_DOCUMENTATION_BROWSER; Name = "configured-browser" })
    }

    $programFilesX86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles)) {
        $candidates.Add([pscustomobject]@{ Path = (Join-Path $env:ProgramFiles "Microsoft\Edge\Application\msedge.exe"); Name = "Microsoft Edge" })
        $candidates.Add([pscustomobject]@{ Path = (Join-Path $env:ProgramFiles "Google\Chrome\Application\chrome.exe"); Name = "Google Chrome" })
    }
    if (-not [string]::IsNullOrWhiteSpace($programFilesX86)) {
        $candidates.Add([pscustomobject]@{ Path = (Join-Path $programFilesX86 "Microsoft\Edge\Application\msedge.exe"); Name = "Microsoft Edge" })
        $candidates.Add([pscustomobject]@{ Path = (Join-Path $programFilesX86 "Google\Chrome\Application\chrome.exe"); Name = "Google Chrome" })
    }
    if (-not [string]::IsNullOrWhiteSpace($localApplicationData)) {
        $candidates.Add([pscustomobject]@{ Path = (Join-Path $localApplicationData "Microsoft\Edge\Application\msedge.exe"); Name = "Microsoft Edge" })
        $candidates.Add([pscustomobject]@{ Path = (Join-Path $localApplicationData "Google\Chrome\Application\chrome.exe"); Name = "Google Chrome" })
    }

    foreach ($commandName in @("msedge", "chrome", "chromium", "chromium-browser")) {
        $command = Get-Command $commandName -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -ne $command) {
            $commandPath = if (-not [string]::IsNullOrWhiteSpace([string]$command.Source)) { [string]$command.Source } else { [string]$command.Path }
            if (-not [string]::IsNullOrWhiteSpace($commandPath)) {
                $candidates.Add([pscustomobject]@{ Path = $commandPath; Name = $commandName })
            }
        }
    }

    $visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace([string]$candidate.Path)) { continue }
        try { $fullPath = [IO.Path]::GetFullPath([string]$candidate.Path) }
        catch { continue }
        if (-not $visited.Add($fullPath)) { continue }
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            return [pscustomobject]@{ Path = $fullPath; Name = [string]$candidate.Name }
        }
    }

    return $null
}

function Get-LocalGptPrintPageFiles {
    param([Parameter(Mandatory)][string]$SiteRoot)

    $siteRootFull = [IO.Path]::GetFullPath($SiteRoot)
    $allFiles = @(
        Get-ChildItem -LiteralPath $siteRootFull -Filter "*.html" -File -Recurse -ErrorAction SilentlyContinue |
            Where-Object {
                $_.Name -notin @("toc.html", "404.html", "search.html") -and
                $_.FullName -notlike "*\.print-book\*"
            }
    )
    $byRelativePath = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in $allFiles) {
        $relative = (Get-LocalGptRelativePath -Root $siteRootFull -Path $file.FullName).Replace('\', '/')
        $byRelativePath[$relative] = $file
    }

    $ordered = [System.Collections.Generic.List[object]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $addRelativePath = {
        param([string]$RelativePath)
        if ([string]::IsNullOrWhiteSpace($RelativePath)) { return }
        $normalized = $RelativePath.Replace('\', '/').TrimStart([char]'/')
        if ($byRelativePath.ContainsKey($normalized) -and $seen.Add($normalized)) {
            $ordered.Add($byRelativePath[$normalized])
        }
    }

    & $addRelativePath "index.html"
    foreach ($tocRelativePath in @("guide/toc.html", "api/toc.html")) {
        $tocPath = Join-Path $siteRootFull ($tocRelativePath.Replace('/', [IO.Path]::DirectorySeparatorChar))
        if (-not (Test-Path -LiteralPath $tocPath -PathType Leaf)) { continue }
        $tocDirectory = Split-Path -Parent $tocPath
        $tocHtml = Get-Content -LiteralPath $tocPath -Raw
        foreach ($match in [regex]::Matches($tocHtml, '(?i)href\s*=\s*["''](?<value>[^"'']+\.html(?:[?#][^"'']*)?)["'']')) {
            $href = [Net.WebUtility]::HtmlDecode($match.Groups['value'].Value)
            $href = $href.Split('#')[0].Split('?')[0]
            if ([string]::IsNullOrWhiteSpace($href) -or [Uri]::IsWellFormedUriString($href, [UriKind]::Absolute)) { continue }
            try {
                $decodedHref = [Uri]::UnescapeDataString($href)
                $resolved = [IO.Path]::GetFullPath((Join-Path $tocDirectory ($decodedHref.Replace('/', [IO.Path]::DirectorySeparatorChar))))
                $relative = (Get-LocalGptRelativePath -Root $siteRootFull -Path $resolved).Replace('\', '/')
                & $addRelativePath $relative
            }
            catch { }
        }
    }

    foreach ($file in @($allFiles | Sort-Object FullName)) {
        $relative = (Get-LocalGptRelativePath -Root $siteRootFull -Path $file.FullName).Replace('\', '/')
        & $addRelativePath $relative
    }
    return @($ordered)
}

function Get-LocalGptHtmlDocumentTitle {
    param(
        [Parameter(Mandatory)][string]$Html,
        [Parameter(Mandatory)][string]$Fallback
    )

    foreach ($pattern in @('(?is)<h1\b[^>]*>(?<value>.*?)</h1>', '(?is)<title\b[^>]*>(?<value>.*?)</title>')) {
        $match = [regex]::Match($Html, $pattern)
        if ($match.Success) {
            $withoutTags = [regex]::Replace($match.Groups['value'].Value, '<[^>]+>', ' ')
            $decoded = [Net.WebUtility]::HtmlDecode($withoutTags)
            $normalized = [regex]::Replace($decoded, '\s+', ' ').Trim()
            if (-not [string]::IsNullOrWhiteSpace($normalized)) { return $normalized }
        }
    }
    return $Fallback
}

function Get-LocalGptHtmlDocumentBody {
    param([Parameter(Mandatory)][string]$Html)

    $withoutScripts = [regex]::Replace($Html, '(?is)<script\b[^>]*>.*?</script>', '')
    foreach ($pattern in @('(?is)<article\b[^>]*>(?<value>.*?)</article>', '(?is)<main\b[^>]*>(?<value>.*?)</main>', '(?is)<body\b[^>]*>(?<value>.*?)</body>')) {
        $match = [regex]::Match($withoutScripts, $pattern)
        if ($match.Success) { return $match.Groups['value'].Value }
    }
    return $withoutScripts
}


function Get-LocalGptApiMemberSectionPresentation {
    param([Parameter(Mandatory)][string]$Name)

    switch ($Name) {
        "Constructors" { return [pscustomobject]@{ Key = "constructors"; IconHtml = "&#x2728;"; Accent = "#7c3aed" } }
        "Fields" { return [pscustomobject]@{ Key = "fields"; IconHtml = "&#x1F9F1;"; Accent = "#d97706" } }
        "Properties" { return [pscustomobject]@{ Key = "properties"; IconHtml = "&#x1F537;"; Accent = "#0891b2" } }
        "Methods" { return [pscustomobject]@{ Key = "methods"; IconHtml = "&#x2699;&#xFE0F;"; Accent = "#2563eb" } }
        "Events" { return [pscustomobject]@{ Key = "events"; IconHtml = "&#x26A1;"; Accent = "#db2777" } }
        "Operators" { return [pscustomobject]@{ Key = "operators"; IconHtml = "&#x2797;"; Accent = "#ea580c" } }
        "Explicit Interface Implementations" { return [pscustomobject]@{ Key = "explicit-interface-implementations"; IconHtml = "&#x1F517;"; Accent = "#0f766e" } }
        "Extension Methods" { return [pscustomobject]@{ Key = "extension-methods"; IconHtml = "&#x1F9E9;"; Accent = "#16a34a" } }
    }
    return $null
}

function Get-LocalGptApiMemberSectionName {
    param([Parameter(Mandatory)][string]$HeadingHtml)

    $withoutDecorations = [regex]::Replace(
        $HeadingHtml,
        '(?is)<span\b[^>]*\bclass=["''][^"'']*\blocalgpt-api-member-(?:icon|count)\b[^"'']*["''][^>]*>.*?</span>',
        '')
    $withoutTags = [regex]::Replace($withoutDecorations, '<[^>]+>', ' ')
    return [regex]::Replace([Net.WebUtility]::HtmlDecode($withoutTags), '\s+', ' ').Trim()
}

function Get-LocalGptApiMemberEntryCount {
    param([Parameter(Mandatory)][string]$SectionHtml)

    $headingCount = [regex]::Matches($SectionHtml, '(?is)<h3\b').Count
    if ($headingCount -gt 0) { return $headingCount }

    $tableBodyMatches = [regex]::Matches($SectionHtml, '(?is)<tbody\b[^>]*>(?<body>.*?)</tbody>')
    $rowCount = 0
    foreach ($tableBodyMatch in $tableBodyMatches) {
        $rowCount += [regex]::Matches($tableBodyMatch.Groups['body'].Value, '(?is)<tr\b').Count
    }
    if ($rowCount -gt 0) { return $rowCount }
    return 1
}

function Get-LocalGptApiMemberSections {
    param([Parameter(Mandatory)][string]$Html)

    $sections = [System.Collections.Generic.List[object]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $headings = @([regex]::Matches($Html, '(?is)<h2\b[^>]*\bid=["''](?<id>[^"'']+)["''][^>]*>(?<value>.*?)</h2>'))
    for ($index = 0; $index -lt $headings.Count; $index++) {
        $match = $headings[$index]
        $name = Get-LocalGptApiMemberSectionName -HeadingHtml $match.Groups['value'].Value
        $presentation = Get-LocalGptApiMemberSectionPresentation -Name $name
        if ($null -eq $presentation) { continue }

        $id = [Net.WebUtility]::HtmlDecode($match.Groups['id'].Value).Trim()
        if ([string]::IsNullOrWhiteSpace($id) -or -not $seen.Add($id)) { continue }
        $contentStart = $match.Index + $match.Length
        $contentEnd = if ($index + 1 -lt $headings.Count) { $headings[$index + 1].Index } else { $Html.Length }
        $sectionHtml = if ($contentEnd -gt $contentStart) { $Html.Substring($contentStart, $contentEnd - $contentStart) } else { '' }
        $count = Get-LocalGptApiMemberEntryCount -SectionHtml $sectionHtml
        $sections.Add([pscustomobject]@{
            Name = $name
            Id = $id
            Key = [string]$presentation.Key
            IconHtml = [string]$presentation.IconHtml
            Accent = [string]$presentation.Accent
            Count = $count
        })
    }
    return @($sections)
}

function Convert-LocalGptApiMemberItems {
    param([Parameter(Mandatory)][string]$Html)

    if ($Html -match '(?i)localgpt-api-member-item') { return $Html }
    $itemEvaluator = [Text.RegularExpressions.MatchEvaluator]{
        param($match)
        return '<section class="localgpt-api-member-item">' + $match.Groups['heading'].Value + '<div class="localgpt-api-member-item-body">' + $match.Groups['content'].Value + '</div></section>'
    }
    return [regex]::Replace(
        $Html,
        '(?is)(?<heading><h3\b[^>]*>.*?</h3>)(?<content>.*?)(?=<h3\b|$)',
        $itemEvaluator)
}

function Convert-LocalGptApiMemberPanels {
    param([Parameter(Mandatory)][string]$Html)

    if ($Html -match '(?i)localgpt-api-member-panel') {
        return [pscustomobject]@{ Html = $Html; PanelCount = 0 }
    }

    $script:localGptApiPanelCounter = 0
    $panelEvaluator = [Text.RegularExpressions.MatchEvaluator]{
        param($match)
        $name = Get-LocalGptApiMemberSectionName -HeadingHtml $match.Groups['headingBody'].Value
        $presentation = Get-LocalGptApiMemberSectionPresentation -Name $name
        if ($null -eq $presentation) { return $match.Value }

        $attrs = $match.Groups['attrs'].Value
        $idMatch = [regex]::Match($attrs, '(?i)\bid=["''](?<id>[^"'']+)["'']')
        $id = if ($idMatch.Success) { [Net.WebUtility]::HtmlDecode($idMatch.Groups['id'].Value).Trim() } else { [string]$presentation.Key }
        $content = Convert-LocalGptApiMemberItems -Html $match.Groups['content'].Value
        $count = Get-LocalGptApiMemberEntryCount -SectionHtml $content
        $countLabel = if ($count -eq 1) { '1 item' } else { "$count items" }
        $script:localGptApiPanelCounter++

        return '<section class="localgpt-api-member-panel localgpt-api-member-panel--' + [string]$presentation.Key + '" data-member-kind="' + (ConvertTo-LocalGptHtml $name) + '" style="--localgpt-api-accent:' + [string]$presentation.Accent + ';" role="region" aria-labelledby="' + (ConvertTo-LocalGptHtml $id) + '">' +
            '<h2' + $attrs + '><span class="localgpt-api-member-icon" aria-hidden="true">' + [string]$presentation.IconHtml + '</span>' + $match.Groups['headingBody'].Value + '<span class="localgpt-api-member-count" aria-label="' + $countLabel + '">' + $count + '</span></h2>' +
            '<div class="localgpt-api-member-panel-body">' + $content + '</div></section>'
    }

    $updated = [regex]::Replace(
        $Html,
        '(?is)<h2\b(?<attrs>[^>]*)>(?<headingBody>.*?)</h2>(?<content>.*?)(?=<h2\b|</article>)',
        $panelEvaluator)
    $panelCount = [int]$script:localGptApiPanelCounter
    Remove-Variable -Name localGptApiPanelCounter -Scope Script -ErrorAction SilentlyContinue
    return [pscustomobject]@{ Html = $updated; PanelCount = $panelCount }
}

function Update-LocalGptApiPresentation {
    param([Parameter(Mandatory)][string]$SiteRoot)

    $apiSiteRoot = Join-Path $SiteRoot "api"
    if (-not (Test-Path -LiteralPath $apiSiteRoot -PathType Container)) { return 0 }
    $panelCount = 0
    foreach ($file in @(Get-ChildItem -LiteralPath $apiSiteRoot -Filter "*.html" -File -Recurse -ErrorAction SilentlyContinue)) {
        if ($file.Name -in @("toc.html", "index.html", "404.html", "search.html")) { continue }
        $html = Get-Content -LiteralPath $file.FullName -Raw
        $result = Convert-LocalGptApiMemberPanels -Html $html
        if ($result.PanelCount -le 0 -or [string]::Equals($result.Html, $html, [StringComparison]::Ordinal)) { continue }
        [IO.File]::WriteAllText($file.FullName, [string]$result.Html, [Text.UTF8Encoding]::new($false))
        $panelCount += [int]$result.PanelCount
    }
    return $panelCount
}

function Get-LocalGptApiPageMetadata {
    param(
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Html
    )

    $kind = "API"
    $displayName = $Title
    foreach ($candidateKind in @("Namespace", "Class", "Interface", "Struct", "Enum", "Delegate", "Record")) {
        $prefix = $candidateKind + " "
        if ($Title.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            $kind = $candidateKind
            $displayName = $Title.Substring($prefix.Length).Trim()
            break
        }
    }

    $namespaceName = ""
    if ($kind -eq "Namespace") {
        $namespaceName = $displayName
    }
    else {
        $text = [regex]::Replace($Html, '<[^>]+>', ' ')
        $text = [regex]::Replace([Net.WebUtility]::HtmlDecode($text), '\s+', ' ')
        $namespaceMatch = [regex]::Match($text, '(?i)\bNamespace:\s*(?<value>[A-Za-z_][A-Za-z0-9_.]*)')
        if ($namespaceMatch.Success) { $namespaceName = $namespaceMatch.Groups['value'].Value }
    }

    return [pscustomobject]@{
        Kind = $kind
        DisplayName = $displayName
        Namespace = $namespaceName
        MemberSections = @(Get-LocalGptApiMemberSections -Html $Html)
    }
}

function Convert-LocalGptPrintDocumentAnchors {
    param(
        [Parameter(Mandatory)][string]$Html,
        [Parameter(Mandatory)][string]$PageAnchor
    )

    $idEvaluator = [Text.RegularExpressions.MatchEvaluator]{
        param($match)
        $attribute = $match.Groups['attribute'].Value
        $quote = $match.Groups['quote'].Value
        $value = $match.Groups['value'].Value
        if ($value.StartsWith($PageAnchor + '-', [StringComparison]::OrdinalIgnoreCase)) { return $match.Value }
        return $attribute + '=' + $quote + $PageAnchor + '-' + $value + $quote
    }
    $result = [regex]::Replace($Html, '(?<attribute>id|name)\s*=\s*(?<quote>["''])(?<value>[^"'']+)(?:\k<quote>)', $idEvaluator, [Text.RegularExpressions.RegexOptions]::IgnoreCase)

    $hrefEvaluator = [Text.RegularExpressions.MatchEvaluator]{
        param($match)
        $quote = $match.Groups['quote'].Value
        $fragment = $match.Groups['fragment'].Value.TrimStart([char]'#')
        if ([string]::IsNullOrWhiteSpace($fragment)) { return $match.Value }
        return 'href=' + $quote + '#' + $PageAnchor + '-' + $fragment + $quote
    }
    return [regex]::Replace($result, 'href\s*=\s*(?<quote>["''])(?<fragment>#[^"'']+)(?:\k<quote>)', $hrefEvaluator, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

function Update-LocalGptApiNavigation {
    param([Parameter(Mandatory)][string]$SiteRoot)

    $apiSiteRoot = Join-Path $SiteRoot "api"
    $tocHtmlPath = Join-Path $apiSiteRoot "toc.html"
    if (-not (Test-Path -LiteralPath $tocHtmlPath -PathType Leaf)) { return 0 }

    $sectionMap = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @(Get-ChildItem -LiteralPath $apiSiteRoot -Filter "*.html" -File -Recurse -ErrorAction SilentlyContinue)) {
        if ($file.Name -in @("toc.html", "index.html", "404.html", "search.html")) { continue }
        $html = Get-Content -LiteralPath $file.FullName -Raw
        $sections = @(Get-LocalGptApiMemberSections -Html $html)
        if ($sections.Count -eq 0) { continue }
        $relative = (Get-LocalGptRelativePath -Root $apiSiteRoot -Path $file.FullName).Replace('\', '/')
        $sectionMap[$relative] = $sections
    }
    if ($sectionMap.Count -eq 0) { return 0 }

    $tocHtml = Get-Content -LiteralPath $tocHtmlPath -Raw
    $groupCount = 0
    $evaluator = [Text.RegularExpressions.MatchEvaluator]{
        param($match)
        $anchorHtml = $match.Value
        if ($anchorHtml -match '(?i)localgpt-api-member-groups') { return $anchorHtml }
        $href = [Net.WebUtility]::HtmlDecode($match.Groups['href'].Value)
        $hrefPath = $href.Split('#')[0].Split('?')[0]
        if ([string]::IsNullOrWhiteSpace($hrefPath) -or [Uri]::IsWellFormedUriString($hrefPath, [UriKind]::Absolute)) { return $anchorHtml }
        try { $normalized = [Uri]::UnescapeDataString($hrefPath).Replace('\', '/').TrimStart([char]'/') }
        catch { return $anchorHtml }
        if (-not $sectionMap.ContainsKey($normalized)) { return $anchorHtml }

        $links = [Text.StringBuilder]::new()
        [void]$links.Append('<ul class="nav localgpt-api-member-groups" aria-label="API member groups">')
        foreach ($section in @($sectionMap[$normalized])) {
            [void]$links.Append('<li class="nav-item localgpt-api-member-nav-item localgpt-api-member-nav-item--')
            [void]$links.Append((ConvertTo-LocalGptHtml ([string]$section.Key)))
            [void]$links.Append('"><a class="nav-link" href="')
            [void]$links.Append((ConvertTo-LocalGptHtml ($hrefPath + '#' + [string]$section.Id)))
            [void]$links.Append('"><span class="localgpt-api-member-nav-icon" aria-hidden="true">')
            [void]$links.Append([string]$section.IconHtml)
            [void]$links.Append('</span><span class="localgpt-api-member-nav-label">')
            [void]$links.Append((ConvertTo-LocalGptHtml ([string]$section.Name)))
            [void]$links.Append('</span><span class="localgpt-api-member-nav-count" aria-label="item count">')
            [void]$links.Append([string]$section.Count)
            [void]$links.Append('</span></a></li>')
        }
        [void]$links.Append('</ul>')
        $script:localGptNavigationGroupCounter++
        return $anchorHtml + $links.ToString()
    }

    $script:localGptNavigationGroupCounter = 0
    $updated = [regex]::Replace(
        $tocHtml,
        '(?is)<a\b[^>]*\bhref=["''](?<href>[^"'']+\.html(?:[?#][^"'']*)?)["''][^>]*>.*?</a>',
        $evaluator)
    $groupCount = [int]$script:localGptNavigationGroupCounter
    Remove-Variable -Name localGptNavigationGroupCounter -Scope Script -ErrorAction SilentlyContinue
    if ($groupCount -gt 0) {
        [IO.File]::WriteAllText($tocHtmlPath, $updated, [Text.UTF8Encoding]::new($false))
    }
    return $groupCount
}

function Convert-LocalGptPrintDocumentLinks {
    param(
        [Parameter(Mandatory)][string]$Html,
        [Parameter(Mandatory)][string]$PagePath,
        [Parameter(Mandatory)][string]$SiteRoot,
        [Parameter(Mandatory)][object]$AnchorMap
    )

    $pageDirectory = Split-Path -Parent $PagePath
    $siteRootFull = [IO.Path]::GetFullPath($SiteRoot).TrimEnd([char[]]@('\', '/'))
    $siteRootPrefix = $siteRootFull + [IO.Path]::DirectorySeparatorChar
    $evaluator = [Text.RegularExpressions.MatchEvaluator]{
        param($match)

        $attribute = $match.Groups['attribute'].Value
        $value = [Net.WebUtility]::HtmlDecode($match.Groups['value'].Value).Trim()
        if ([string]::IsNullOrWhiteSpace($value) -or
            $value.StartsWith('#') -or
            $value.StartsWith('data:', [StringComparison]::OrdinalIgnoreCase) -or
            $value.StartsWith('mailto:', [StringComparison]::OrdinalIgnoreCase) -or
            $value.StartsWith('javascript:', [StringComparison]::OrdinalIgnoreCase) -or
            [Uri]::IsWellFormedUriString($value, [UriKind]::Absolute)) {
            return $match.Value
        }

        $fragment = ''
        $fragmentIndex = $value.IndexOf('#')
        if ($fragmentIndex -ge 0) {
            $fragment = $value.Substring($fragmentIndex)
            $value = $value.Substring(0, $fragmentIndex)
        }
        $queryIndex = $value.IndexOf('?')
        if ($queryIndex -ge 0) { $value = $value.Substring(0, $queryIndex) }
        if ([string]::IsNullOrWhiteSpace($value)) { return $match.Value }

        try { $resolvedPath = [IO.Path]::GetFullPath((Join-Path $pageDirectory ($value.Replace('/', [IO.Path]::DirectorySeparatorChar)))) }
        catch { return $match.Value }
        if (-not $resolvedPath.StartsWith($siteRootPrefix, [StringComparison]::OrdinalIgnoreCase) -and
            -not [string]::Equals($resolvedPath, $siteRootFull, [StringComparison]::OrdinalIgnoreCase)) {
            return $match.Value
        }

        if ($attribute.Equals('href', [StringComparison]::OrdinalIgnoreCase) -and
            [string]::Equals([IO.Path]::GetExtension($resolvedPath), '.html', [StringComparison]::OrdinalIgnoreCase)) {
            $relativeTarget = (Get-LocalGptRelativePath -Root $siteRootFull -Path $resolvedPath).Replace('\', '/')
            if ($AnchorMap.ContainsKey($relativeTarget)) {
                $target = '#' + $AnchorMap[$relativeTarget]
                if (-not [string]::IsNullOrWhiteSpace($fragment)) {
                    $target += '-' + [Uri]::UnescapeDataString($fragment.TrimStart([char]'#'))
                }
                return $attribute + '="' + $target + '"'
            }
        }

        if (Test-Path -LiteralPath $resolvedPath -PathType Leaf) {
            return $attribute + '="' + (ConvertTo-LocalGptFileUri -Path $resolvedPath) + $fragment + '"'
        }
        return $match.Value
    }

    return [regex]::Replace($Html, '(?<attribute>href|src)\s*=\s*["''](?<value>[^"'']+)["'']', $evaluator, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

function New-LocalGptHtmlPrintBook {
    param(
        [Parameter(Mandatory)][string]$SiteRoot,
        [Parameter(Mandatory)][string]$DestinationPath
    )

    $pages = @(Get-LocalGptPrintPageFiles -SiteRoot $SiteRoot)
    if ($pages.Count -eq 0) { throw "The DocFX site did not contain printable HTML pages." }

    $anchorMap = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([StringComparer]::OrdinalIgnoreCase)
    $pageModels = [System.Collections.Generic.List[object]]::new()
    for ($index = 0; $index -lt $pages.Count; $index++) {
        $page = $pages[$index]
        $relative = (Get-LocalGptRelativePath -Root $SiteRoot -Path $page.FullName).Replace('\', '/')
        $anchor = 'localgpt-document-{0:D5}' -f ($index + 1)
        $anchorMap[$relative] = $anchor
        $html = Get-Content -LiteralPath $page.FullName -Raw
        $title = Get-LocalGptHtmlDocumentTitle -Html $html -Fallback $relative
        $body = Get-LocalGptHtmlDocumentBody -Html $html
        $isApi = $relative.StartsWith('api/', [StringComparison]::OrdinalIgnoreCase)
        $apiMetadata = if ($isApi) { Get-LocalGptApiPageMetadata -Title $title -Html $body } else { $null }
        $apiKind = ''
        $apiDisplayName = ''
        $apiNamespace = ''
        $memberSections = @()
        if ($null -ne $apiMetadata) {
            $apiKind = [string]$apiMetadata.Kind
            $apiDisplayName = [string]$apiMetadata.DisplayName
            $apiNamespace = [string]$apiMetadata.Namespace
            $memberSections = @($apiMetadata.MemberSections)
        }
        $pageModels.Add([pscustomobject]@{
            Path = $page.FullName
            Relative = $relative
            Anchor = $anchor
            Title = $title
            Html = $html
            Body = $body
            IsApi = $isApi
            ApiKind = $apiKind
            ApiDisplayName = $apiDisplayName
            ApiNamespace = $apiNamespace
            MemberSections = $memberSections
        })
    }

    $destinationDirectory = Split-Path -Parent $DestinationPath
    Remove-LocalGptTemporaryPath -Path $destinationDirectory
    New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null

    $builder = [Text.StringBuilder]::new()
    [void]$builder.AppendLine('<!doctype html>')
    [void]$builder.AppendLine('<html lang="en"><head><meta charset="utf-8" />')
    [void]$builder.AppendLine('<meta name="viewport" content="width=device-width, initial-scale=1" />')
    [void]$builder.AppendLine("<title>LocalGPT $Version complete documentation</title>")
    foreach ($cssFile in @(Get-ChildItem -LiteralPath $SiteRoot -Filter "*.css" -File -Recurse -ErrorAction SilentlyContinue | Sort-Object FullName)) {
        [void]$builder.AppendLine('<link rel="stylesheet" href="' + (ConvertTo-LocalGptFileUri -Path $cssFile.FullName) + '" />')
    }
    $printStyles = @'
<style>
@page { size: A4 landscape; margin: 9mm 10mm 11mm; }
html, body { background: #fff !important; color: #242424 !important; font-family: "Segoe UI", Arial, sans-serif !important; font-size: 9.35pt; line-height: 1.38; }
body { margin: 0 !important; max-width: none !important; }
.localgpt-print-cover { min-height: 178mm; display: flex; flex-direction: column; justify-content: center; break-after: page; }
.localgpt-print-cover::before { content: "LOCALGPT DOCUMENTATION"; color: #0067b8; font-size: 9pt; font-weight: 700; letter-spacing: .16em; margin-bottom: 12pt; }
.localgpt-print-cover h1 { color: #171717; font-size: 31pt; font-weight: 650; line-height: 1.06; margin: 0 0 12pt; }
.localgpt-print-cover p { color: #505050; font-size: 11.5pt; margin: 3pt 0; max-width: 64rem; }
.localgpt-print-toc { break-after: page; }
.localgpt-print-toc > h1 { border-bottom: 2px solid #0067b8; color: #171717; font-size: 24pt; margin: 0 0 13pt; padding-bottom: 6pt; }
.localgpt-print-toc-section { margin: 0 0 14pt; }
.localgpt-print-toc-section > h2 { color: #323130; font-size: 14pt; margin: 10pt 0 5pt; }
.localgpt-print-toc-conceptual { columns: 3; column-gap: 1.35rem; padding-left: 1.2rem; }
.localgpt-print-toc-conceptual li { break-inside: avoid; font-size: 8.5pt; line-height: 1.25; margin: 1.2pt 0; }
.localgpt-print-api-overview-link { font-size: 9pt; font-weight: 600; margin: 3pt 0 8pt; }
.localgpt-print-toc-namespace { border-top: 1px solid #d2d0ce; break-inside: auto; margin-top: 7pt; padding-top: 5pt; }
.localgpt-print-toc-namespace h3 { break-after: avoid; color: #0067b8; font-size: 10.5pt; margin: 0 0 3pt; }
.localgpt-print-toc-namespace ul { columns: 3; column-gap: 1.25rem; list-style: none; margin: 0; padding: 0; }
.localgpt-print-toc-namespace li { break-inside: avoid; font-size: 7.8pt; line-height: 1.22; margin: 1.4pt 0; }
.localgpt-print-api-kind { color: #605e5c; font-size: 6.7pt; font-weight: 700; letter-spacing: .03em; text-transform: uppercase; }
.localgpt-print-member-links { display: block; margin-left: 9pt; }
.localgpt-print-member-links a { color: #5c5c5c !important; display: inline-block; font-size: 6.8pt; margin-right: 5pt; }
.localgpt-print-document { break-before: page; page-break-before: always; }
.localgpt-print-document:first-of-type { break-before: auto; page-break-before: auto; }
.localgpt-print-workspace { break-before: page; page-break-before: always; }
.localgpt-print-api-document { break-before: auto; page-break-before: auto; border-top: 1px solid #e1dfdd; margin-top: 13pt; padding-top: 10pt; }
.localgpt-print-api-namespace { border-top: 0; break-before: page !important; page-break-before: always !important; margin-top: 0; padding-top: 0; }
.localgpt-print-api-namespace + .localgpt-print-api-document { border-top: 0; margin-top: 9pt; }
.localgpt-print-source { align-items: baseline; border-bottom: 1px solid #d1d1d1; color: #605e5c; display: flex; font-size: 7.5pt; gap: 1rem; justify-content: space-between; margin-bottom: 8pt; padding-bottom: 4pt; }
.localgpt-print-workspace-header { border-bottom-color: #0067b8; color: #4a4a4a; }
.localgpt-print-api-breadcrumb { border-bottom-color: #8ab4d4; color: #4a4a4a; }
.localgpt-print-api-breadcrumb span:first-child, .localgpt-print-workspace-header span:first-child { color: #0067b8; font-weight: 650; }
.localgpt-print-content { display: block !important; margin: 0 !important; max-width: none !important; opacity: 1 !important; padding: 0 !important; visibility: visible !important; width: auto !important; }
.localgpt-print-content .content, .localgpt-print-content .markdown, .localgpt-print-content .tabGroup > section, .localgpt-print-content details, .localgpt-print-content details > * { display: block !important; opacity: 1 !important; visibility: visible !important; }
.localgpt-print-content nav, .localgpt-print-content button, .localgpt-print-content .action-list, .localgpt-print-content .affix, .localgpt-print-content .tabGroup > ul { display: none !important; }
.localgpt-print-content h1 { color: #171717 !important; font-size: 20pt; font-weight: 650; line-height: 1.14; margin: 0 0 9pt; }
.localgpt-print-api-namespace .localgpt-print-content h1 { border-bottom: 3px solid #0067b8; font-size: 24pt; margin-bottom: 12pt; padding-bottom: 7pt; }
.localgpt-print-api-document:not(.localgpt-print-api-namespace) .localgpt-print-content h1 { border-bottom: 1px solid #d2d0ce; font-size: 18pt; padding-bottom: 6pt; }
.localgpt-print-content h2 { border-left: 3px solid #0067b8; color: #242424 !important; font-size: 14.5pt; line-height: 1.2; margin: 15pt 0 7pt; padding: 3pt 0 3pt 7pt; }
.localgpt-print-content h3 { color: #323130 !important; font-size: 11.5pt; line-height: 1.25; margin: 11pt 0 5pt; }
.localgpt-print-content h4, .localgpt-print-content h5 { color: #323130 !important; font-size: 9.8pt; margin: 9pt 0 4pt; }
.localgpt-print-content h1, .localgpt-print-content h2, .localgpt-print-content h3, .localgpt-print-content h4 { break-after: avoid; page-break-after: avoid; }
.localgpt-print-content p, .localgpt-print-content ul, .localgpt-print-content ol, .localgpt-print-content dl { margin-bottom: 6pt; margin-top: 4pt; }
.localgpt-print-content p, .localgpt-print-content li { orphans: 3; widows: 3; }
.localgpt-print-content li { margin: 1.5pt 0; }
.localgpt-print-content dl { display: grid; grid-template-columns: minmax(9rem, 18%) 1fr; column-gap: 9pt; row-gap: 3pt; }
.localgpt-print-content dt { color: #0067b8; font-weight: 650; }
.localgpt-print-content dd { margin: 0; min-width: 0; }
.localgpt-print-content pre, .localgpt-print-content code { font-family: Consolas, "Cascadia Mono", monospace !important; overflow-wrap: anywhere !important; white-space: pre-wrap !important; word-break: break-word !important; }
.localgpt-print-content pre { background: #f3f2f1 !important; border: 1px solid #d2d0ce; border-left: 4px solid #0078d4; break-inside: auto; font-size: 7.8pt; line-height: 1.28; margin: 6pt 0 8pt; padding: 7pt 8pt !important; }
.localgpt-print-content code { font-size: .92em; }
.localgpt-print-content table { border-collapse: collapse; break-inside: auto; font-size: 8.3pt; table-layout: auto; width: 100% !important; }
.localgpt-print-content thead { display: table-header-group; }
.localgpt-print-content th { background: #f3f2f1 !important; color: #323130; font-weight: 650; }
.localgpt-print-content th, .localgpt-print-content td { border: 1px solid #d2d0ce; padding: 3.5pt 4.5pt !important; vertical-align: top; }
.localgpt-print-content tbody tr:nth-child(even) { background: #faf9f8 !important; }
.localgpt-print-content tr, .localgpt-print-content img, .localgpt-print-content blockquote { break-inside: avoid; }
.localgpt-print-content blockquote { background: #f5f9fc; border-left: 4px solid #50a7dc; margin: 7pt 0; padding: 6pt 9pt; }
.localgpt-print-content img { height: auto !important; max-height: 176mm !important; max-width: 100% !important; object-fit: contain; }
.localgpt-print-content a { color: #0067b8 !important; overflow-wrap: anywhere; text-decoration: none; }
.localgpt-print-content a::after { content: none !important; }
.localgpt-print-member-link { align-items: center; display: inline-flex !important; gap: 2pt; }
.localgpt-print-member-icon { font-family: "Segoe UI Emoji", "Segoe UI Symbol", sans-serif; font-size: 7pt; }
.localgpt-print-member-count { background: #eef3f8; border-radius: 999px; color: #4a5562; font-size: 6pt; margin-left: 1pt; padding: .5pt 2.5pt; }
.localgpt-print-content .localgpt-api-member-panel { --localgpt-api-accent: #0067b8; border: 1px solid #cfd8e3; border-left: 4px solid var(--localgpt-api-accent); border-radius: 7pt; break-inside: auto; margin: 10pt 0 12pt; overflow: hidden; }
.localgpt-print-content .localgpt-api-member-panel > h2 { align-items: center; background: color-mix(in srgb, var(--localgpt-api-accent) 9%, #fff); border: 0 !important; display: flex; font-size: 13pt !important; gap: 6pt; line-height: 1.15; margin: 0 !important; padding: 6.5pt 8pt !important; }
.localgpt-print-content .localgpt-api-member-icon { align-items: center; background: color-mix(in srgb, var(--localgpt-api-accent) 15%, #fff); border: 1px solid color-mix(in srgb, var(--localgpt-api-accent) 28%, #fff); border-radius: 5pt; display: inline-flex; flex: 0 0 auto; font-family: "Segoe UI Emoji", "Segoe UI Symbol", sans-serif; font-size: 11pt; height: 20pt; justify-content: center; width: 20pt; }
.localgpt-print-content .localgpt-api-member-count { background: #fff; border: 1px solid color-mix(in srgb, var(--localgpt-api-accent) 24%, #d2d0ce); border-radius: 999px; color: #4a4a4a; font-size: 7pt; font-weight: 650; margin-left: auto; min-width: 15pt; padding: 2pt 5pt; text-align: center; }
.localgpt-print-content .localgpt-api-member-panel-body { padding: 4pt 8pt 7pt; }
.localgpt-print-content .localgpt-api-member-item { border-top: 1px solid #e5e7eb; margin: 0; padding: 6pt 0 3pt; }
.localgpt-print-content .localgpt-api-member-item:first-child { border-top: 0; }
.localgpt-print-content .localgpt-api-member-item > h3 { color: color-mix(in srgb, var(--localgpt-api-accent) 75%, #242424) !important; margin-top: 1pt; }
.localgpt-print-content .localgpt-api-member-item-body { min-width: 0; }
.localgpt-print-content .localgpt-api-member-panel--constructors .localgpt-api-member-item,
.localgpt-print-content .localgpt-api-member-panel--fields .localgpt-api-member-item,
.localgpt-print-content .localgpt-api-member-panel--properties .localgpt-api-member-item,
.localgpt-print-content .localgpt-api-member-panel--events .localgpt-api-member-item,
.localgpt-print-content .localgpt-api-member-panel--operators .localgpt-api-member-item { break-inside: avoid-page; page-break-inside: avoid; }
@media print { .localgpt-print-document { box-shadow: none !important; } }
</style>
'@
    [void]$builder.AppendLine($printStyles)
    [void]$builder.AppendLine('</head><body>')
    [void]$builder.AppendLine('<section class="localgpt-print-cover">')
    [void]$builder.AppendLine("<h1>LocalGPT $Version</h1>")
    [void]$builder.AppendLine('<p>Complete product, architecture, operations, and XML-generated API documentation.</p>')
    [void]$builder.AppendLine("<p>$($pageModels.Count) HTML reference pages · generated $([DateTime]::UtcNow.ToString('u'))</p>")
    [void]$builder.AppendLine('</section>')
    [void]$builder.AppendLine('<section class="localgpt-print-toc"><h1>Contents</h1>')
    [void]$builder.AppendLine('<div class="localgpt-print-toc-section"><h2>Product, architecture, and operations</h2><ol class="localgpt-print-toc-conceptual">')
    foreach ($page in @($pageModels | Where-Object { -not $_.IsApi })) {
        [void]$builder.AppendLine('<li><a href="#' + $page.Anchor + '">' + (ConvertTo-LocalGptHtml $page.Title) + '</a></li>')
    }
    [void]$builder.AppendLine('</ol></div>')
    [void]$builder.AppendLine('<div class="localgpt-print-toc-section localgpt-print-toc-api"><h2>API reference</h2>')
    $currentNamespace = ''
    foreach ($page in @($pageModels | Where-Object { $_.IsApi })) {
        if ($page.ApiKind -eq 'Namespace') {
            if (-not [string]::IsNullOrWhiteSpace($currentNamespace)) { [void]$builder.AppendLine('</ul></section>') }
            $currentNamespace = if ([string]::IsNullOrWhiteSpace($page.ApiNamespace)) { $page.ApiDisplayName } else { $page.ApiNamespace }
            [void]$builder.AppendLine('<section class="localgpt-print-toc-namespace"><h3><a href="#' + $page.Anchor + '">Namespace ' + (ConvertTo-LocalGptHtml $currentNamespace) + '</a></h3><ul>')
            continue
        }
        if ($page.Relative -eq 'api/index.html') {
            [void]$builder.AppendLine('<p class="localgpt-print-api-overview-link"><a href="#' + $page.Anchor + '">' + (ConvertTo-LocalGptHtml $page.Title) + '</a></p>')
            continue
        }
        if ([string]::IsNullOrWhiteSpace($currentNamespace)) {
            $currentNamespace = if ([string]::IsNullOrWhiteSpace($page.ApiNamespace)) { 'Other API' } else { $page.ApiNamespace }
            [void]$builder.AppendLine('<section class="localgpt-print-toc-namespace"><h3>' + (ConvertTo-LocalGptHtml $currentNamespace) + '</h3><ul>')
        }
        [void]$builder.Append('<li><a href="#' + $page.Anchor + '"><span class="localgpt-print-api-kind">' + (ConvertTo-LocalGptHtml $page.ApiKind) + '</span> ' + (ConvertTo-LocalGptHtml $page.ApiDisplayName) + '</a>')
        if (@($page.MemberSections).Count -gt 0) {
            [void]$builder.Append('<span class="localgpt-print-member-links">')
            foreach ($section in @($page.MemberSections)) {
                [void]$builder.Append('<a class="localgpt-print-member-link localgpt-print-member-link--' + (ConvertTo-LocalGptHtml ([string]$section.Key)) + '" href="#' + $page.Anchor + '-' + (ConvertTo-LocalGptHtml ([string]$section.Id)) + '"><span class="localgpt-print-member-icon" aria-hidden="true">' + [string]$section.IconHtml + '</span>' + (ConvertTo-LocalGptHtml ([string]$section.Name)) + ' <span class="localgpt-print-member-count">' + [string]$section.Count + '</span></a>')
            }
            [void]$builder.Append('</span>')
        }
        [void]$builder.AppendLine('</li>')
    }
    if (-not [string]::IsNullOrWhiteSpace($currentNamespace)) { [void]$builder.AppendLine('</ul></section>') }
    [void]$builder.AppendLine('</div></section>')

    foreach ($page in $pageModels) {
        $body = [string]$page.Body
        $body = [regex]::Replace($body, '(?i)\s+hidden(?:\s*=\s*(?:"hidden"|''hidden''|hidden))?', '')
        $body = [regex]::Replace($body, '(?i)<details\b(?![^>]*\bopen\b)', '<details open')
        $body = Convert-LocalGptPrintDocumentAnchors -Html $body -PageAnchor $page.Anchor
        $body = Convert-LocalGptPrintDocumentLinks -Html $body -PagePath $page.Path -SiteRoot $SiteRoot -AnchorMap $anchorMap
        if ($page.IsApi) {
            $kindClass = ([string]$page.ApiKind).ToLowerInvariant()
            $documentClass = 'localgpt-print-document localgpt-print-api-document localgpt-print-api-' + $kindClass
        }
        else {
            $documentClass = 'localgpt-print-document localgpt-print-workspace'
        }
        [void]$builder.AppendLine('<section class="' + $documentClass + '" id="' + $page.Anchor + '">')
        if ($page.IsApi) {
            $namespaceLabel = if ([string]::IsNullOrWhiteSpace($page.ApiNamespace)) { 'LocalGPT API' } else { [string]$page.ApiNamespace }
            [void]$builder.AppendLine('<div class="localgpt-print-source localgpt-print-api-breadcrumb"><span>API reference / ' + (ConvertTo-LocalGptHtml $namespaceLabel) + '</span><span>' + (ConvertTo-LocalGptHtml $page.Relative) + '</span></div>')
        }
        else {
            [void]$builder.AppendLine('<div class="localgpt-print-source localgpt-print-workspace-header"><span>LocalGPT ' + (ConvertTo-LocalGptHtml $Version) + ' / Documentation workspace</span><span>' + (ConvertTo-LocalGptHtml $page.Relative) + '</span></div>')
        }
        [void]$builder.AppendLine('<article class="localgpt-print-content">' + $body + '</article>')
        [void]$builder.AppendLine('</section>')
    }

    [void]$builder.AppendLine('</body></html>')
    [IO.File]::WriteAllText($DestinationPath, $builder.ToString(), [Text.UTF8Encoding]::new($false))
    return $pageModels.Count
}

function Invoke-LocalGptBrowserPdf {
    param(
        [Parameter(Mandatory)][string]$BrowserPath,
        [Parameter(Mandatory)][string]$HtmlPath,
        [Parameter(Mandatory)][string]$PdfPath,
        [Parameter(Mandatory)][string]$WorkingRoot
    )

    Remove-Item -LiteralPath $PdfPath -Force -ErrorAction SilentlyContinue
    $inputUri = ConvertTo-LocalGptFileUri -Path $HtmlPath
    $diagnostics = [System.Collections.Generic.List[string]]::new()
    $profileParentRoot = Join-Path ([IO.Path]::GetTempPath()) "LocalGPT\DocumentationBrowserProfiles"
    New-Item -ItemType Directory -Path $profileParentRoot -Force | Out-Null
    foreach ($headlessMode in @("--headless=new", "--headless")) {
        # Keep Chromium's volatile profile outside the print-book directory. Chromium child
        # processes can retire profile files asynchronously, which makes recursive Remove-Item
        # race with disappearing files on Windows PowerShell.
        $profileRoot = Join-Path $profileParentRoot ("browser-profile-" + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $profileRoot -Force | Out-Null
        try {
            $arguments = @(
                $headlessMode,
                "--disable-gpu",
                "--disable-extensions",
                "--disable-dev-shm-usage",
                "--no-first-run",
                "--no-default-browser-check",
                "--allow-file-access-from-files",
                "--run-all-compositor-stages-before-draw",
                "--virtual-time-budget=30000",
                "--print-to-pdf-no-header",
                "--no-pdf-header-footer",
                "--user-data-dir=$profileRoot",
                "--print-to-pdf=$PdfPath",
                $inputUri
            )
            $previousErrorActionPreference = $ErrorActionPreference
            try {
                $ErrorActionPreference = "Continue"
                $output = @(& $BrowserPath @arguments 2>&1)
                $exitCode = [int]$LASTEXITCODE
            }
            finally {
                $ErrorActionPreference = $previousErrorActionPreference
            }
            foreach ($line in $output) {
                $safeLine = [regex]::Replace([string]$line, '(?i)\berror\s*:', 'diagnostic:')
                if (-not [string]::IsNullOrWhiteSpace($safeLine)) { $diagnostics.Add($safeLine) }
            }
            for ($attempt = 0; $attempt -lt 120; $attempt++) {
                $pdfFile = Get-Item -LiteralPath $PdfPath -ErrorAction SilentlyContinue
                if ($null -ne $pdfFile -and $pdfFile.Length -gt 0) { break }
                Start-Sleep -Milliseconds 500
            }
            if ($exitCode -eq 0 -and (Test-Path -LiteralPath $PdfPath -PathType Leaf)) {
                return [pscustomobject]@{ Succeeded = $true; ExitCode = $exitCode; Diagnostics = @($diagnostics); HeadlessMode = $headlessMode }
            }
        }
        finally {
            Remove-LocalGptTemporaryPath -Path $profileRoot -Attempts 8 -DelayMilliseconds 250
        }
    }

    return [pscustomobject]@{ Succeeded = $false; ExitCode = [int]$LASTEXITCODE; Diagnostics = @($diagnostics); HeadlessMode = "" }
}


function Find-LocalGptGhostscript {
    $candidates = [System.Collections.Generic.List[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($env:LOCALGPT_DOCUMENTATION_PDF_COMPRESSOR)) {
        $candidates.Add($env:LOCALGPT_DOCUMENTATION_PDF_COMPRESSOR)
    }

    foreach ($commandName in @("gswin64c", "gswin32c", "gs")) {
        $command = Get-Command $commandName -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -ne $command) {
            $commandPath = if (-not [string]::IsNullOrWhiteSpace([string]$command.Source)) { [string]$command.Source } else { [string]$command.Path }
            if (-not [string]::IsNullOrWhiteSpace($commandPath)) { $candidates.Add($commandPath) }
        }
    }

    foreach ($programRoot in @($env:ProgramFiles, [Environment]::GetEnvironmentVariable("ProgramFiles(x86)"))) {
        if ([string]::IsNullOrWhiteSpace($programRoot)) { continue }
        $ghostscriptRoot = Join-Path $programRoot "gs"
        if (-not (Test-Path -LiteralPath $ghostscriptRoot -PathType Container)) { continue }
        Get-ChildItem -LiteralPath $ghostscriptRoot -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object {
                $candidates.Add((Join-Path $_.FullName "bin\gswin64c.exe"))
                $candidates.Add((Join-Path $_.FullName "bin\gswin32c.exe"))
            }
    }

    $visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        try { $fullPath = [IO.Path]::GetFullPath($candidate) }
        catch { continue }
        if (-not $visited.Add($fullPath)) { continue }
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) { return $fullPath }
    }
    return $null
}

function Optimize-LocalGptPdf {
    param(
        [Parameter(Mandatory)][string]$Path,
        [long]$MinimumBytes = 65536
    )

    $source = Get-Item -LiteralPath $Path -ErrorAction SilentlyContinue
    if ($null -eq $source) {
        return [pscustomobject]@{ Applied = $false; Mode = "none"; BeforeBytes = 0L; AfterBytes = 0L; SavedBytes = 0L; Diagnostic = "PDF was not found." }
    }

    $ghostscript = Find-LocalGptGhostscript
    if ([string]::IsNullOrWhiteSpace($ghostscript)) {
        return [pscustomobject]@{ Applied = $false; Mode = "none"; BeforeBytes = [long]$source.Length; AfterBytes = [long]$source.Length; SavedBytes = 0L; Diagnostic = "Ghostscript was not installed; the browser renderer's native compression was retained." }
    }

    $temporary = Join-Path $source.DirectoryName ("." + $source.Name + ".optimized.pdf")
    Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue
    $arguments = @(
        "-dNOPAUSE",
        "-dBATCH",
        "-dSAFER",
        "-sDEVICE=pdfwrite",
        "-dCompatibilityLevel=1.7",
        "-dAutoRotatePages=/None",
        "-dDetectDuplicateImages=true",
        "-dCompressFonts=true",
        "-dSubsetFonts=true",
        "-dCompressPages=true",
        "-dEmbedAllFonts=true",
        "-dDownsampleColorImages=false",
        "-dDownsampleGrayImages=false",
        "-dDownsampleMonoImages=false",
        "-sOutputFile=$temporary",
        $source.FullName
    )

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& $ghostscript @arguments 2>&1)
        $exitCode = [int]$LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    $optimized = Get-Item -LiteralPath $temporary -ErrorAction SilentlyContinue
    if ($exitCode -eq 0 -and $null -ne $optimized -and $optimized.Length -lt $source.Length -and (Test-LocalGptCompletePdf -Path $temporary -MinimumBytes $MinimumBytes)) {
        $before = [long]$source.Length
        $after = [long]$optimized.Length
        Move-Item -LiteralPath $temporary -Destination $source.FullName -Force
        return [pscustomobject]@{ Applied = $true; Mode = "ghostscript-lossless-resources"; BeforeBytes = $before; AfterBytes = $after; SavedBytes = ($before - $after); Diagnostic = "Optimized with $ghostscript" }
    }

    Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue
    $tail = @($output | Select-Object -Last 10) -join " | "
    if ([string]::IsNullOrWhiteSpace($tail)) { $tail = "No additional compressor diagnostic was emitted." }
    return [pscustomobject]@{ Applied = $false; Mode = "none"; BeforeBytes = [long]$source.Length; AfterBytes = [long]$source.Length; SavedBytes = 0L; Diagnostic = "Ghostscript exit code $exitCode; $tail" }
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
    param(
        [switch]$AllowProvisioning,
        [switch]$PreferCompatibleLts
    )

    $nodeInfo = Find-LocalGptNode
    if ($PreferCompatibleLts) {
        $cachedLts = Get-LocalGptNodeInfo -Path $provisionedNodeExecutable -Provisioned $true
        if ($null -ne $cachedLts) { return $cachedLts }
        if ($null -ne $nodeInfo -and $nodeInfo.Major -le $maximumPreferredNodeMajor) { return $nodeInfo }

        if ($AllowProvisioning) {
            try {
                return Install-LocalGptNode
            }
            catch {
                if ($null -ne $nodeInfo) {
                    Write-Warning "Compatible Node.js $provisionedNodeVersion provisioning failed; falling back to installed $($nodeInfo.Version): $($_.Exception.Message)"
                    return $nodeInfo
                }
                throw
            }
        }
    }

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
    if ([string]$metadataConfig.memberLayout -ne "samePage") {
        throw "DocFX memberLayout must be samePage so complete XML member documentation stays grouped by type and remains printable."
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

    # Windows PowerShell promotes native stderr records to terminating errors when the
    # script-wide ErrorActionPreference is Stop. DocFX's PDF renderer writes Node warnings
    # to stderr even on a successful run, so capture them as diagnostics and trust the
    # native exit code plus generated artifacts instead.
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = if ($script:useManifestTool) {
            @(& dotnet tool run docfx @Arguments 2>&1)
        }
        else {
            @(& $script:docfxExecutable @Arguments 2>&1)
        }
        $exitCode = [int]$LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    foreach ($entry in $output) {
        $line = [string]$entry
        # ConsoleToMSBuild scans text independently from the native exit code. Keep DocFX and
        # Node diagnostics visible without allowing a handled stderr line to become MSB3077.
        $line = [regex]::Replace($line, '(?i)\b(?:fatalerror|error)\s*:', 'diagnostic:')
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
                    $itemsMatch = [regex]::Match($apiTocText, '(?m)^items:\s*$')
                    if (-not $itemsMatch.Success) {
                        throw "The generated API TOC did not contain the expected root items node: $apiTocPath"
                    }
                    $updatedApiTocText = $apiTocText.Insert(
                        $itemsMatch.Index + $itemsMatch.Length,
                        "`r`n- name: API overview`r`n  href: index.md")
                    Set-Content -LiteralPath $apiTocPath -Value $updatedApiTocText -Encoding utf8
                }

                $buildResult = Invoke-LocalGptDocfx -Arguments @("build", $configPath)
                $apiHtmlRoot = Join-Path $siteRoot "api"
                $apiHtmlCount = @(Get-ChildItem -LiteralPath $apiHtmlRoot -Filter "*.html" -File -Recurse -ErrorAction SilentlyContinue).Count
                $docfxBuildSucceeded = $buildResult.ExitCode -eq 0 -and (Test-Path -LiteralPath (Join-Path $siteRoot "index.html") -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $apiHtmlRoot "index.html") -PathType Leaf) -and $apiHtmlCount -gt 1
                if ($docfxBuildSucceeded) {
                    $apiMemberPanelCount = Update-LocalGptApiPresentation -SiteRoot $siteRoot
                    if ($apiMemberPanelCount -eq 0) {
                        $warnings.Add("The DocFX site rendered successfully, but no API member panels were generated.")
                    }
                    $apiNavigationGroupCount = Update-LocalGptApiNavigation -SiteRoot $siteRoot
                    if ($apiNavigationGroupCount -eq 0) {
                        $warnings.Add("The DocFX site rendered successfully, but no API member-section navigation groups were discovered.")
                    }
                }
                else {
                    $warnings.Add("DocFX site generation did not render the complete API reference; static documentation was generated instead.")
                }
            }
        }
        catch {
            $htmlFailure = "DocFX HTML generation raised an exception: $($_.Exception.Message)"
            $warnings.Add($htmlFailure)
            Write-Warning $htmlFailure
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
        Get-ChildItem -LiteralPath $siteRoot -Filter "*.pdf" -File -Recurse -ErrorAction SilentlyContinue |
            Remove-Item -Force -ErrorAction SilentlyContinue
        Remove-LocalGptTemporaryPath -Path $printBookRoot
        $pdfSourcePageCount = @(Get-LocalGptPrintPageFiles -SiteRoot $siteRoot).Count

        # Prefer one browser-printed book while the API graph remains within a bounded source-page count.
        # It uses the same rendered HTML as the working site, embeds shared fonts once, and permits compact
        # print-only formatting. The official DocFX PDF plug-in remains the reliable compatibility fallback.
        if ($pdfSourcePageCount -gt 0 -and $pdfSourcePageCount -le $maximumBrowserPrintSourcePages) {
            try {
                $browser = Find-LocalGptDocumentationBrowser
                if ($null -ne $browser) {
                    $pdfSourcePageCount = New-LocalGptHtmlPrintBook -SiteRoot $siteRoot -DestinationPath $printBookPath
                    Write-Host "Printing $pdfSourcePageCount DocFX HTML pages as one compact LocalGPT PDF with $($browser.Name)." -ForegroundColor Cyan
                    $browserResult = Invoke-LocalGptBrowserPdf -BrowserPath $browser.Path -HtmlPath $printBookPath -PdfPath $pdfPath -WorkingRoot $printBookRoot
                    $pdfCandidateCount = if (Test-Path -LiteralPath $pdfPath -PathType Leaf) { 1 } else { 0 }
                    if ($browserResult.Succeeded) {
                        $pdfGenerated = Test-LocalGptCompletePdf -Path $pdfPath -MinimumBytes $minimumCompletePdfBytes
                        if ($pdfGenerated) {
                            $resolvedPdf = Get-Item -LiteralPath $pdfPath
                            $pdfFileSize = $resolvedPdf.Length
                            $pdfMode = "html-browser-print"
                            $pdfRenderer = [string]$browser.Name
                            $pdfGeneratedSourcePath = Get-LocalGptRelativePath -Root $docsRoot -Path $printBookPath
                        }
                        else {
                            $warnings.Add("The browser produced a PDF, but it was smaller than $minimumCompletePdfBytes bytes or did not have a valid PDF header.")
                        }
                    }

                    if (-not $pdfGenerated) {
                        $browserTail = @($browserResult.Diagnostics | Select-Object -Last 20) -join " | "
                        if ([string]::IsNullOrWhiteSpace($browserTail)) { $browserTail = "The browser returned no additional diagnostic." }
                        $warnings.Add("HTML browser PDF generation failed with exit code $($browserResult.ExitCode): $browserTail")
                    }
                }
                else {
                    $warnings.Add("Microsoft Edge, Google Chrome, or Chromium was not found for compact HTML-to-PDF printing.")
                }
            }
            catch {
                $warnings.Add("Complete DocFX HTML print-book generation failed: $($_.Exception.Message)")
            }
        }
        elseif ($pdfSourcePageCount -gt $maximumBrowserPrintSourcePages) {
            $warnings.Add("The DocFX site contains $pdfSourcePageCount printable HTML pages, above the browser-print limit of $maximumBrowserPrintSourcePages; using the DocFX PDF plug-in.")
        }

        if (-not $pdfGenerated) {
            Get-ChildItem -LiteralPath $siteRoot -Filter "*.pdf" -File -Recurse -ErrorAction SilentlyContinue |
                Remove-Item -Force -ErrorAction SilentlyContinue
            try {
                $nodeInfo = Resolve-LocalGptNode -AllowProvisioning -PreferCompatibleLts
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

                    Write-Host "Browser printing was unavailable; generating the complete PDF with the DocFX PDF plug-in and Node.js $nodeVersionUsed." -ForegroundColor Cyan
                    $pdfResult = Invoke-LocalGptDocfx -Arguments @("pdf", $configPath, "--logLevel", "info")
                    $pdfCandidates = @(
                        Get-ChildItem -LiteralPath $siteRoot -Filter "*.pdf" -File -Recurse -ErrorAction SilentlyContinue |
                            Sort-Object @{ Expression = { if ($_.Name -eq $pdfName) { 0 } else { 1 } } },
                                @{ Expression = { $_.Length }; Descending = $true }
                    )
                    $pdfCandidateCount = $pdfCandidates.Count
                    if ($pdfResult.ExitCode -eq 0 -and $pdfCandidateCount -gt 0) {
                        $docfxPdf = $pdfCandidates | Where-Object { $_.Name -eq $pdfName } | Select-Object -First 1
                        if ($null -eq $docfxPdf) { $docfxPdf = $pdfCandidates | Select-Object -First 1 }
                        $pdfGeneratedSourcePath = Get-LocalGptRelativePath -Root $siteRoot -Path $docfxPdf.FullName
                        if (-not [string]::Equals($docfxPdf.FullName, $pdfPath, [StringComparison]::OrdinalIgnoreCase)) {
                            Copy-Item -LiteralPath $docfxPdf.FullName -Destination $pdfPath -Force
                        }
                        $pdfGenerated = Test-LocalGptCompletePdf -Path $pdfPath -MinimumBytes $minimumCompletePdfBytes
                        if ($pdfGenerated) {
                            $resolvedPdf = Get-Item -LiteralPath $pdfPath
                            $pdfFileSize = $resolvedPdf.Length
                            $pdfMode = "docfx-pdf-plugin"
                            $pdfRenderer = "DocFX PDF plug-in"
                        }
                    }

                    if (-not $pdfGenerated) {
                        $candidateSummary = @($pdfCandidates | ForEach-Object { "$(Get-LocalGptRelativePath -Root $siteRoot -Path $_.FullName)=$($_.Length)" }) -join ", "
                        $diagnosticTail = @($pdfResult.Output | Select-Object -Last 30) -join " | "
                        if ([string]::IsNullOrWhiteSpace($diagnosticTail)) { $diagnosticTail = "DocFX returned no PDF diagnostics." }
                        $warnings.Add("DocFX PDF plug-in exited with code $($pdfResult.ExitCode); candidates=[$candidateSummary]: $diagnosticTail")
                    }
                }
                else {
                    $warnings.Add("Node.js $minimumNodeMajor or later was unavailable for DocFX PDF generation.")
                }
            }
            catch {
                $warnings.Add("DocFX PDF plug-in failed: $($_.Exception.Message)")
            }
        }

        if ($pdfGenerated) {
            $compression = Optimize-LocalGptPdf -Path $pdfPath -MinimumBytes $minimumCompletePdfBytes
            $pdfCompressionMode = [string]$compression.Mode
            $pdfBytesBeforeCompression = [long]$compression.BeforeBytes
            $pdfCompressionSavedBytes = [long]$compression.SavedBytes
            if ($compression.Applied) {
                $pdfFileSize = [long]$compression.AfterBytes
                Write-Host "Compressed the documentation PDF from $pdfBytesBeforeCompression to $pdfFileSize bytes without downsampling content." -ForegroundColor Green
            }
            elseif (-not [string]::IsNullOrWhiteSpace([string]$compression.Diagnostic)) {
                $warnings.Add([string]$compression.Diagnostic)
                $pdfFileSize = (Get-Item -LiteralPath $pdfPath).Length
            }
        }
    }
    elseif ($docfxBuildSucceeded) {
        $warnings.Add("Complete PDF generation was explicitly disabled; this HTML-only diagnostic build does not emit a fallback PDF.")
    }

    if ($RequirePdf -and -not $pdfGenerated) {
        $pdfFailureDetails = @(
            $warnings |
                Where-Object { $_ -match '(?i)(DocFX HTML|print-book|browser PDF|Microsoft Edge|Google Chrome|Chromium|DocFX PDF|Node\.js|Playwright)' } |
                Select-Object -Last 6
        ) -join " | "
        if ([string]::IsNullOrWhiteSpace($pdfFailureDetails)) {
            $pdfFailureDetails = "No additional PDF diagnostic was emitted."
        }
        throw "Complete documentation PDF generation failed. $pdfFailureDetails"
    }
}
finally {
    Pop-Location
    Remove-LocalGptTemporaryPath -Path $inputRoot
    Remove-LocalGptTemporaryPath -Path $printBookRoot -Attempts 8 -DelayMilliseconds 250
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
        apiNavigationGroupCount = $apiNavigationGroupCount
        pdfBytes = $pdfFileSize
        pdfBytesBeforeCompression = $pdfBytesBeforeCompression
        pdfCompressionMode = $pdfCompressionMode
        pdfCompressionSavedBytes = $pdfCompressionSavedBytes
        pdfGeneratedSourcePath = $pdfGeneratedSourcePath
        pdfCandidateCount = $pdfCandidateCount
        pdfRenderer = $pdfRenderer
        pdfSourcePageCount = $pdfSourcePageCount
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
