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

function Set-Utf8TextFileIdempotent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Content
    )

    $normalized = $Content.TrimEnd("`r", "`n") + [Environment]::NewLine
    if (Test-Path -LiteralPath $Path) {
        $existing = [IO.File]::ReadAllText($Path)
        if ($existing -ceq $normalized) { return }
    }

    [IO.File]::WriteAllText($Path, $normalized, [Text.UTF8Encoding]::new($false))
}

$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$AssemblyPath = [IO.Path]::GetFullPath($AssemblyPath)
$XmlDocumentationPath = [IO.Path]::GetFullPath($XmlDocumentationPath)
if (-not [string]::IsNullOrWhiteSpace($OutputWebRoot)) {
    $OutputWebRoot = [IO.Path]::GetFullPath($OutputWebRoot)
}

. (Join-Path $RepositoryRoot "build/NodeRuntime.Common.ps1")

Write-Host "LocalGPT documentation input: repository=$RepositoryRoot; assembly=$AssemblyPath; xml=$XmlDocumentationPath; version=$Version"

$docsRoot = Join-Path $RepositoryRoot "docs"
$inputRoot = Join-Path $docsRoot "input"
$siteRoot = Join-Path $docsRoot "_site"
$apiRoot = Join-Path $docsRoot "api"
$sourceWebRoot = Join-Path $RepositoryRoot "src/LocalGPT/wwwroot/help-docs"
$configPath = Join-Path $docsRoot "docfx.json"
$docfxDependencyProjectPath = Join-Path $docsRoot "DocfxDependencies.csproj"
$tocPath = Join-Path $docsRoot "toc.yml"
$guideTocPath = Join-Path $docsRoot "guide/toc.yml"
$pdfTocPath = Join-Path $docsRoot "pdf/toc.yml"
$pdfCoverPath = Join-Path $docsRoot "pdf-cover.html"
$manifestPath = Join-Path $RepositoryRoot ".config/dotnet-tools.json"
$fallbackToolRoot = Join-Path $docsRoot ".tools"
$internalNotesRoot = Join-Path $docsRoot "internal-notes"
$printBookParentRoot = Join-Path $docsRoot ".print-book"
$printBookRoot = Join-Path $printBookParentRoot ([Guid]::NewGuid().ToString('N'))
$printBookPath = Join-Path $printBookRoot "LocalGPT-$Version-complete.html"
$pdfName = "LocalGPT-$Version.pdf"
$pdfLinkStubPath = Join-Path $docsRoot $pdfName
$pdfLinkStubCreated = $false
$websiteThemeAssetCount = 0
$htmlPreflightValidated = $false
$warnings = [System.Collections.Generic.List[string]]::new()
$documentationMode = "static-fallback"
$pdfMode = "unavailable"
$toolSource = "unavailable"
$apiYamlCount = 0
$apiHtmlCount = 0
$apiNavigationGroupCount = 0
$xmlCommentPolishCount = 0
$unresolvedAssemblyReferences = @()
$docfxDependencyRepairCount = 0
$articleSourceCount = @(
    Get-ChildItem -LiteralPath $docsRoot -Filter "*.md" -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object {
            $_.FullName -notlike "$apiRoot\*" -and
            $_.FullName -notlike "$inputRoot\*" -and
            $_.FullName -notlike "$siteRoot\*" -and
            $_.FullName -notlike "$fallbackToolRoot\*" -and
            $_.FullName -notlike "$internalNotesRoot\*"
        }
).Count
$pdfFileSize = 0
$pdfGeneratedSourcePath = ""
$pdfCandidateCount = 0
$pdfRenderer = ""
$pdfSourcePageCount = 0
$minimumCompletePdfBytes = 1048576
$minimumNodeMajor = 20
$maximumPreferredNodeMajor = 22
$provisionedNodeVersion = "22.23.2"
$documentationToolCacheRoot = Get-LocalGptDocumentationToolCacheRoot -FallbackRoot $fallbackToolRoot
$playwrightBrowserRoot = Join-Path $documentationToolCacheRoot "ms-playwright-docfx-2.78.5"
$documentationLockRoot = Join-Path $documentationToolCacheRoot "locks"
$documentationLockPath = Join-Path $documentationLockRoot "LocalGPT-documentation.lock"
$documentationWorkRoot = Join-Path $documentationToolCacheRoot ("work/" + [Guid]::NewGuid().ToString('N'))
$polishedXmlPath = Join-Path $documentationWorkRoot "LocalGPT.xml"
$documentationLockStream = $null
$nodeVersionUsed = ""
$nodeProvisioned = $false
$nodePlatformUsed = ""
$nodeArchitectureUsed = ""
$pdfTimeoutMilliseconds = 1800000
$pdfCompressionMode = "none"
$pdfBytesBeforeCompression = 0
$pdfCompressionSavedBytes = 0
$maximumBrowserPrintSourcePages = if ([IO.Path]::DirectorySeparatorChar -eq '\') { 1500 } else { 1000 }

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


function Convert-LocalGptDocumentationSentence {
    param([AllowEmptyString()][string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) { return $Text }
    $leading = [regex]::Match($Text, '^\s*').Value
    $trailing = [regex]::Match($Text, '\s*$').Value
    $sentence = [regex]::Replace($Text.Trim(), '\s+', ' ')

    $sentence = [regex]::Replace($sentence, '(?i)\bwhether or not\b', 'whether')
    $sentence = [regex]::Replace($sentence, '(?i)\bin order to\b', 'to')
    $sentence = [regex]::Replace($sentence, '(?i)\breturns back\b', 'returns')
    $sentence = [regex]::Replace($sentence, '(?i)\butilizes\b', 'uses')
    $sentence = [regex]::Replace($sentence, '(?i)\bprovides the ability to\b', 'allows callers to')
    $sentence = [regex]::Replace($sentence, '(?i)\bcurrently existing\b', 'existing')

    if ($sentence -match '^(gets|sets|represents|provides|contains|creates|returns|configures|invokes|validates|records|describes|indicates|determines|initializes|loads|saves|updates|deletes|adds|removes|builds|executes|converts|parses|formats|resolves|checks|stores|reads|writes|tracks|maps|ensures|specifies|defines)\b') {
        $sentence = $sentence.Substring(0, 1).ToUpperInvariant() + $sentence.Substring(1)
    }

    if ($sentence.Length -gt 2 -and
        $sentence -notmatch '[.!?…:;\)\]\}]$' -and
        $sentence -notmatch '^[A-Za-z_][A-Za-z0-9_.<>`]*$') {
        $sentence += '.'
    }

    return $leading + $sentence + $trailing
}

function Write-LocalGptPolishedXmlDocumentation {
    param(
        [Parameter(Mandatory)][string]$SourcePath,
        [Parameter(Mandatory)][string]$DestinationPath
    )

    [xml]$document = Get-Content -LiteralPath $SourcePath -Raw -Encoding UTF8
    $polishedCount = 0
    $nodes = @($document.SelectNodes('/doc/members/member/summary | /doc/members/member/remarks | /doc/members/member/returns | /doc/members/member/value | /doc/members/member/param | /doc/members/member/typeparam | /doc/members/member/exception'))
    foreach ($node in $nodes) {
        # Complex XML comments retain their authored structure. Simple prose-only comments receive
        # conservative grammar normalization without changing identifiers, links, examples or code.
        if ($node.ChildNodes.Count -ne 1 -or
            ($node.FirstChild.NodeType -ne [Xml.XmlNodeType]::Text -and $node.FirstChild.NodeType -ne [Xml.XmlNodeType]::CDATA)) {
            continue
        }

        $before = [string]$node.FirstChild.Value
        $after = Convert-LocalGptDocumentationSentence -Text $before
        if ([string]::Equals($before, $after, [StringComparison]::Ordinal)) { continue }
        $node.FirstChild.Value = $after
        $polishedCount++
    }

    $settings = [Xml.XmlWriterSettings]::new()
    $settings.Encoding = [Text.UTF8Encoding]::new($false)
    $settings.Indent = $true
    $settings.NewLineHandling = [Xml.NewLineHandling]::Entitize
    $writer = [Xml.XmlWriter]::Create($DestinationPath, $settings)
    try { $document.Save($writer) }
    finally { $writer.Dispose() }
    return $polishedCount
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

    [xml]$xml = Get-Content -LiteralPath $XmlPath -Raw -Encoding UTF8
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
        Set-Content -LiteralPath (Join-Path $Destination "api/$fileName") -Value (Get-LocalGptHtmlPage -Title $typeName -Body $body -RelativePrefix "../") -Encoding utf8
        $apiLinks.Add("<li><a href=`"$fileName`">$(ConvertTo-LocalGptHtml $typeName)</a></li>")
    }
    $apiBody = "<h1>API reference</h1><p class=`"muted`">$($types.Count) documented types and $($members.Count) compiler XML members.</p><ul>$($apiLinks -join '')</ul>"
    Set-Content -LiteralPath (Join-Path $Destination "api/index.html") -Value (Get-LocalGptHtmlPage -Title "API reference" -Body $apiBody -RelativePrefix "../") -Encoding utf8

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
        $markdown = Get-Content -LiteralPath $article.FullName -Raw -Encoding UTF8
        $body = Convert-LocalGptMarkdownToHtml $markdown
        Set-Content -LiteralPath (Join-Path $Destination "articles/$target") -Value (Get-LocalGptHtmlPage -Title $name -Body $body -RelativePrefix "../") -Encoding utf8
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


function Assert-LocalGptGeneratedHtmlPreflight {
    param([Parameter(Mandatory)][string]$SiteRoot)

    $validator = Join-Path $RepositoryRoot ".github/scripts/prepare-pages-artifact.py"
    if (-not (Test-Path -LiteralPath $validator -PathType Leaf)) {
        throw "The LocalGPT documentation HTML preflight validator was not found: $validator"
    }

    $python = Get-Command python -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $python) {
        $python = Get-Command python3 -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    }
    if ($null -eq $python) {
        throw "Python 3 is required to validate generated documentation HTML before the long PDF render."
    }

    Write-Host "Validating generated DocFX HTML accessibility and local links before the long PDF render..." -ForegroundColor DarkCyan
    & $python.Source $validator --source $SiteRoot --html-only
    if ($LASTEXITCODE -ne 0) {
        throw "Generated DocFX HTML failed the pre-PDF accessibility/link validation. PDF rendering was skipped so the release fails fast."
    }

    Write-Host "Generated DocFX HTML preflight passed before PDF rendering." -ForegroundColor DarkGreen
}

function New-LocalGptDocfxPdfLinkStub {
    param([Parameter(Mandatory)][string]$Path)

    $stub = @"
%PDF-1.4
% LocalGPT build-time DocFX link-validation placeholder
1 0 obj
<< /Type /Catalog >>
endobj
trailer
<< /Root 1 0 R >>
%%EOF
"@
    [IO.File]::WriteAllText($Path, $stub, [Text.Encoding]::ASCII)
}

function Test-LocalGptDocfxPdfLinkStub {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $false }
    try {
        $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
        try {
            $bufferLength = [Math]::Min(512, [int]$stream.Length)
            $buffer = New-Object byte[] $bufferLength
            $read = $stream.Read($buffer, 0, $buffer.Length)
            if ($read -le 0) { return $false }
            $prefixText = [Text.Encoding]::ASCII.GetString($buffer, 0, $read)
            return $prefixText.IndexOf('LocalGPT build-time DocFX link-validation placeholder', [StringComparison]::Ordinal) -ge 0
        }
        finally { $stream.Dispose() }
    }
    catch {
        return $false
    }
}

function Install-LocalGptWebsiteThemeAssets {
    param([Parameter(Mandatory)][string]$SiteRoot)

    $themeSourceRoot = Join-Path $docsRoot "templates/localgpt/public"
    $cssSource = Join-Path $themeSourceRoot "main.css"
    $javascriptSource = Join-Path $themeSourceRoot "main.js"
    $faviconSource = Join-Path $themeSourceRoot "favicon.ico"
    $faviconSvgSource = Join-Path $themeSourceRoot "favicon.svg"
    $logoSource = Join-Path $themeSourceRoot "logo.svg"
    foreach ($requiredThemeSource in @($cssSource, $javascriptSource, $faviconSource, $faviconSvgSource, $logoSource)) {
        if (-not (Test-Path -LiteralPath $requiredThemeSource -PathType Leaf)) {
            throw "The LocalGPT DocFX website theme source is incomplete: $requiredThemeSource"
        }
    }

    $assetRoot = Join-Path $SiteRoot "styles"
    New-Item -ItemType Directory -Path $assetRoot -Force | Out-Null
    $cssTarget = Join-Path $assetRoot "localgpt-kawaii.css"
    $javascriptTarget = Join-Path $assetRoot "localgpt-kawaii.js"
    Copy-Item -LiteralPath $cssSource -Destination $cssTarget -Force
    Copy-Item -LiteralPath $javascriptSource -Destination $javascriptTarget -Force
    Copy-Item -LiteralPath $faviconSource -Destination (Join-Path $SiteRoot "favicon.ico") -Force
    Copy-Item -LiteralPath $faviconSvgSource -Destination (Join-Path $SiteRoot "favicon.svg") -Force
    Copy-Item -LiteralPath $logoSource -Destination (Join-Path $SiteRoot "logo.svg") -Force

    $cssHash = (Get-FileHash -LiteralPath $cssTarget -Algorithm SHA256).Hash.Substring(0, 12).ToLowerInvariant()
    $javascriptHash = (Get-FileHash -LiteralPath $javascriptTarget -Algorithm SHA256).Hash.Substring(0, 12).ToLowerInvariant()
    $faviconSvgHash = (Get-FileHash -LiteralPath (Join-Path $SiteRoot "favicon.svg") -Algorithm SHA256).Hash.Substring(0, 12).ToLowerInvariant()
    $faviconIcoHash = (Get-FileHash -LiteralPath (Join-Path $SiteRoot "favicon.ico") -Algorithm SHA256).Hash.Substring(0, 12).ToLowerInvariant()
    $themeBootstrap = @'
<script data-localgpt-theme-bootstrap="true">
(function () {
  var cookieName = "localgpt-docs-theme";
  var valid = { light: true, dark: true, auto: true };
  var preference = null;
  var prefix = encodeURIComponent(cookieName) + "=";
  try {
    document.cookie.split(";").some(function (part) {
      part = part.trim();
      if (part.indexOf(prefix) === 0) {
        try { preference = decodeURIComponent(part.substring(prefix.length)); }
        catch (_) { preference = null; }
        return true;
      }
      return false;
    });
    if (!valid[preference]) preference = localStorage.getItem(cookieName);
    if (!valid[preference]) preference = localStorage.getItem("theme");
  } catch (_) { }
  if (!valid[preference]) preference = "auto";
  var resolved = preference === "auto"
    ? (window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light")
    : preference;
  document.documentElement.dataset.localgptThemePreference = preference;
  document.documentElement.setAttribute("data-bs-theme", resolved);
  try {
    localStorage.setItem(cookieName, preference);
    localStorage.setItem("theme", preference);
  } catch (_) { }
  var secure = location.protocol === "https:" ? "; Secure" : "";
  document.cookie = prefix + encodeURIComponent(preference) + "; Max-Age=31536000; Path=/; SameSite=Lax" + secure;
})();
</script>
'@

    $siteRootFull = [IO.Path]::GetFullPath($SiteRoot)
    $updatedCount = 0
    foreach ($file in @(Get-ChildItem -LiteralPath $siteRootFull -Filter "*.html" -File -Recurse -ErrorAction SilentlyContinue)) {
        if ($file.FullName -like "*\.print-book\*") { continue }
        $relative = (Get-LocalGptRelativePath -Root $siteRootFull -Path $file.FullName).Replace('\', '/')
        $depth = [Math]::Max(0, @($relative.Split('/') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count - 1)
        $prefixPath = if ($depth -gt 0) { ("../" * $depth) -join "" } else { "" }
        $cssHref = $prefixPath + "styles/localgpt-kawaii.css?v=$cssHash"
        $javascriptHref = $prefixPath + "styles/localgpt-kawaii.js?v=$javascriptHash"
        # Version favicon URLs because browsers cache site icons more aggressively than normal assets.
        $faviconSvgHref = $prefixPath + "favicon.svg?v=$faviconSvgHash"
        $faviconIcoHref = $prefixPath + "favicon.ico?v=$faviconIcoHash"
        $html = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        $updated = $html

        if ($updated -notmatch '(?i)<html\b[^>]*\blocalgpt-kawaii-docs\b') {
            if ($updated -match '(?i)<html\b[^>]*\bclass\s*=\s*"') {
                $updated = [regex]::Replace($updated, '(?i)(<html\b[^>]*\bclass\s*=\s*")', '${1}localgpt-kawaii-docs ', 1)
            }
            elseif ($updated -match "(?i)<html\b[^>]*\bclass\s*=\s*'") {
                $updated = [regex]::Replace($updated, "(?i)(<html\b[^>]*\bclass\s*=\s*')", '${1}localgpt-kawaii-docs ', 1)
            }
            else {
                $updated = [regex]::Replace($updated, '(?i)<html\b', '<html class="localgpt-kawaii-docs"', 1)
            }
        }

        # DocFX modern API pages can omit the html language attribute on some hosts. Keep the
        # generated site accessible and deterministic before PDF/Pages validation rather than
        # discovering this only after the expensive PDF render has completed.
        if ($updated -notmatch '(?i)<html\b[^>]*\blang\s*=\s*["''][^"'']+["'']') {
            if ($updated -match '(?i)<html\b[^>]*\blang\s*=') {
                $updated = [regex]::Replace($updated, '(?i)(<html\b[^>]*\blang\s*=\s*)["''][^"'']*["'']', '${1}"en"', 1)
            }
            else {
                $updated = [regex]::Replace($updated, '(?i)<html\b', '<html lang="en"', 1)
            }
        }

        if ($updated -notmatch '(?i)data-localgpt-theme-bootstrap') {
            if ($updated -match '(?i)</head>') {
                $updated = [regex]::Replace($updated, '(?i)</head>', $themeBootstrap + "`r`n</head>", 1)
            }
        }

        $iconTags = '<link rel="icon" type="image/svg+xml" href="' + $faviconSvgHref + '" data-localgpt-favicon="true" />' + "`r`n" +
            '<link rel="alternate icon" href="' + $faviconIcoHref + '" />'
        if ($updated -match '(?i)<link\s+rel=["'']icon["''][^>]*>') {
            $updated = [regex]::Replace($updated, '(?i)<link\s+rel=["'']icon["''][^>]*>', $iconTags, 1)
        }
        elseif ($updated -notmatch '(?i)data-localgpt-favicon' -and $updated -match '(?i)</head>') {
            $updated = [regex]::Replace($updated, '(?i)</head>', $iconTags + "`r`n</head>", 1)
        }

        if ($updated -notmatch '(?i)data-localgpt-kawaii-style') {
            $styleTag = '<link rel="stylesheet" href="' + $cssHref + '" data-localgpt-kawaii-style="true" />'
            if ($updated -match '(?i)</head>') {
                $updated = [regex]::Replace($updated, '(?i)</head>', $styleTag + "`r`n</head>", 1)
            }
        }
        if ($updated -notmatch '(?i)data-localgpt-kawaii-script') {
            $scriptTag = '<script type="module" src="' + $javascriptHref + '" data-localgpt-kawaii-script="true"></script>'
            if ($updated -match '(?i)</body>') {
                $updated = [regex]::Replace($updated, '(?i)</body>', $scriptTag + "`r`n</body>", 1)
            }
        }

        # Repair only the known DocFX short-link mismatches that point at real maintained pages.
        $updated = $updated.Replace('href="IRegexPatternService.html"', 'href="LocalGPT.Interfaces.IRegexPatternService.html"')
        $updated = $updated.Replace('href="IRegexFunctionParameterService.html"', 'href="LocalGPT.Interfaces.IRegexFunctionParameterService.html"')

        if (-not [string]::Equals($updated, $html, [StringComparison]::Ordinal)) {
            [IO.File]::WriteAllText($file.FullName, $updated, [Text.UTF8Encoding]::new($false))
            $updatedCount++
        }
    }

    return $updatedCount
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
        $candidates.Add([pscustomobject]@{ Path = (Join-Path $env:ProgramFiles "Microsoft/Edge/Application/msedge.exe"); Name = "Microsoft Edge" })
        $candidates.Add([pscustomobject]@{ Path = (Join-Path $env:ProgramFiles "Google/Chrome/Application/chrome.exe"); Name = "Google Chrome" })
    }
    if (-not [string]::IsNullOrWhiteSpace($programFilesX86)) {
        $candidates.Add([pscustomobject]@{ Path = (Join-Path $programFilesX86 "Microsoft/Edge/Application/msedge.exe"); Name = "Microsoft Edge" })
        $candidates.Add([pscustomobject]@{ Path = (Join-Path $programFilesX86 "Google/Chrome/Application/chrome.exe"); Name = "Google Chrome" })
    }
    if (-not [string]::IsNullOrWhiteSpace($localApplicationData)) {
        $candidates.Add([pscustomobject]@{ Path = (Join-Path $localApplicationData "Microsoft/Edge/Application/msedge.exe"); Name = "Microsoft Edge" })
        $candidates.Add([pscustomobject]@{ Path = (Join-Path $localApplicationData "Google/Chrome/Application/chrome.exe"); Name = "Google Chrome" })
    }

    if ([IO.Path]::DirectorySeparatorChar -ne '\') {
        $unixName = ''
        try { $unixName = [string](& uname -s 2>$null | Select-Object -First 1) } catch { $unixName = '' }
        if ([string]::Equals($unixName.Trim(), 'Darwin', [StringComparison]::OrdinalIgnoreCase)) {
            $homePath = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
            $applicationRoots = [System.Collections.Generic.List[string]]::new()
            $applicationRoots.Add('/Applications')
            if (-not [string]::IsNullOrWhiteSpace($homePath)) { $applicationRoots.Add((Join-Path $homePath 'Applications')) }
            foreach ($applicationRoot in $applicationRoots) {
                $candidates.Add([pscustomobject]@{ Path = (Join-Path $applicationRoot 'Google Chrome.app/Contents/MacOS/Google Chrome'); Name = 'Google Chrome' })
                $candidates.Add([pscustomobject]@{ Path = (Join-Path $applicationRoot 'Microsoft Edge.app/Contents/MacOS/Microsoft Edge'); Name = 'Microsoft Edge' })
                $candidates.Add([pscustomobject]@{ Path = (Join-Path $applicationRoot 'Chromium.app/Contents/MacOS/Chromium'); Name = 'Chromium' })
            }
        }
    }

    foreach ($commandName in @("msedge", "microsoft-edge", "microsoft-edge-stable", "chrome", "google-chrome", "google-chrome-stable", "chromium", "chromium-browser")) {
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
        $tocHtml = Get-Content -LiteralPath $tocPath -Raw -Encoding UTF8
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
        "Constructors" { return [pscustomobject]@{ Key = "constructors"; IconHtml = "&#x2728;"; Accent = "#a855f7" } }
        "Fields" { return [pscustomobject]@{ Key = "fields"; IconHtml = "&#x1F9F6;"; Accent = "#f59e0b" } }
        "Properties" { return [pscustomobject]@{ Key = "properties"; IconHtml = "&#x1F43E;"; Accent = "#06b6d4" } }
        "Methods" { return [pscustomobject]@{ Key = "methods"; IconHtml = "&#x2699;&#xFE0F;"; Accent = "#3b82f6" } }
        "Events" { return [pscustomobject]@{ Key = "events"; IconHtml = "&#x26A1;"; Accent = "#ec4899" } }
        "Operators" { return [pscustomobject]@{ Key = "operators"; IconHtml = "&#x2797;"; Accent = "#f97316" } }
        "Explicit Interface Implementations" { return [pscustomobject]@{ Key = "explicit-interface-implementations"; IconHtml = "&#x1F517;"; Accent = "#14b8a6" } }
        "Extension Methods" { return [pscustomobject]@{ Key = "extension-methods"; IconHtml = "&#x1F9E9;"; Accent = "#22c55e" } }
    }
    return $null
}

function Get-LocalGptApiKindPresentation {
    param([AllowEmptyString()][string]$Kind)

    switch ($Kind) {
        "Interface" { return [pscustomobject]@{ Key = "interfaces"; Label = "Interfaces"; IconHtml = "&#x1F43E;"; Accent = "#8b5cf6"; Order = 10 } }
        "Class" { return [pscustomobject]@{ Key = "classes"; Label = "Classes"; IconHtml = "&#x2728;"; Accent = "#ec4899"; Order = 20 } }
        "Struct" { return [pscustomobject]@{ Key = "structs"; Label = "Structs"; IconHtml = "&#x1F9E9;"; Accent = "#06b6d4"; Order = 30 } }
        "Record" { return [pscustomobject]@{ Key = "records"; Label = "Records"; IconHtml = "&#x1F4DA;"; Accent = "#6366f1"; Order = 40 } }
        "Enum" { return [pscustomobject]@{ Key = "enums"; Label = "Enums"; IconHtml = "&#x1F3A8;"; Accent = "#f59e0b"; Order = 50 } }
        "Delegate" { return [pscustomobject]@{ Key = "delegates"; Label = "Delegates"; IconHtml = "&#x1F380;"; Accent = "#f472b6"; Order = 60 } }
        default { return [pscustomobject]@{ Key = "other-types"; Label = "Other types"; IconHtml = "&#x1F338;"; Accent = "#64748b"; Order = 90 } }
    }
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

function Convert-LocalGptApiKawaiiDetails {
    param([Parameter(Mandatory)][string]$Html)

    $updated = [regex]::Replace($Html, '(?i)>\s*Property Value\s*<', '>Value<')
    $updated = [regex]::Replace($updated, '(?i)>\s*Field Value\s*<', '>Value<')
    if ($updated -match '(?i)localgpt-api-neko-note') { return $updated }

    $headingMatch = [regex]::Match($updated, '(?is)<h1\b[^>]*>(?<value>.*?)</h1>')
    if (-not $headingMatch.Success) { return $updated }
    $headingText = [regex]::Replace([Net.WebUtility]::HtmlDecode([regex]::Replace($headingMatch.Groups['value'].Value, '<[^>]+>', ' ')), '\s+', ' ').Trim()
    $isNamespace = $headingText.StartsWith('Namespace ', [StringComparison]::OrdinalIgnoreCase)
    $label = if ($isNamespace) { 'Neko & Puppy namespace shelf' } else { 'Neko & Puppy type guide' }
    $message = if ($isNamespace) {
        'Types are grouped by kind, so every interface, class, and helper lands in a cozy little home, nya~ woof!' 
    }
    else {
        'Tiny paws, tidy details: constructors, properties, methods, and events stay cuddled up with the type that owns them, nya~ woof!' 
    }
    $note = '<aside class="localgpt-api-neko-note" role="note"><span class="localgpt-api-neko-mascot" aria-hidden="true">&#x1F431;&#x2009;&#x1F436;</span><span><strong>' + $label + '</strong> · ' + $message + '</span></aside>'
    return $updated.Insert($headingMatch.Index + $headingMatch.Length, $note)
}

function Update-LocalGptApiPresentation {
    param([Parameter(Mandatory)][string]$SiteRoot)

    $apiSiteRoot = Join-Path $SiteRoot "api"
    if (-not (Test-Path -LiteralPath $apiSiteRoot -PathType Container)) { return 0 }
    $panelCount = 0
    foreach ($file in @(Get-ChildItem -LiteralPath $apiSiteRoot -Filter "*.html" -File -Recurse -ErrorAction SilentlyContinue)) {
        if ($file.Name -in @("toc.html", "index.html", "404.html", "search.html")) { continue }
        $html = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        $result = Convert-LocalGptApiMemberPanels -Html $html
        $updatedHtml = Convert-LocalGptApiKawaiiDetails -Html ([string]$result.Html)
        if ([string]::Equals($updatedHtml, $html, [StringComparison]::Ordinal)) { continue }
        [IO.File]::WriteAllText($file.FullName, $updatedHtml, [Text.UTF8Encoding]::new($false))
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

    $pageMap = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @(Get-ChildItem -LiteralPath $apiSiteRoot -Filter "*.html" -File -Recurse -ErrorAction SilentlyContinue)) {
        if ($file.Name -in @("toc.html", "index.html", "404.html", "search.html")) { continue }
        $html = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        $title = Get-LocalGptHtmlDocumentTitle -Html $html -Fallback $file.BaseName
        $body = Get-LocalGptHtmlDocumentBody -Html $html
        $metadata = Get-LocalGptApiPageMetadata -Title $title -Html $body
        $relative = (Get-LocalGptRelativePath -Root $apiSiteRoot -Path $file.FullName).Replace('\', '/')
        $pageMap[$relative] = [pscustomobject]@{
            Kind = [string]$metadata.Kind
            DisplayName = [string]$metadata.DisplayName
            Namespace = [string]$metadata.Namespace
            MemberSections = @($metadata.MemberSections)
        }
    }
    if ($pageMap.Count -eq 0) { return 0 }

    $tocHtml = Get-Content -LiteralPath $tocHtmlPath -Raw -Encoding UTF8
    $script:localGptNavigationGroupCounter = 0
    $evaluator = [Text.RegularExpressions.MatchEvaluator]{
        param($match)
        $href = [Net.WebUtility]::HtmlDecode($match.Groups['href'].Value)
        $hrefPath = $href.Split('#')[0].Split('?')[0]
        if ([string]::IsNullOrWhiteSpace($hrefPath) -or [Uri]::IsWellFormedUriString($hrefPath, [UriKind]::Absolute)) { return $match.Value }
        try { $normalized = [Uri]::UnescapeDataString($hrefPath).Replace('\', '/').TrimStart([char]'/') }
        catch { return $match.Value }
        if (-not $pageMap.ContainsKey($normalized)) { return $match.Value }

        $metadata = $pageMap[$normalized]
        $kindPresentation = Get-LocalGptApiKindPresentation -Kind ([string]$metadata.Kind)
        $attributes = $match.Groups['attrs'].Value
        $anchorHtml = '<a' + $attributes + ' data-localgpt-api-kind="' + (ConvertTo-LocalGptHtml ([string]$metadata.Kind)) + '" data-localgpt-api-namespace="' + (ConvertTo-LocalGptHtml ([string]$metadata.Namespace)) + '">'
        if ([string]$metadata.Kind -eq 'Namespace') {
            $anchorHtml += '<span class="localgpt-api-type-icon" aria-hidden="true">&#x1F431;</span>' + (ConvertTo-LocalGptHtml ([string]$metadata.DisplayName))
        }
        else {
            $anchorHtml += '<span class="localgpt-api-type-icon" aria-hidden="true">' + [string]$kindPresentation.IconHtml + '</span>' + (ConvertTo-LocalGptHtml ([string]$metadata.DisplayName))
        }
        $anchorHtml += '</a>'

        if (@($metadata.MemberSections).Count -gt 0) {
            $links = [Text.StringBuilder]::new()
            [void]$links.Append('<ul class="nav localgpt-api-member-groups" aria-label="API member groups">')
            foreach ($section in @($metadata.MemberSections)) {
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
            $anchorHtml += $links.ToString()
        }

        $script:localGptNavigationGroupCounter++
        return $anchorHtml
    }

    $updated = [regex]::Replace(
        $tocHtml,
        '(?is)<a(?<attrs>\b[^>]*\bhref=["''](?<href>[^"'']+\.html(?:[?#][^"'']*)?)["''][^>]*)>.*?</a>(?:\s*<ul\b[^>]*\blocalgpt-api-member-groups\b[^>]*>.*?</ul>)?',
        $evaluator)
    $groupCount = [int]$script:localGptNavigationGroupCounter
    Remove-Variable -Name localGptNavigationGroupCounter -Scope Script -ErrorAction SilentlyContinue

    if ($groupCount -gt 0 -and $updated -notmatch 'localgpt-group-api-navigation') {
        $navigationScript = @'
<script id="localgpt-group-api-navigation">
(() => {
  const order = ["Interface", "Class", "Struct", "Record", "Enum", "Delegate", "API"];
  const presentations = {
    Interface: ["🐶", "Interfaces", "interfaces"],
    Class: ["🐱", "Classes", "classes"],
    Struct: ["🐾", "Structs", "structs"],
    Record: ["📚", "Records", "records"],
    Enum: ["🌟", "Enums", "enums"],
    Delegate: ["🎀", "Delegates", "delegates"],
    API: ["🌸", "Other types", "other-types"]
  };
  document.querySelectorAll('a[data-localgpt-api-kind="Namespace"]').forEach(namespaceLink => {
    const namespaceItem = namespaceLink.closest('li');
    if (!namespaceItem) return;
    const childList = Array.from(namespaceItem.children).find(child => child.tagName === 'UL');
    if (!childList || childList.dataset.localgptGrouped === 'true') return;
    const items = Array.from(childList.children).filter(item => {
      const link = Array.from(item.children).find(child => child.matches?.('a[data-localgpt-api-kind]'));
      return link && link.dataset.localgptApiKind !== 'Namespace';
    });
    if (!items.length) return;
    const buckets = new Map();
    items.forEach(item => {
      const link = Array.from(item.children).find(child => child.matches?.('a[data-localgpt-api-kind]'));
      const kind = link?.dataset.localgptApiKind || 'API';
      if (!buckets.has(kind)) buckets.set(kind, []);
      buckets.get(kind).push(item);
    });
    items.forEach(item => item.remove());
    order.filter(kind => buckets.has(kind)).forEach(kind => {
      const [icon, label, key] = presentations[kind] || presentations.API;
      const wrapper = document.createElement('li');
      wrapper.className = `nav-item localgpt-api-kind-navigation-group localgpt-api-kind-navigation-group--${key}`;
      wrapper.innerHTML = `<div class="localgpt-api-kind-navigation-heading"><span aria-hidden="true">${icon}</span><span>${label}</span><span class="localgpt-api-kind-navigation-count">${buckets.get(kind).length}</span></div><ul class="nav"></ul>`;
      const list = wrapper.querySelector('ul');
      buckets.get(kind).sort((left, right) => {
        const leftText = left.querySelector(':scope > a')?.textContent || '';
        const rightText = right.querySelector(':scope > a')?.textContent || '';
        return leftText.localeCompare(rightText, undefined, { sensitivity: 'base' });
      }).forEach(item => list.appendChild(item));
      childList.appendChild(wrapper);
    });
    childList.dataset.localgptGrouped = 'true';
  });
})();
</script>
'@
        $updated = [regex]::Replace($updated, '(?i)</body>', $navigationScript + '</body>', 1)
    }

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
        $html = Get-Content -LiteralPath $page.FullName -Raw -Encoding UTF8
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
html, body { background: linear-gradient(180deg, #fff6fb, #fff0f8) !important; color: #4b2159 !important; font-family: "Trebuchet MS", "Segoe UI", Arial, sans-serif !important; font-size: 9.35pt; line-height: 1.38; }
body { margin: 0 !important; max-width: none !important; }
.localgpt-print-cover { min-height: 178mm; display: flex; flex-direction: column; justify-content: center; break-after: page; }
.localgpt-print-cover::before { content: "🐾 🐱 🐶 LOCALGPT · KAWAII DOCS 2026"; color: #d946ef; font-family: "Segoe UI Emoji", "Segoe UI", sans-serif; font-size: 9pt; font-weight: 800; letter-spacing: .12em; margin-bottom: 12pt; }
.localgpt-print-cover h1 { color: #ffffff; text-shadow: 0 1pt 0 rgba(155, 81, 224, .55), 0 0 8pt rgba(236, 72, 153, .18); font-size: 31pt; font-weight: 700; line-height: 1.06; margin: 0 0 12pt; }
.localgpt-print-cover p { color: #6b3b79; font-size: 11.5pt; margin: 3pt 0; max-width: 64rem; }
.localgpt-print-toc { break-after: page; }
.localgpt-print-toc > h1 { border-bottom: 2px solid #d8b4fe; color: #171717; font-size: 24pt; margin: 0 0 13pt; padding-bottom: 6pt; }
.localgpt-print-toc-section { margin: 0 0 14pt; }
.localgpt-print-toc-section > h2 { color: #323130; font-size: 14pt; margin: 10pt 0 5pt; }
.localgpt-print-toc-conceptual { columns: 3; column-gap: 1.35rem; padding-left: 1.2rem; }
.localgpt-print-toc-conceptual li { break-inside: avoid; font-size: 8.5pt; line-height: 1.25; margin: 1.2pt 0; }
.localgpt-print-api-overview-link { font-size: 9pt; font-weight: 600; margin: 3pt 0 8pt; }
.localgpt-print-toc-namespace { background: linear-gradient(135deg, #fdf4ff, #f0f9ff 72%); border: 1px solid #eadcf8; border-radius: 8pt; break-inside: auto; margin-top: 8pt; padding: 6pt 7pt 7pt; }
.localgpt-print-toc-namespace h3 { align-items: center; break-after: avoid; color: #7c3aed; display: flex; font-size: 11pt; gap: 4pt; margin: 0 0 5pt; }
.localgpt-print-toc-kind-list { columns: 3; column-gap: 1.25rem; list-style: none; margin: 0; padding: 0; }
.localgpt-print-toc-kind-list > li { break-inside: avoid; font-size: 7.8pt; line-height: 1.22; margin: 1.4pt 0; }
.localgpt-print-toc-kind { --localgpt-api-kind-accent: #8b5cf6; margin-top: 5pt; }
.localgpt-print-toc-kind h4 { align-items: center; break-after: avoid; color: var(--localgpt-api-kind-accent); display: flex; font-size: 8.5pt; gap: 3pt; margin: 0 0 2pt; }
.localgpt-print-toc-kind-icon { font-family: "Segoe UI Emoji", "Segoe UI Symbol", sans-serif; }
.localgpt-print-toc-kind-count { background: #fff; border: 1px solid color-mix(in srgb, var(--localgpt-api-kind-accent) 22%, #e5e7eb); border-radius: 999px; color: #555; font-size: 6pt; margin-left: 2pt; padding: .5pt 3pt; }
.localgpt-print-member-links { display: block; margin-left: 8pt; }
.localgpt-print-member-links a { color: #5c5c5c !important; display: inline-block; font-size: 6.8pt; margin-right: 5pt; }
.localgpt-print-document { break-before: page; page-break-before: always; }
.localgpt-print-document:first-of-type { break-before: auto; page-break-before: auto; }
.localgpt-print-workspace { break-before: page; page-break-before: always; }
.localgpt-print-api-document { break-before: auto; page-break-before: auto; border-top: 1px solid #e1dfdd; margin-top: 13pt; padding-top: 10pt; }
.localgpt-print-api-namespace { border-top: 0; break-before: page !important; page-break-before: always !important; margin-top: 0; padding-top: 0; }
.localgpt-print-api-namespace + .localgpt-print-api-document { border-top: 0; margin-top: 9pt; }
.localgpt-print-source { align-items: baseline; border-bottom: 1px solid #d1d1d1; color: #605e5c; display: flex; font-size: 7.5pt; gap: 1rem; justify-content: space-between; margin-bottom: 8pt; padding-bottom: 4pt; }
.localgpt-print-workspace-header { border-bottom-color: #0067b8; color: #4a4a4a; }
.localgpt-print-api-breadcrumb { border-bottom-color: #d8b4fe; color: #4a4a4a; }
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
.localgpt-print-content .localgpt-api-neko-note { align-items: center; background: linear-gradient(90deg, #fdf4ff, #eff6ff); border: 1px solid #eadcf8; border-radius: 7pt; color: #5b456d; display: flex; font-size: 7.5pt; gap: 5pt; margin: 0 0 9pt; padding: 4.5pt 7pt; }
.localgpt-print-content .localgpt-api-neko-mascot { font-family: "Segoe UI Emoji", "Segoe UI Symbol", sans-serif; font-size: 12pt; }
.localgpt-print-content .localgpt-api-member-panel--constructors .localgpt-api-member-item,
.localgpt-print-content .localgpt-api-member-panel--fields .localgpt-api-member-item,
.localgpt-print-content .localgpt-api-member-panel--properties .localgpt-api-member-item,
.localgpt-print-content .localgpt-api-member-panel--events .localgpt-api-member-item,
.localgpt-print-content .localgpt-api-member-panel--operators .localgpt-api-member-item { break-inside: avoid-page; page-break-inside: avoid; }
/* Compact kawaii print surface: cute vectors and text glyphs, never a page-sized raster background. */
body::before, body::after { content: none !important; display: none !important; }
html, body { background: #fff0f8 !important; color: #51285d !important; }
.localgpt-print-cover {
  background: #ec4899 !important;
  border: 2px solid #ffffff;
  border-radius: 16pt;
  box-sizing: border-box;
  color: #ffffff !important;
  padding: 20mm;
  position: relative;
}
.localgpt-print-cover::after {
  color: rgba(255,255,255,.95);
  content: "✦ ･ﾟ✧ 🐾 🐱 🐶 ✧･ﾟ ✦";
  font-family: "Segoe UI Emoji", "Segoe UI Symbol", sans-serif;
  font-size: 15pt;
  letter-spacing: .16em;
  margin-top: 16pt;
}
.localgpt-print-cover::before,
.localgpt-print-cover h1,
.localgpt-print-cover p { color: #ffffff !important; text-shadow: none !important; }
.localgpt-print-toc {
  background: #fff8fc !important;
  border: 1px solid #f3c8df;
  border-radius: 10pt;
  box-sizing: border-box;
  padding: 10pt 12pt;
}
.localgpt-print-toc > h1,
.localgpt-print-toc-section > h2 {
  background: #d946ef !important;
  border: 0 !important;
  border-radius: 7pt;
  color: #ffffff !important;
  padding: 5pt 8pt;
}
.localgpt-print-toc-section > h2 { background: #8b5cf6 !important; }
.localgpt-print-toc-namespace { background: #fff7fb !important; border-color: #efc6df !important; }
.localgpt-print-toc-namespace h3,
.localgpt-print-toc-kind h4 {
  background: #ec4899 !important;
  border-radius: 5pt;
  color: #ffffff !important;
  padding: 3pt 5pt;
}
.localgpt-print-toc-kind h4 { background: var(--localgpt-api-kind-accent) !important; }
.localgpt-print-document {
  background: #fffafd !important;
  border: 1px solid #f1c9df;
  border-radius: 9pt;
  box-shadow: none !important;
  box-sizing: border-box;
  padding: 7pt 8pt 9pt;
  position: relative;
}
.localgpt-print-document::before {
  color: #d946ef;
  content: "✦ ･ﾟ✧ 🐾 🐱 🐶 ✧･ﾟ ✦";
  display: block;
  font-family: "Segoe UI Emoji", "Segoe UI Symbol", sans-serif;
  font-size: 7.5pt;
  letter-spacing: .09em;
  margin: 0 0 4pt;
  text-align: right;
}
.localgpt-print-source { border-bottom-color: #efb8d5 !important; color: #7a477f !important; }
.localgpt-print-api-breadcrumb span:first-child,
.localgpt-print-workspace-header span:first-child { color: #c026d3 !important; }
.localgpt-print-content h1 {
  background: #ec4899 !important;
  border: 0 !important;
  border-radius: 7pt;
  color: #ffffff !important;
  padding: 6pt 8pt !important;
  text-shadow: none !important;
}
.localgpt-print-api-namespace .localgpt-print-content h1 { background: #8b5cf6 !important; }
.localgpt-print-content h2 {
  background: #fff0f8 !important;
  border-left-color: #ec4899 !important;
  border-radius: 0 5pt 5pt 0;
  color: #6b2d72 !important;
}
.localgpt-print-content h3,
.localgpt-print-content h4,
.localgpt-print-content h5 { color: #6b2d72 !important; }
.localgpt-print-content pre { background: #fff7fb !important; border-color: #ecc7dc !important; border-left-color: #8b5cf6 !important; }
.localgpt-print-content th { background: #fce7f3 !important; color: #652d6e !important; }
.localgpt-print-content tbody tr:nth-child(even) { background: #fff8fc !important; }
.localgpt-print-content blockquote { background: #fdf2f8 !important; border-left-color: #ec4899 !important; }
.localgpt-print-content a { color: #a21caf !important; }
.localgpt-print-content .localgpt-api-member-panel {
  background: #fff8fc !important;
  border-color: #ecc7dc !important;
  border-left-color: var(--localgpt-api-accent) !important;
  box-shadow: none !important;
}
.localgpt-print-content .localgpt-api-member-panel > h2 {
  background: var(--localgpt-api-accent) !important;
  border-radius: 0 !important;
  color: #ffffff !important;
  text-shadow: none !important;
}
.localgpt-print-content .localgpt-api-member-panel > h2::after {
  color: #ffffff !important;
  content: "  🐱 🐶 ✨" !important;
  font-family: "Segoe UI Emoji", "Segoe UI Symbol", sans-serif;
}
.localgpt-print-content .localgpt-api-member-icon,
.localgpt-print-content .localgpt-api-member-count {
  background: #ffffff !important;
  border-color: rgba(255,255,255,.75) !important;
  color: #7e2f75 !important;
}
.localgpt-print-content .localgpt-api-member-panel-body,
.localgpt-print-content .localgpt-api-member-item { background: #fffdfd !important; }
.localgpt-print-content .localgpt-api-neko-note {
  background: #fce7f3 !important;
  border-color: #efb8d5 !important;
  color: #6b2d72 !important;
}
@media print {
  html, body { print-color-adjust: exact; -webkit-print-color-adjust: exact; }
  body::before, body::after { content: none !important; display: none !important; }
  .localgpt-print-document { box-shadow: none !important; }
}
</style>
'@
    [void]$builder.AppendLine($printStyles)
    [void]$builder.AppendLine('</head><body class="localgpt-print-book">')
    [void]$builder.AppendLine('<section class="localgpt-print-cover">')
    [void]$builder.AppendLine("<h1>LocalGPT $Version</h1>")
    [void]$builder.AppendLine('<p>A cozy, complete guide to LocalGPT product behavior, architecture, operations, and XML-generated API details.</p>')
    [void]$builder.AppendLine("<p>&#x1F43E; $($pageModels.Count) HTML reference pages · carefully arranged in 2026 · generated $([DateTime]::UtcNow.ToString('u'))</p>")
    [void]$builder.AppendLine('</section>')
    [void]$builder.AppendLine('<section class="localgpt-print-toc"><h1>Contents</h1>')
    [void]$builder.AppendLine('<div class="localgpt-print-toc-section"><h2>Product, architecture, and operations</h2><ol class="localgpt-print-toc-conceptual">')
    foreach ($page in @($pageModels | Where-Object { -not $_.IsApi })) {
        [void]$builder.AppendLine('<li><a href="#' + $page.Anchor + '">' + (ConvertTo-LocalGptHtml $page.Title) + '</a></li>')
    }
    [void]$builder.AppendLine('</ol></div>')
    [void]$builder.AppendLine('<div class="localgpt-print-toc-section localgpt-print-toc-api"><h2>&#x1F431; API reference</h2>')
    $apiPages = @($pageModels | Where-Object { $_.IsApi })
    $apiOverview = @($apiPages | Where-Object { $_.Relative -eq 'api/index.html' } | Select-Object -First 1)
    if ($apiOverview.Count -gt 0) {
        [void]$builder.AppendLine('<p class="localgpt-print-api-overview-link"><a href="#' + $apiOverview[0].Anchor + '">&#x1F338; ' + (ConvertTo-LocalGptHtml $apiOverview[0].Title) + '</a></p>')
    }

    $namespaceNames = [System.Collections.Generic.List[string]]::new()
    $namespaceSeen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($page in $apiPages) {
        if ($page.Relative -eq 'api/index.html') { continue }
        $namespaceName = if ($page.ApiKind -eq 'Namespace') {
            if ([string]::IsNullOrWhiteSpace($page.ApiNamespace)) { $page.ApiDisplayName } else { $page.ApiNamespace }
        }
        elseif ([string]::IsNullOrWhiteSpace($page.ApiNamespace)) { 'Other API' }
        else { $page.ApiNamespace }
        if ($namespaceSeen.Add($namespaceName)) { $namespaceNames.Add($namespaceName) }
    }

    foreach ($namespaceName in $namespaceNames) {
        $namespacePage = @($apiPages | Where-Object {
            $_.ApiKind -eq 'Namespace' -and
            ((-not [string]::IsNullOrWhiteSpace($_.ApiNamespace) -and $_.ApiNamespace -eq $namespaceName) -or
             ([string]::IsNullOrWhiteSpace($_.ApiNamespace) -and $_.ApiDisplayName -eq $namespaceName))
        } | Select-Object -First 1)
        $namespaceHeading = if ($namespacePage.Count -gt 0) {
            '<a href="#' + $namespacePage[0].Anchor + '">&#x1F431; ' + (ConvertTo-LocalGptHtml $namespaceName) + '</a>'
        }
        else { '&#x1F431; ' + (ConvertTo-LocalGptHtml $namespaceName) }
        [void]$builder.AppendLine('<section class="localgpt-print-toc-namespace"><h3>' + $namespaceHeading + '</h3>')

        $namespaceTypes = @($apiPages | Where-Object {
            $_.Relative -ne 'api/index.html' -and $_.ApiKind -ne 'Namespace' -and
            ((-not [string]::IsNullOrWhiteSpace($_.ApiNamespace) -and $_.ApiNamespace -eq $namespaceName) -or
             ([string]::IsNullOrWhiteSpace($_.ApiNamespace) -and $namespaceName -eq 'Other API'))
        })
        $kindNames = @($namespaceTypes | ForEach-Object { [string]$_.ApiKind } | Sort-Object -Unique)
        $kindModels = foreach ($kindName in $kindNames) {
            $presentation = Get-LocalGptApiKindPresentation -Kind $kindName
            [pscustomobject]@{ Kind = $kindName; Presentation = $presentation }
        }
        foreach ($kindModel in @($kindModels | Sort-Object { [int]$_.Presentation.Order })) {
            $kindPages = @($namespaceTypes | Where-Object { $_.ApiKind -eq $kindModel.Kind } | Sort-Object ApiDisplayName)
            if ($kindPages.Count -eq 0) { continue }
            $presentation = $kindModel.Presentation
            [void]$builder.AppendLine('<section class="localgpt-print-toc-kind localgpt-print-toc-kind--' + (ConvertTo-LocalGptHtml ([string]$presentation.Key)) + '" style="--localgpt-api-kind-accent:' + [string]$presentation.Accent + ';"><h4><span class="localgpt-print-toc-kind-icon" aria-hidden="true">' + [string]$presentation.IconHtml + '</span><span>' + (ConvertTo-LocalGptHtml ([string]$presentation.Label)) + '</span><span class="localgpt-print-toc-kind-count">' + [string]$kindPages.Count + '</span></h4><ul class="localgpt-print-toc-kind-list">')
            foreach ($page in $kindPages) {
                [void]$builder.Append('<li><a href="#' + $page.Anchor + '">' + (ConvertTo-LocalGptHtml $page.ApiDisplayName) + '</a>')
                if (@($page.MemberSections).Count -gt 0) {
                    [void]$builder.Append('<span class="localgpt-print-member-links">')
                    foreach ($section in @($page.MemberSections)) {
                        [void]$builder.Append('<a class="localgpt-print-member-link localgpt-print-member-link--' + (ConvertTo-LocalGptHtml ([string]$section.Key)) + '" href="#' + $page.Anchor + '-' + (ConvertTo-LocalGptHtml ([string]$section.Id)) + '"><span class="localgpt-print-member-icon" aria-hidden="true">' + [string]$section.IconHtml + '</span>' + (ConvertTo-LocalGptHtml ([string]$section.Name)) + ' <span class="localgpt-print-member-count">' + [string]$section.Count + '</span></a>')
                    }
                    [void]$builder.Append('</span>')
                }
                [void]$builder.AppendLine('</li>')
            }
            [void]$builder.AppendLine('</ul></section>')
        }
        [void]$builder.AppendLine('</section>')
    }
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
            [void]$builder.AppendLine('<div class="localgpt-print-source localgpt-print-api-breadcrumb"><span>&#x1F43E; API reference / ' + (ConvertTo-LocalGptHtml $namespaceLabel) + '</span><span>' + (ConvertTo-LocalGptHtml $page.Relative) + '</span></div>')
        }
        else {
            [void]$builder.AppendLine('<div class="localgpt-print-source localgpt-print-workspace-header"><span>&#x2728; LocalGPT ' + (ConvertTo-LocalGptHtml $Version) + ' / Documentation workspace</span><span>' + (ConvertTo-LocalGptHtml $page.Relative) + '</span></div>')
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
    $profileParentRoot = Join-Path ([IO.Path]::GetTempPath()) "LocalGPT/DocumentationBrowserProfiles"
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
                "--disable-background-mode",
                "--disable-background-networking",
                "--disable-background-timer-throttling",
                "--disable-component-update",
                "--disable-renderer-backgrounding",
                "--disable-sync",
                "--disable-breakpad",
                "--disable-crash-reporter",
                "--no-service-autorun",
                "--no-first-run",
                "--no-default-browser-check",
                "--allow-file-access-from-files",
                "--hide-scrollbars",
                "--run-all-compositor-stages-before-draw",
                "--virtual-time-budget=180000",
                "--js-flags=--max-old-space-size=4096",
                "--disable-features=BackForwardCache,CalculateNativeWinOcclusion,MediaRouter,OptimizationHints,Translate,msEdgeStartupBoost,msEdgeBackgroundMode",
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
            $lastObservedLength = -1L
            $stableLengthChecks = 0
            for ($attempt = 0; $attempt -lt 360; $attempt++) {
                $pdfFile = Get-Item -LiteralPath $PdfPath -ErrorAction SilentlyContinue
                if ($null -ne $pdfFile -and $pdfFile.Length -gt 0) {
                    if ($pdfFile.Length -eq $lastObservedLength) {
                        $stableLengthChecks++
                    }
                    else {
                        $lastObservedLength = [long]$pdfFile.Length
                        $stableLengthChecks = 0
                    }
                    if ($stableLengthChecks -ge 4) { break }
                }
                Start-Sleep -Milliseconds 500
            }
            if (Test-Path -LiteralPath $PdfPath -PathType Leaf) {
                $pdfFile = Get-Item -LiteralPath $PdfPath -ErrorAction SilentlyContinue
                if ($null -ne $pdfFile -and $pdfFile.Length -gt 0) {
                    if ($exitCode -ne 0) {
                        $diagnostics.Add("The browser returned exit code $exitCode after writing a PDF candidate; the candidate will be validated by LocalGPT.")
                    }
                    return [pscustomobject]@{ Succeeded = $true; ExitCode = $exitCode; Diagnostics = @($diagnostics); HeadlessMode = $headlessMode }
                }
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
                $candidates.Add((Join-Path $_.FullName "bin/gswin64c.exe"))
                $candidates.Add((Join-Path $_.FullName "bin/gswin32c.exe"))
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

function Resolve-LocalGptDocumentationNode {
    return Resolve-LocalGptNodeRuntime `
        -CacheRoot $documentationToolCacheRoot `
        -Version $provisionedNodeVersion `
        -MinimumMajor $minimumNodeMajor `
        -MaximumPreferredMajor $maximumPreferredNodeMajor `
        -AllowProvisioning `
        -PreferCompatibleLts
}


function Enter-LocalGptDocumentationLock {
    param(
        [Parameter(Mandatory)][string]$Path,
        [int]$TimeoutSeconds = 1800
    )

    $directory = Split-Path -Parent $Path
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $deadline = [DateTime]::UtcNow.AddSeconds([Math]::Max(1, $TimeoutSeconds))
    $announced = $false
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $stream = [IO.File]::Open($Path, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            $stream.SetLength(0)
            $payload = [Text.Encoding]::UTF8.GetBytes("pid=$PID;utc=$([DateTime]::UtcNow.ToString('O'));repository=$RepositoryRoot")
            $stream.Write($payload, 0, $payload.Length)
            $stream.Flush()
            return $stream
        }
        catch [IO.IOException] {
            if (-not $announced) {
                Write-Host "Another LocalGPT documentation build is finishing. Waiting for its workspace lock..." -ForegroundColor Yellow
                $announced = $true
            }
            Start-Sleep -Milliseconds 500
        }
    }

    throw "Timed out waiting for the LocalGPT documentation workspace lock: $Path"
}

function Wait-LocalGptFilesReadable {
    param(
        [Parameter(Mandatory)][string]$Root,
        [int]$Attempts = 20,
        [int]$DelayMilliseconds = 250
    )

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) { return $true }
    for ($attempt = 1; $attempt -le [Math]::Max(1, $Attempts); $attempt++) {
        $blocked = $false
        foreach ($file in @(Get-ChildItem -LiteralPath $Root -File -Recurse -ErrorAction SilentlyContinue)) {
            $stream = $null
            try {
                $stream = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
            }
            catch [IO.IOException] {
                $blocked = $true
                break
            }
            finally {
                if ($null -ne $stream) { $stream.Dispose() }
            }
        }
        if (-not $blocked) { return $true }
        Start-Sleep -Milliseconds ([Math]::Max(1, $DelayMilliseconds))
    }
    return $false
}

function Test-LocalGptTransientDocfxFailure {
    param([AllowNull()][object[]]$Output)
    $text = @($Output | ForEach-Object { [string]$_ }) -join "`n"
    return $text -match '(?i)(being used by another process|process cannot access the file|sharing violation|System\.IO\.IOException|SafeFileHandle\.CreateFile)'
}

function Get-LocalGptUnresolvedAssemblyReferences {
    param([AllowNull()][object[]]$Output)

    $names = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in @($Output)) {
        $line = [string]$entry
        $match = [regex]::Match($line, '(?i)Unable\s+to\s+resolve\s+assembly\s+reference\s+([^,\s]+)')
        if ($match.Success) {
            [void]$names.Add($match.Groups[1].Value.Trim())
        }
    }
    return @($names | Sort-Object)
}

function Get-LocalGptSharedRuntimeProbeDirectories {
    $directories = [System.Collections.Generic.List[string]]::new()
    $dotnetCommand = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $dotnetCommand) { return @() }

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $runtimeLines = @(& dotnet --list-runtimes 2>$null)
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    foreach ($entry in $runtimeLines) {
        $line = [string]$entry
        $match = [regex]::Match($line, '^\s*(Microsoft\.(?:NETCore|AspNetCore)\.App)\s+([^\s]+)\s+\[(.+)\]\s*$')
        if (-not $match.Success) { continue }
        $version = $match.Groups[2].Value.Trim()
        $basePath = $match.Groups[3].Value.Trim()
        $candidate = Join-Path $basePath $version
        if ((Test-Path -LiteralPath $candidate -PathType Container) -and -not $directories.Contains($candidate)) {
            $directories.Add($candidate)
        }
    }

    # Prefer the .NET 10 runtime family used by LocalGPT, then keep other installed runtimes as a
    # last-resort probe source for tooling-only references without changing the application target.
    return @($directories | Sort-Object @{ Expression = { if ($_ -match '[\\/]10\.0\.[^\\/]+$') { 0 } else { 1 } } }, @{ Expression = { $_ } })
}

function Resolve-LocalGptNuGetAssemblyReference {
    param([Parameter(Mandatory)][string]$ReferenceName)

    $packageRoots = [System.Collections.Generic.List[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)) {
        $explicitRoot = [IO.Path]::GetFullPath($env:NUGET_PACKAGES)
        if ((Test-Path -LiteralPath $explicitRoot -PathType Container) -and -not $packageRoots.Contains($explicitRoot)) {
            $packageRoots.Add($explicitRoot)
        }
    }

    $userProfile = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
    if (-not [string]::IsNullOrWhiteSpace($userProfile)) {
        $defaultRoot = Join-Path $userProfile '.nuget/packages'
        if ((Test-Path -LiteralPath $defaultRoot -PathType Container) -and -not $packageRoots.Contains($defaultRoot)) {
            $packageRoots.Add($defaultRoot)
        }
    }

    foreach ($packageRoot in $packageRoots) {
        $packageDirectory = Join-Path $packageRoot $ReferenceName.ToLowerInvariant()
        if (-not (Test-Path -LiteralPath $packageDirectory -PathType Container)) { continue }
        $candidates = @(
            Get-ChildItem -LiteralPath $packageDirectory -Filter ($ReferenceName + '.dll') -File -Recurse -ErrorAction SilentlyContinue |
                Sort-Object `
                    @{ Expression = { if ($_.FullName -match '[\\/]lib[\\/]net10\.0[\\/]') { 0 } elseif ($_.FullName -match '[\\/]lib[\\/]netstandard2\.[01][\\/]') { 1 } else { 2 } } }, `
                    @{ Expression = { $_.FullName }; Descending = $true }
        )
        if ($candidates.Count -gt 0) { return $candidates[0].FullName }
    }

    return $null
}

function Initialize-LocalGptDocfxPinnedDependencies {
    param([Parameter(Mandatory)][string]$DependencyProjectPath)

    if (-not (Test-Path -LiteralPath $DependencyProjectPath -PathType Leaf)) {
        throw "The DocFX dependency project is missing: $DependencyProjectPath"
    }

    # Avoid an extra restore when the exact probe assembly is already available in the user package cache.
    if ($null -ne (Resolve-LocalGptNuGetAssemblyReference -ReferenceName 'System.Formats.Nrbf')) {
        return
    }

    Write-Host "Restoring pinned DocFX-only dependency probes (application dependency graph remains unchanged)..." -ForegroundColor DarkCyan
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & dotnet restore $DependencyProjectPath --disable-parallel --force-evaluate
        $restoreExitCode = [int]$LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($restoreExitCode -ne 0) {
        throw "DocFX dependency probe restore failed with exit code ${restoreExitCode}: $DependencyProjectPath"
    }

    if ($null -eq (Resolve-LocalGptNuGetAssemblyReference -ReferenceName 'System.Formats.Nrbf')) {
        throw "The DocFX dependency probe restore completed, but System.Formats.Nrbf.dll could not be located in the NuGet package cache."
    }
}

function Repair-LocalGptDocfxAssemblyReferences {
    param(
        [Parameter(Mandatory)][string[]]$ReferenceNames,
        [Parameter(Mandatory)][string]$InputRoot,
        [Parameter(Mandatory)][string]$AssemblyDirectory
    )

    $probeDirectories = [System.Collections.Generic.List[string]]::new()
    if (Test-Path -LiteralPath $AssemblyDirectory -PathType Container) { $probeDirectories.Add($AssemblyDirectory) }
    foreach ($runtimeDirectory in @(Get-LocalGptSharedRuntimeProbeDirectories)) {
        if (-not $probeDirectories.Contains($runtimeDirectory)) { $probeDirectories.Add($runtimeDirectory) }
    }

    $copied = [System.Collections.Generic.List[string]]::new()
    foreach ($referenceName in @($ReferenceNames | Sort-Object -Unique)) {
        if ([string]::IsNullOrWhiteSpace($referenceName)) { continue }
        $destination = Join-Path $InputRoot ($referenceName + '.dll')
        if (Test-Path -LiteralPath $destination -PathType Leaf) { continue }

        $source = $null
        foreach ($probeDirectory in $probeDirectories) {
            $candidate = Join-Path $probeDirectory ($referenceName + '.dll')
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                $source = $candidate
                break
            }
        }
        if ($null -eq $source) {
            $source = Resolve-LocalGptNuGetAssemblyReference -ReferenceName $referenceName
        }
        if ($null -eq $source) { continue }

        Copy-Item -LiteralPath $source -Destination $destination -Force
        $copied.Add($referenceName)
        Write-Host "DocFX dependency probe: copied '$referenceName.dll' from '$source'." -ForegroundColor DarkCyan
    }

    return @($copied)
}

function Invoke-LocalGptDocfxWithRetry {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [string]$ReadableRoot = "",
        [string]$ResetRootOnRetry = "",
        [int]$Attempts = 4
    )

    $lastResult = $null
    for ($attempt = 1; $attempt -le [Math]::Max(1, $Attempts); $attempt++) {
        if (-not [string]::IsNullOrWhiteSpace($ReadableRoot)) {
            [void](Wait-LocalGptFilesReadable -Root $ReadableRoot -Attempts 20 -DelayMilliseconds 250)
        }
        $lastResult = Invoke-LocalGptDocfx -Arguments $Arguments
        $transientFailure = Test-LocalGptTransientDocfxFailure -Output $lastResult.Output
        if ($lastResult.ExitCode -eq 0 -and -not $transientFailure) { return $lastResult }
        if (-not $transientFailure -or $attempt -ge $Attempts) { return $lastResult }

        $delay = 500 * $attempt
        Write-Warning "DocFX hit a transient Windows file-sharing lock. Retrying attempt $($attempt + 1) of $Attempts after ${delay}ms."
        Start-Sleep -Milliseconds $delay
        if (-not [string]::IsNullOrWhiteSpace($ResetRootOnRetry)) {
            Remove-LocalGptTemporaryPath -Path $ResetRootOnRetry -Attempts 8 -DelayMilliseconds 250
        }
    }
    return $lastResult
}

$documentationLockStream = Enter-LocalGptDocumentationLock -Path $documentationLockPath
New-Item -ItemType Directory -Path $documentationWorkRoot -Force | Out-Null

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
$documentationXmlPath = Join-Path $inputRoot "LocalGPT.xml"
$xmlCommentPolishCount = Write-LocalGptPolishedXmlDocumentation -SourcePath $XmlDocumentationPath -DestinationPath $polishedXmlPath
Copy-Item -LiteralPath $polishedXmlPath -Destination $documentationXmlPath -Force
Write-Host "Polished $xmlCommentPolishCount simple XML documentation passage(s) for clearer generated prose."

$indexPath = Join-Path $docsRoot "index.md"
if (Test-Path -LiteralPath $indexPath) {
    $index = Get-Content -LiteralPath $indexPath -Raw -Encoding UTF8
    $index = [regex]::Replace($index, '\*\*Version [^*]+\*\*', "**Version $Version**")
    $index = [regex]::Replace($index, 'LocalGPT-[0-9]+\.[0-9]+\.[0-9]+\.pdf', $pdfName)
    Set-Utf8TextFileIdempotent -Path $indexPath -Content $index
}

if (Test-Path -LiteralPath $pdfTocPath) {
    $pdfToc = Get-Content -LiteralPath $pdfTocPath -Raw -Encoding UTF8
    $pdfToc = [regex]::Replace($pdfToc, '(?m)^pdfFileName:\s*LocalGPT-[^\r\n]+\.pdf\s*$', "pdfFileName: $pdfName")
    Set-Utf8TextFileIdempotent -Path $pdfTocPath -Content $pdfToc
}

if (Test-Path -LiteralPath $pdfCoverPath) {
    $cover = Get-Content -LiteralPath $pdfCoverPath -Raw -Encoding UTF8
    $cover = [regex]::Replace($cover, 'LocalGPT [0-9]+\.[0-9]+\.[0-9]+ Documentation', "LocalGPT $Version Documentation")
    $cover = [regex]::Replace($cover, 'Version [0-9]+\.[0-9]+\.[0-9]+', "Version $Version")
    Set-Utf8TextFileIdempotent -Path $pdfCoverPath -Content $cover
}

if (Test-Path -LiteralPath $configPath) {
    $configText = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8
    $configText = [regex]::Replace($configText, '"localgptVersion"\s*:\s*"[^"]+"', '"localgptVersion": "' + $Version + '"')
    $configText = [regex]::Replace($configText, '"_appFooter"\s*:\s*"LocalGPT [^"]+"', '"_appFooter": "LocalGPT ' + $Version + ' · generated documentation"')
    Set-Utf8TextFileIdempotent -Path $configPath -Content $configText

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
    $tocText = Get-Content -LiteralPath $tocPath -Raw -Encoding UTF8
    if ($tocText -notmatch '(?m)^\s*href:\s*guide/\s*$' -or $tocText -notmatch '(?m)^\s*href:\s*api/\s*$') {
        throw "The root DocFX TOC must remain navbar-only and reference the guide and API TOCs."
    }
    if (-not (Test-Path -LiteralPath $guideTocPath -PathType Leaf)) {
        throw "The Microsoft Learn-style guide TOC is missing: $guideTocPath"
    }
    $pdfTocText = Get-Content -LiteralPath $pdfTocPath -Raw -Encoding UTF8
    if ($pdfTocText -notmatch '(?m)^\s*href:\s*\.\./guide/toc\.yml\s*$' -or $pdfTocText -notmatch '(?m)^\s*href:\s*\.\./api/toc\.yml\s*$') {
        throw "The dedicated PDF TOC must nest both guide/toc.yml and api/toc.yml."
    }
}

# A prior interrupted build may have left the tiny DocFX link-validation placeholder behind.
# Remove only our own marker-bearing stub; never delete a real authored PDF from the docs tree.
if (Test-LocalGptDocfxPdfLinkStub -Path $pdfLinkStubPath) {
    Remove-Item -LiteralPath $pdfLinkStubPath -Force -ErrorAction SilentlyContinue
}
if (-not (Test-Path -LiteralPath $pdfLinkStubPath -PathType Leaf)) {
    New-LocalGptDocfxPdfLinkStub -Path $pdfLinkStubPath
    $pdfLinkStubCreated = $true
}

$docfxExecutable = $null
$useManifestTool = $false
function Invoke-LocalGptDocfx {
    param([Parameter(Mandatory)][string[]]$Arguments)

    # Stream DocFX output as it arrives. The PDF command can legitimately spend many minutes
    # rendering a four-digit page set, and buffering native output until process exit makes a
    # healthy build look frozen. Keep a copy for retry/error diagnostics while writing each line
    # immediately to the host. Windows PowerShell can promote native stderr records when the
    # script-wide ErrorActionPreference is Stop, so temporarily continue and trust the native
    # exit code plus generated artifacts.
    $previousErrorActionPreference = $ErrorActionPreference
    $capturedOutput = [System.Collections.Generic.List[string]]::new()
    try {
        $ErrorActionPreference = "Continue"
        if ($script:useManifestTool) {
            & dotnet tool run docfx @Arguments 2>&1 | ForEach-Object {
                $rawLine = [string]$_
                $capturedOutput.Add($rawLine)
                # ConsoleToMSBuild scans text independently from the native exit code. Keep
                # handled DocFX/Node diagnostics visible without turning them into MSB3077.
                $displayLine = [regex]::Replace($rawLine, '(?i)\b(?:fatalerror|error)\s*:', 'diagnostic:')
                Write-Host "[DocFX] $displayLine"
            }
        }
        else {
            & $script:docfxExecutable @Arguments 2>&1 | ForEach-Object {
                $rawLine = [string]$_
                $capturedOutput.Add($rawLine)
                $displayLine = [regex]::Replace($rawLine, '(?i)\b(?:fatalerror|error)\s*:', 'diagnostic:')
                Write-Host "[DocFX] $displayLine"
            }
        }
        $exitCode = [int]$LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = @($capturedOutput.ToArray())
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
            if (-not (Test-Path -LiteralPath $documentationXmlPath -PathType Leaf)) {
                Copy-Item -LiteralPath $polishedXmlPath -Destination $documentationXmlPath -Force
            }

            # DocFX 2.78.x may reflect System.Resources.Extensions metadata that references
            # System.Formats.Nrbf. The application itself does not need a direct reference, so keep
            # this dependency in the documentation-only project and materialize it solely into the
            # temporary DocFX probe directory.
            Initialize-LocalGptDocfxPinnedDependencies -DependencyProjectPath $docfxDependencyProjectPath
            $pinnedProbeReferences = @(Repair-LocalGptDocfxAssemblyReferences `
                -ReferenceNames @('System.Formats.Nrbf') `
                -InputRoot $inputRoot `
                -AssemblyDirectory $assemblyDirectory)
            $docfxDependencyRepairCount += $pinnedProbeReferences.Count

            $metadataResult = Invoke-LocalGptDocfxWithRetry -Arguments @("metadata", $configPath) -ReadableRoot $inputRoot -ResetRootOnRetry $apiRoot
            for ($dependencyRepairPass = 1; $dependencyRepairPass -le 3; $dependencyRepairPass++) {
                $unresolvedAssemblyReferences = @(Get-LocalGptUnresolvedAssemblyReferences -Output $metadataResult.Output)
                if ($unresolvedAssemblyReferences.Count -eq 0) { break }

                $repairedReferences = @(Repair-LocalGptDocfxAssemblyReferences `
                    -ReferenceNames $unresolvedAssemblyReferences `
                    -InputRoot $inputRoot `
                    -AssemblyDirectory $assemblyDirectory)
                $docfxDependencyRepairCount += $repairedReferences.Count
                if ($repairedReferences.Count -eq 0) { break }

                Remove-LocalGptTemporaryPath -Path $apiRoot -Attempts 8 -DelayMilliseconds 250
                Write-Host "Retrying DocFX metadata after repairing $($repairedReferences.Count) assembly reference(s)." -ForegroundColor Cyan
                $metadataResult = Invoke-LocalGptDocfxWithRetry -Arguments @("metadata", $configPath) -ReadableRoot $inputRoot -ResetRootOnRetry $apiRoot
            }
            $unresolvedAssemblyReferences = @(Get-LocalGptUnresolvedAssemblyReferences -Output $metadataResult.Output)
            $apiTocPath = Join-Path $apiRoot "toc.yml"
            $apiYamlCount = @(Get-ChildItem -LiteralPath $apiRoot -Filter "*.yml" -File -Recurse -ErrorAction SilentlyContinue).Count
            $metadataSucceeded = $metadataResult.ExitCode -eq 0 -and $unresolvedAssemblyReferences.Count -eq 0 -and (Test-Path -LiteralPath $apiTocPath -PathType Leaf) -and $apiYamlCount -gt 1
            if (-not $metadataSucceeded) {
                $metadataTail = @($metadataResult.Output | Select-Object -Last 20) -join " | "
                if ([string]::IsNullOrWhiteSpace($metadataTail)) { $metadataTail = "DocFX returned no metadata diagnostic." }
                if ($unresolvedAssemblyReferences.Count -gt 0) {
                    $warnings.Add("DocFX metadata extraction retained unresolved assembly references after dependency repair: $($unresolvedAssemblyReferences -join ', '). $metadataTail")
                }
                else {
                    $warnings.Add("DocFX metadata extraction did not produce the complete API graph (exit code $($metadataResult.ExitCode)): $metadataTail")
                }
            }
            else {
                $apiIndex = @"
# LocalGPT API reference

This reference is generated from LocalGPT.dll and its side-by-side LocalGPT.xml compiler documentation for version $Version.

The namespace, type, and member pages below are generated by DocFX and are included in the complete versioned PDF.

Use the grouped API navigation to browse namespaces, types, properties, methods, events, and the other generated member sections.
"@
                Set-Content -LiteralPath (Join-Path $apiRoot "index.md") -Value $apiIndex -Encoding utf8

                $apiTocText = Get-Content -LiteralPath $apiTocPath -Raw -Encoding UTF8
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

                $buildResult = Invoke-LocalGptDocfxWithRetry -Arguments @("build", $configPath) -ReadableRoot $apiRoot -ResetRootOnRetry $siteRoot
                $apiHtmlRoot = Join-Path $siteRoot "api"
                $apiHtmlCount = @(Get-ChildItem -LiteralPath $apiHtmlRoot -Filter "*.html" -File -Recurse -ErrorAction SilentlyContinue).Count
                $docfxBuildSucceeded = $buildResult.ExitCode -eq 0 -and (Test-Path -LiteralPath (Join-Path $siteRoot "index.html") -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $apiHtmlRoot "index.html") -PathType Leaf) -and $apiHtmlCount -gt 1
                if ($pdfLinkStubCreated) {
                    Remove-Item -LiteralPath (Join-Path $siteRoot $pdfName) -Force -ErrorAction SilentlyContinue
                }
                if ($docfxBuildSucceeded) {
                    $apiMemberPanelCount = Update-LocalGptApiPresentation -SiteRoot $siteRoot
                    if ($apiMemberPanelCount -eq 0) {
                        $warnings.Add("The DocFX site rendered successfully, but no API member panels were generated.")
                    }
                    $apiNavigationGroupCount = Update-LocalGptApiNavigation -SiteRoot $siteRoot
                    if ($apiNavigationGroupCount -eq 0) {
                        $warnings.Add("The DocFX site rendered successfully, but no API member-section navigation groups were discovered.")
                    }
                    $websiteThemeAssetCount = Install-LocalGptWebsiteThemeAssets -SiteRoot $siteRoot
                    if ($websiteThemeAssetCount -eq 0) {
                        $warnings.Add("The DocFX site rendered successfully, but the cache-busted LocalGPT website theme was not injected into any HTML page.")
                    }
                }
                else {
                    $buildTail = @($buildResult.Output | Select-Object -Last 30) -join " | "
                    if ([string]::IsNullOrWhiteSpace($buildTail)) { $buildTail = "DocFX returned no site-build diagnostic." }
                    $warnings.Add("DocFX site generation did not render the complete API reference (exit code $($buildResult.ExitCode)): $buildTail")
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

    if ($unresolvedAssemblyReferences.Count -gt 0) {
        throw "DocFX metadata extraction failed before PDF generation because unresolved assembly references remain: $($unresolvedAssemblyReferences -join ', ')."
    }

    if (-not $docfxBuildSucceeded) {
        New-LocalGptStaticDocumentation -XmlPath $polishedXmlPath -Destination $siteRoot
        $documentationMode = "static-fallback"
        $apiHtmlCount = @(Get-ChildItem -LiteralPath (Join-Path $siteRoot "api") -Filter "*.html" -File -Recurse -ErrorAction SilentlyContinue).Count
    }
    else {
        $documentationMode = "docfx"
        Copy-Item -LiteralPath $polishedXmlPath -Destination (Join-Path $siteRoot "LocalGPT.xml") -Force
        $preflightPdfStubPath = Join-Path $siteRoot $pdfName
        if ($pdfLinkStubCreated) {
            Copy-Item -LiteralPath $pdfLinkStubPath -Destination $preflightPdfStubPath -Force
        }
        try {
            Assert-LocalGptGeneratedHtmlPreflight -SiteRoot $siteRoot
            $htmlPreflightValidated = $true
        }
        finally {
            if ($pdfLinkStubCreated) {
                Remove-Item -LiteralPath $preflightPdfStubPath -Force -ErrorAction SilentlyContinue
            }
        }
    }

    # Publish the current HTML tree before the long PDF render. This prevents a failed or
    # interrupted PDF step from leaving the running LocalGPT application with stale, blank,
    # or unthemed help content. The final publication pass below adds status and PDF metadata.
    foreach ($publishRoot in $publishRoots) {
        Remove-Item -LiteralPath $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
        Copy-Item -Path (Join-Path $siteRoot "*") -Destination $publishRoot -Recurse -Force
    }

    [xml]$xmlForCount = Get-Content -LiteralPath $polishedXmlPath -Raw -Encoding UTF8
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
                    Write-Host "Printing $pdfSourcePageCount DocFX HTML pages as one complete LocalGPT PDF with $($browser.Name)." -ForegroundColor Cyan
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
            Write-Host "The DocFX site contains $pdfSourcePageCount printable HTML pages, above the host browser-print limit of $maximumBrowserPrintSourcePages; using the DocFX PDF plug-in directly." -ForegroundColor DarkCyan
        }

        if (-not $pdfGenerated) {
            Get-ChildItem -LiteralPath $siteRoot -Filter "*.pdf" -File -Recurse -ErrorAction SilentlyContinue |
                Remove-Item -Force -ErrorAction SilentlyContinue
            try {
                $nodeInfo = Resolve-LocalGptDocumentationNode
                if ($null -ne $nodeInfo) {
                    $nodeVersionUsed = [string]$nodeInfo.Version
                    $nodeProvisioned = [bool]$nodeInfo.Provisioned
                    $nodePlatformUsed = [string]$nodeInfo.Platform
                    $nodeArchitectureUsed = [string]$nodeInfo.Architecture
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

                    Write-Host "Browser printing was unavailable; generating the complete PDF with the DocFX PDF plug-in and Node.js $nodeVersionUsed. This can take several minutes for $pdfSourcePageCount pages; DocFX output is streamed live below." -ForegroundColor Cyan
                    $pdfResult = Invoke-LocalGptDocfx -Arguments @("pdf", $configPath, "--logLevel", "verbose")
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
    if ($pdfLinkStubCreated) {
        Remove-Item -LiteralPath $pdfLinkStubPath -Force -ErrorAction SilentlyContinue
    }
    Remove-LocalGptTemporaryPath -Path $inputRoot
    Remove-LocalGptTemporaryPath -Path $documentationWorkRoot -Attempts 8 -DelayMilliseconds 250
    Remove-LocalGptTemporaryPath -Path $printBookRoot -Attempts 8 -DelayMilliseconds 250
}

# GitHub Pages must serve the generated DocFX files verbatim rather than passing them through Jekyll.
$noJekyllPath = Join-Path $siteRoot ".nojekyll"
if (-not (Test-Path -LiteralPath $noJekyllPath -PathType Leaf)) {
    [IO.File]::WriteAllText($noJekyllPath, [string]::Empty, [Text.UTF8Encoding]::new($false))
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
        xmlCommentPolishCount = $xmlCommentPolishCount
        apiYamlCount = $apiYamlCount
        apiHtmlCount = $apiHtmlCount
        apiNavigationGroupCount = $apiNavigationGroupCount
        websiteThemeAssetCount = $websiteThemeAssetCount
        htmlPreflightValidated = $htmlPreflightValidated
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
        nodePlatform = $nodePlatformUsed
        nodeArchitecture = $nodeArchitectureUsed
        pdfTimeoutMilliseconds = $pdfTimeoutMilliseconds
        completeApiReference = $documentationMode -eq "docfx" -and $apiYamlCount -gt 1 -and $apiHtmlCount -gt 1 -and $unresolvedAssemblyReferences.Count -eq 0
        unresolvedAssemblyReferenceCount = $unresolvedAssemblyReferences.Count
        unresolvedAssemblyReferences = @($unresolvedAssemblyReferences)
        docfxDependencyRepairCount = $docfxDependencyRepairCount
        warnings = @($warnings)
    }
    $statusPath = Join-Path $publishRoot "documentation-status.json"
    $status | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statusPath -Encoding utf8

    $requiredArtifacts = [System.Collections.Generic.List[string]]::new()
    $requiredArtifacts.Add((Join-Path $publishRoot "index.html"))
    $requiredArtifacts.Add((Join-Path $publishRoot "LocalGPT.xml"))
    $requiredArtifacts.Add($statusPath)
    if ($documentationMode -eq "docfx") {
        $requiredArtifacts.Add((Join-Path $publishRoot "styles/localgpt-kawaii.css"))
        $requiredArtifacts.Add((Join-Path $publishRoot "styles/localgpt-kawaii.js"))
        $requiredArtifacts.Add((Join-Path $publishRoot "favicon.ico"))
        $requiredArtifacts.Add((Join-Path $publishRoot "favicon.svg"))
        $requiredArtifacts.Add((Join-Path $publishRoot "logo.svg"))
    }
    if ($RequirePdf -or $pdfGenerated) {
        $requiredArtifacts.Add((Join-Path $publishRoot $pdfName))
    }
    foreach ($requiredArtifact in $requiredArtifacts) {
        if (-not (Test-Path -LiteralPath $requiredArtifact -PathType Leaf)) {
            throw "Documentation generation did not produce the required artifact: $requiredArtifact"
        }
    }
    if ($documentationMode -eq "docfx") {
        $publishedIndex = Get-Content -LiteralPath (Join-Path $publishRoot "index.html") -Raw -Encoding UTF8
        foreach ($themeMarker in @("localgpt-kawaii-docs", "data-localgpt-theme-bootstrap", "data-localgpt-favicon", "data-localgpt-kawaii-style", "data-localgpt-kawaii-script")) {
            if ($publishedIndex -notmatch [regex]::Escape($themeMarker)) {
                throw "The generated DocFX home page is missing the required Kawaii theme marker: $themeMarker"
            }
        }
    }
}

Write-Host "LocalGPT documentation generated for version $Version using $documentationMode; PDF mode: $pdfMode." -ForegroundColor Green
if ($null -ne $documentationLockStream) {
    $documentationLockStream.Dispose()
    $documentationLockStream = $null
}
