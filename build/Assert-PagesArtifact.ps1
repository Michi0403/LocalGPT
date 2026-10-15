param(
    [string]$Source = '',
    [string]$Archive = '',
    [string]$Output = '',
    [string]$ExpectedVersion = '',
    [switch]$HtmlOnly
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$maxFiles = 20000
$maxBytes = 536870912L
$minimumPdf = 524288L
$temporaryExtract = ''

function Assert-ContainedPath([string]$Root, [string]$Path) {
    $base = [IO.Path]::GetFullPath($Root).TrimEnd([char[]]@('/', '\')) + [IO.Path]::DirectorySeparatorChar
    $candidate = [IO.Path]::GetFullPath($Path)
    if (-not $candidate.StartsWith($base, [StringComparison]::OrdinalIgnoreCase)) { throw "Documentation path escapes artifact: $Path" }
    return $candidate
}
function Read-Json([string]$Path) {
    return (Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json)
}
function Get-StatusValue($Status, [string[]]$Names, $Default = $null) {
    foreach ($name in $Names) {
        $property = $Status.PSObject.Properties[$name]
        if ($null -ne $property -and $null -ne $property.Value) { return $property.Value }
    }
    return $Default
}
function Assert-HtmlSite([string]$Root) {
    $files = @(Get-ChildItem -LiteralPath $Root -Recurse -File -Filter '*.html')
    $apiCount = @($files | Where-Object { $_.FullName.Substring($Root.Length).Replace('\','/') -match '(^|/)api/' }).Count
    foreach ($file in $files) {
        $text = [IO.File]::ReadAllText($file.FullName, [Text.Encoding]::UTF8)
        if ($file.Name -ne 'toc.html') {
            if ($text -notmatch '(?is)<html\b[^>]*\blang\s*=\s*["\x27][^"\x27]+') { throw "HTML lang missing: $($file.FullName)" }
            if ($text -notmatch '(?is)<meta\b[^>]*\bname\s*=\s*["\x27]viewport["\x27]') { throw "HTML viewport missing: $($file.FullName)" }
            if ($text -notmatch '(?is)<title\b') { throw "HTML title missing: $($file.FullName)" }
            if ($text -notmatch '(?is)<(?:main|article)\b') { throw "HTML landmark missing: $($file.FullName)" }
            if ($text -match '(?is)<img\b(?![^>]*\balt\s*=)[^>]*>') { throw "HTML image without alt: $($file.FullName)" }
        }
        foreach ($match in [regex]::Matches($text, '(?is)\b(?:href|src)\s*=\s*["\x27](?<value>[^"\x27]+)["\x27]')) {
            $raw = $match.Groups['value'].Value
            if ($raw -match '^(?:#|mailto:|tel:|javascript:|data:|[a-z][a-z0-9+.-]*:|//)') { continue }
            $urlPath = ($raw -split '[?#]', 2)[0]
            if ([string]::IsNullOrEmpty($urlPath)) { continue }
            $urlPath = [Uri]::UnescapeDataString($urlPath).Replace('/', [IO.Path]::DirectorySeparatorChar)
            $target = if ($urlPath.StartsWith([string][IO.Path]::DirectorySeparatorChar)) {
                Join-Path $Root ($urlPath.TrimStart([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)))
            } else { Join-Path $file.DirectoryName $urlPath }
            $target = Assert-ContainedPath $Root $target
            if (Test-Path -LiteralPath $target -PathType Container) { $target = Join-Path $target 'index.html' }
            if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { throw "Broken local documentation link: $($file.FullName) -> $raw" }
        }
    }
    Write-Host "PowerShell HTML accessibility and link preflight passed: $($files.Count) HTML, $apiCount API HTML."
    return [pscustomobject]@{ Count = $files.Count; ApiCount = $apiCount }
}
function Assert-Documentation([string]$Root, [string]$Version, [bool]$OnlyHtml) {
    $html = Assert-HtmlSite $Root
    if ($OnlyHtml) { return }
    foreach ($relative in @('index.html','api/index.html','documentation-status.json','styles/localgpt-kawaii.css','styles/localgpt-kawaii.js','favicon.svg','favicon.ico','logo.svg')) {
        if (-not (Test-Path -LiteralPath (Join-Path $Root $relative) -PathType Leaf)) { throw "Documentation artifact is incomplete: $relative" }
    }
    foreach ($item in @(
        @{ File='index.html'; Tokens=@('localgpt-kawaii-docs','data-localgpt-theme-bootstrap','data-localgpt-favicon','data-localgpt-kawaii-style','data-localgpt-kawaii-script') },
        @{ File='styles/localgpt-kawaii.css'; Tokens=@('localgpt-theme-control','localgpt-kawaii-sky','localgpt-cursor-paw','--kawaii-docs-rail-width') },
        @{ File='styles/localgpt-kawaii.js'; Tokens=@('mountThemeControl','localgpt-docs-theme','persistTheme','localgpt-cursor-paw') },
        @{ File='favicon.svg'; Tokens=@('LocalGPT cat paw','<svg') }
    )) {
        $body = [IO.File]::ReadAllText((Join-Path $Root $item.File))
        foreach ($token in $item.Tokens) { if (-not $body.Contains($token)) { throw "Documentation theme contract missing '$token' in $($item.File)" } }
    }
    $status = Read-Json (Join-Path $Root 'documentation-status.json')
    $actualVersion = [string](Get-StatusValue $status @('version','Version') '')
    if ([string]::IsNullOrWhiteSpace($actualVersion)) { throw 'Documentation status does not declare a version.' }
    if ($Version -and $actualVersion -ne $Version) { throw "Documentation version $actualVersion does not match $Version" }
    if ($html.Count -lt 20 -or $html.ApiCount -lt 1) { throw 'Incomplete HTML/API documentation.' }
    $complete = [bool](Get-StatusValue $status @('completeApiReference','CompleteApiReference') $false)
    if ($complete -and $html.ApiCount -lt 100) { throw 'Complete API reference claims fewer than 100 API pages.' }
    if (-not $complete -and [string](Get-StatusValue $status @('documentationMode','DocumentationMode') '') -notmatch 'source') { throw 'Incomplete reference must declare source documentation mode.' }
    $pdfAvailable = [bool](Get-StatusValue $status @('pdfAvailable','PdfAvailable') $true)
    $pdfName = [string](Get-StatusValue $status @('pdfFileName','PdfFileName') '')
    if ($pdfAvailable) {
        if (-not $pdfName) { throw 'Documentation status is missing pdfFileName.' }
        $pdf = Assert-ContainedPath $Root (Join-Path $Root $pdfName)
        if (-not (Test-Path -LiteralPath $pdf -PathType Leaf)) { throw "Missing documentation PDF $pdfName" }
        $length = (Get-Item -LiteralPath $pdf).Length
        if ($length -lt $minimumPdf) { throw 'Documentation PDF is too small.' }
        $declared = Get-StatusValue $status @('pdfBytes','PdfBytes') $null
        if ($null -ne $declared -and [long]$declared -ne $length) { throw 'Declared PDF length does not match actual PDF.' }
        $stream = [IO.File]::OpenRead($pdf)
        try {
            $bytes = New-Object byte[] ([Math]::Min(4194304L, $length))
            $read = $stream.Read($bytes, 0, $bytes.Length)
            $header = [Text.Encoding]::ASCII.GetString($bytes, 0, [Math]::Min($read, 16))
            if (-not $header.StartsWith('%PDF-')) { throw 'Invalid PDF signature.' }
            $preview = [Text.Encoding]::ASCII.GetString($bytes, 0, $read)
            if ($preview.Contains('ReportLab') -or $preview.Contains('Deterministic fallback documentation index')) { throw 'Obsolete PDF placeholder is not valid release documentation.' }
            $mode = [string](Get-StatusValue $status @('pdfAccessibilityMode','PdfAccessibilityMode') 'tagged-pdf-required')
            $preflight = [bool](Get-StatusValue $status @('htmlPreflightValidated','HtmlPreflightValidated') $false)
            if (-not ($mode -eq 'html-accessibility-fallback' -and $preflight) -and -not $preview.Contains('/StructTreeRoot')) {
                # Tagged structure can occur past the initial bounded PDF preview; inspect by streaming text probe.
                $found = $false
                $stream.Position = 0
                $buffer = New-Object byte[] 1048576
                $tail = ''
                while (($n = $stream.Read($buffer,0,$buffer.Length)) -gt 0) {
                    $segment = $tail + [Text.Encoding]::GetEncoding(28591).GetString($buffer,0,$n)
                    if ($segment.Contains('/StructTreeRoot')) { $found = $true; break }
                    $tail = $segment.Substring([Math]::Max(0,$segment.Length - 32))
                }
                if (-not $found) { throw 'PDF missing uncompressed /StructTreeRoot; compressed-object tagging requires the existing PDF accessibility verifier.' }
            }
        } finally { $stream.Dispose() }
    } else {
        $releaseName = [string](Get-StatusValue $status @('releasePdfFileName','pdfFileName') '')
        if (-not $releaseName -or [long](Get-StatusValue $status @('releasePdfBytes','pdfBytes') 0) -lt $minimumPdf) { throw 'HTML-only Pages metadata does not preserve release PDF.' }
        if (Test-Path -LiteralPath (Join-Path $Root $releaseName) -PathType Leaf) { throw 'HTML-only Pages unexpectedly embeds release PDF.' }
    }
}
function Copy-Tree([string]$From, [string]$To, [string]$PdfToExclude) {
    if (Test-Path -LiteralPath $To) { Remove-Item -LiteralPath $To -Recurse -Force }
    New-Item -ItemType Directory -Path $To -Force | Out-Null
    $full = [IO.Path]::GetFullPath($From).TrimEnd([char[]]@('/', '\'))
    foreach ($file in Get-ChildItem -LiteralPath $full -Recurse -File -Force) {
        if ($PdfToExclude -and $file.Name -eq $PdfToExclude) { continue }
        $relative = $file.FullName.Substring($full.Length).TrimStart([char[]]@('/', '\'))
        $target = Assert-ContainedPath $To (Join-Path $To $relative)
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }
    [IO.File]::WriteAllText((Join-Path $To '.nojekyll'), '', [Text.UTF8Encoding]::new($false))
}
try {
    if ([bool]$Archive -eq [bool]$Source) { throw 'Supply exactly one of -Source or -Archive.' }
    if ($Archive) {
        Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $temporaryExtract = Join-Path ([IO.Path]::GetTempPath()) ('localgpt-pages-' + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $temporaryExtract -Force | Out-Null
        $zip = [IO.Compression.ZipFile]::OpenRead($Archive)
        try {
            if ($zip.Entries.Count -gt $maxFiles) { throw 'Pages archive contains too many entries.' }
            $size = 0L; $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
            foreach ($entry in $zip.Entries) {
                $size += $entry.Length
                if ($size -gt $maxBytes) { throw 'Pages archive exceeds safe uncompressed size limit.' }
                $name = $entry.FullName.Replace('\','/')
                if (-not $seen.Add($name) -or $name.StartsWith('/') -or $name -match '(^|/)\.\.(/|$)' -or $name.Contains(':')) { throw "Unsafe/duplicate Pages archive entry: $name" }
            }
        } finally { $zip.Dispose() }
        [IO.Compression.ZipFile]::ExtractToDirectory($Archive, $temporaryExtract)
        $Source = $temporaryExtract
        if (-not (Test-Path -LiteralPath (Join-Path $Source 'index.html'))) {
            $nested = @(Get-ChildItem -LiteralPath $Source -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'index.html') })
            if ($nested.Count -ne 1) { throw 'Pages archive must have one documentation root.' }
            $Source = $nested[0].FullName
        }
    }
    $Source = [IO.Path]::GetFullPath($Source)
    Assert-Documentation $Source $ExpectedVersion ([bool]$HtmlOnly)
    if ($HtmlOnly) { return }
    if (-not $Output) { throw 'Documentation validation needs -Output when not using -HtmlOnly.' }
    $status = Read-Json (Join-Path $Source 'documentation-status.json')
    if ($Archive) { Copy-Tree $Source $Output '' }
    else {
        $pdfName = [string](Get-StatusValue $status @('pdfFileName','PdfFileName') '')
        Copy-Tree $Source $Output $pdfName
        $status.pdfAvailable = $false
        $status | Add-Member -NotePropertyName releasePdfFileName -NotePropertyValue $pdfName -Force
        $status | Add-Member -NotePropertyName releasePdfTagged -NotePropertyValue $true -Force
        $status | Add-Member -NotePropertyName releasePdfBytes -NotePropertyValue ([long](Get-StatusValue $status @('pdfBytes','PdfBytes') 0)) -Force
        $status | Add-Member -NotePropertyName pagesPdfPublished -NotePropertyValue $false -Force
        $status | Add-Member -NotePropertyName pagesPdfExcludedReason -NotePropertyValue 'Release PDF is distributed separately.' -Force
        [IO.File]::WriteAllText((Join-Path $Output 'documentation-status.json'), ($status | ConvertTo-Json -Depth 20), [Text.UTF8Encoding]::new($false))
        if ($pdfName) {
            foreach ($html in Get-ChildItem -LiteralPath $Output -Recurse -File -Filter '*.html') {
                $body = [IO.File]::ReadAllText($html.FullName)
                if ($body.Contains($pdfName)) {
                    $body = [regex]::Replace($body, '(?i)href=["\x27](?:\.\./|\./)?' + [regex]::Escape($pdfName) + '["\x27]', 'href="https://github.com/Michi0403/LocalGPT/releases/latest"')
                    [IO.File]::WriteAllText($html.FullName, $body, [Text.UTF8Encoding]::new($false))
                }
            }
        }
    }
    Assert-Documentation ([IO.Path]::GetFullPath($Output)) $ExpectedVersion $false
    $meta = [ordered]@{
        deploymentSource = $(if ($Archive) { 'tracked Kawaii documentation snapshot' } else { 'explicit documentation directory (HTML-only Pages snapshot)' })
        version = [string](Get-StatusValue $status @('version','Version') '')
        artifact = [IO.Path]::GetFullPath($Output)
        pagesPdfPublished = [bool]$Archive -and [bool](Get-StatusValue $status @('pdfAvailable','PdfAvailable') $false)
        localLinksValid = $true
        htmlAccessibilityValid = $true
        sourceArchiveSha256 = $(if ($Archive) { (Get-FileHash -Algorithm SHA256 -LiteralPath $Archive).Hash.ToLowerInvariant() } else { '' })
    }
    [IO.File]::WriteAllText((Join-Path $Output 'github-pages-deployment.json'), ($meta | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    Write-Host "Pages artifact validated by PowerShell: $Output"
} finally {
    if ($temporaryExtract -and (Test-Path -LiteralPath $temporaryExtract)) { Remove-Item -LiteralPath $temporaryExtract -Recurse -Force -ErrorAction SilentlyContinue }
}
