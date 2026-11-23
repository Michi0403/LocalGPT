$ErrorActionPreference = 'Stop'
$app = Join-Path $PSScriptRoot '../src/LocalGPT'
$request = [IO.File]::ReadAllText((Join-Path $app 'Services/OneWire/CreatePublisherDocumentFunction.cs'))
$state = [IO.File]::ReadAllText((Join-Path $app 'Services/OneWire/OneWireStateServices.cs'))
$controller = [IO.File]::ReadAllText((Join-Path $app 'Controller/OneWireController.cs'))
$chat = [IO.File]::ReadAllText((Join-Path $app 'Components/Shared/ChatOfficeDownloads.razor'))

$formats = [ordered]@{
    spreadsheet = 'xlsx'; text = 'docx'; rtf = 'rtf'
    html = 'html'; txt = 'txt'; csv = 'csv'
}
$schema = ([regex]::Match($request, '(?s)ParameterSchemaJson: """\s*(.*?)\s*"""').Groups[1].Value | ConvertFrom-Json)
if (Compare-Object @($formats.Keys) @($schema.properties.kind.enum)) {
    throw 'The PublisherStudio request schema differs from the supported document formats.'
}

$requestGuard = [regex]::Match($request, 'if \(kind is not \([^\r\n]+').Value
$resultGuard = [regex]::Match($state, 'if \(kind is "spreadsheet"[^\r\n]+').Value
foreach ($kind in $formats.Keys) {
    if (-not $requestGuard.Contains('"' + $kind + '"')) {
        throw "PublisherStudio request does not accept $kind."
    }
}

# Previously approved work can still be read and downloaded.
foreach ($kind in @('xls', 'pdf', 'odt', 'epub', 'doc', 'mht')) { $formats[$kind] = $kind }
foreach ($kind in $formats.Keys) {
    if (-not $resultGuard.Contains('"' + $kind + '"') -or
        $controller -notmatch ('"' + $kind + '" => File\(bytes, "[^"]+", "PublisherStudio\.' + $formats[$kind] + '"\)') -or
        ($kind -ne 'text' -and -not $chat.Contains('"' + $kind + '" => "Download '))) {
        throw "PublisherStudio result or download integration is missing for $kind."
    }
}

foreach ($contract in @(
    'ValidateReturnedDocument\(kind, bytes\);\s*item\.DocumentKind = kind;',
    '"%PDF-"u8', '@"\{\\rtf"u8',
    'case "doc":\s*case "xls":[\s\S]*?0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1',
    'case "html":\s*case "txt":\s*case "csv":[\s\S]*?throwOnInvalidBytes: true',
    '"odt" => ReadMimetype\(\) == "application/vnd\.oasis\.opendocument\.text"',
    '"epub" => ReadMimetype\(\) == "application/epub\+zip"',
    '"spreadsheet" => [^\r\n]*"xl/workbook\.xml"',
    '"text" => [^\r\n]*"word/document\.xml"'
)) {
    if ($state -notmatch $contract) {
        throw "Missing PublisherStudio result validation contract: $contract"
    }
}
Write-Host 'PublisherStudio document request, result validation and download contracts passed.'
