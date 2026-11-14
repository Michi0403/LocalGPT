[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Assert-SourceContract {
    param([string]$RelativePath, [string]$Expected)
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Family protocol contract is missing its source: $RelativePath"
    }
    $source = [System.IO.File]::ReadAllText($path)
    if ($source.IndexOf($Expected, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Family protocol compatibility contract failed in $RelativePath : expected $Expected"
    }
}

Assert-SourceContract 'src/LocalGPT/BusinessObjects/FamilyWireJsonConventions.cs' 'public void ApplyEnumCompatibility(JsonSerializerOptions options)'
Assert-SourceContract 'src/LocalGPT/BusinessObjects/FamilyWireJsonConventions.cs' 'options.PropertyNameCaseInsensitive = true;'
Assert-SourceContract 'src/LocalGPT/BusinessObjects/FamilyWireJsonConventions.cs' 'DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull'
Assert-SourceContract 'src/LocalGPT/BusinessObjects/FamilyWireJsonConventions.cs' 'new JsonStringEnumConverter()'
Assert-SourceContract 'src/LocalGPT/BusinessObjects/FamilyWireJsonConventions.cs' 'NumberHandling = JsonNumberHandling.AllowReadingFromString'
Assert-SourceContract 'src/LocalGPT/Services/OneWire/OneWireEnvelopeCodec.cs' 'new FamilyWireJsonConventions().CreateOneWireOptions()'
Assert-SourceContract 'src/LocalGPT/Controller/OneWireHttpController.cs' 'return Ok(profile);'
Assert-SourceContract 'src/LocalGPT/Program.WebFeatures.cs' 'new FamilyWireJsonConventions().ApplyEnumCompatibility(options);'
Assert-SourceContract 'src/LocalGPT/Program.WebFeatures.cs' 'options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;'
Assert-SourceContract 'src/LocalGPT/Program.ServiceRegistration.cs' 'options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;'
Assert-SourceContract 'src/LocalGPT/Program.ServiceRegistration.cs' '.AddMessagePackProtocol(options =>'
Assert-SourceContract 'src/LocalGPT/Program.ServiceRegistration.cs' 'MessagePack.Resolvers.ContractlessStandardResolver.Instance'
Assert-SourceContract 'src/LocalGPT/Mcp/LocalGptMcpEndpoint.cs' 'new FamilyWireJsonConventions().CreateWebOptions()'
Assert-SourceContract 'src/LocalGPT/Mcp/LocalGptMcpGatewayAccessor.cs' 'new FamilyWireJsonConventions().CreateWebOptions()'
Assert-SourceContract 'src/LocalGPT/Services/OneWire/OneWireExecutionServices.cs' '["Status"] = JsonSerializer.SerializeToElement(status.ToString(), codec.JsonOptions)'
Assert-SourceContract 'src/LocalGPT/Controller/OneWireHttpController.cs' '["UpdatedUtc"] = JsonSerializer.SerializeToElement(item.UpdatedUtc, codec.JsonOptions)'

# The convention object must remain a stateless configuration value, not a runtime static class.
# Protect this architectural boundary instead of weakening the general static/diagnostics guards.
$conventionPath = Join-Path $root 'src/LocalGPT/BusinessObjects/FamilyWireJsonConventions.cs'
$conventionSource = [System.IO.File]::ReadAllText($conventionPath)
if ($conventionSource -notmatch 'public sealed class FamilyWireJsonConventions' -or
    $conventionSource -match '\bstatic\s+class\s+FamilyWireJsonConventions\b' -or
    $conventionSource -match '\bpublic\s+static\s+(?:void|JsonSerializerOptions)\b') {
    throw 'Family JSON conventions must remain a non-static stateless configuration type.'
}
if (Test-Path -LiteralPath (Join-Path $root 'src/LocalGPT/Services/OneWire/FamilyWireJsonConventions.cs')) {
    throw 'The JSON convention was incorrectly reintroduced as a runtime service.'
}

Write-Host 'Family protocol JSON compatibility source contracts passed; platform runtime round trips still require integration tests.'
