param([switch]$RunGuestSmoke, [string]$RuntimeOutputDirectory = '')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Assert-SourceContains([string]$RelativePath, [string]$Pattern, [string]$Description) {
    $file = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing sandbox source: $RelativePath" }
    $source = [System.IO.File]::ReadAllText($file)
    if (-not [regex]::IsMatch($source, $Pattern)) { throw "Session sandbox isolation contract missing: $Description ($RelativePath)" }
}

$backend = 'src/LocalGPT/Services/SessionSandboxExecutionService.cs'
$provider = 'src/LocalGPT/Services/HyperlightUnikraftSessionSandboxProvider.cs'
$ui = 'src/LocalGPT/Components/Pages/Chat.razor'
Assert-SourceContains $backend 'ISessionSandboxExecutionService\s*,\s*IHostedService' 'backend-owned hosted execution'
Assert-SourceContains $backend 'CreateLinkedTokenSource' 'independent bounded cancellation'
Assert-SourceContains $backend 'Kill\(entireProcessTree:\s*true\)' 'process tree shutdown'
Assert-SourceContains $backend 'MaximumCapturedCharactersPerStream' 'bounded execution evidence'
Assert-SourceContains $backend 'if \(!request\.UserConfirmed\)' 'fresh execution approval required'
Assert-SourceContains $backend 'LocalGptApplicationDataPaths\.ResolveUserPath' 'cross-platform user-scoped persistence'
Assert-SourceContains $provider 'UseShellExecute\s*=\s*false' 'shell-disabled process invocation'
Assert-SourceContains $provider 'start\.ArgumentList\.Add\("--initrd"\)' 'explicit bundled guest image isolation'
Assert-SourceContains $provider 'start\.ArgumentList\.Add\("--scratch-mb"\)' 'bounded runtime-specific guest scratch budget'
Assert-SourceContains $provider 'hyperlight-powershell' 'PowerShell as isolated guest, not host fallback'
Assert-SourceContains $provider 'File.Exists\(guestImage\)' 'guest image required before launch'
Assert-SourceContains $provider 'VerifyBundledRuntime' 'guest and bundled executable integrity checked before execution'
Assert-SourceContains 'src/LocalGPT/LocalGPT.csproj' 'ProvisionLocalGptHyperlightGuestsForBuild' 'first build provisions guests'
Assert-SourceContains 'src/LocalGPT/LocalGPT.csproj' 'ProvisionLocalGptHyperlightGuestsForPublish' 'native publish requires bundled guests'
Assert-SourceContains 'build/Provision-HyperlightGuestRuntimes.ps1' 'SHA256SUMS' 'upstream executable SHA256 verification'
Assert-SourceContains 'build/Provision-HyperlightGuestRuntimes.ps1' 'dotnet-jit.*powershell' 'all three bundled guest images'
Assert-SourceContains $provider 'start\.Environment\.Clear\(\)' 'no inherited model/API credentials'
Assert-SourceContains $ui 'data-testid="session-sandbox-workspace"' 'session tools UI'
Assert-SourceContains $ui 'data-testid="session-sandbox-codedom"' 'explicit CodeDOM source staging in session tools'
Assert-SourceContains 'src/LocalGPT/Services/CodeDomSourceService.cs' 'GenerateCodeFromCompileUnit' 'typed shared CodeDOM source generation'
Assert-SourceContains 'src/LocalGPT/Services/CodeDomSourceService.cs' 'includeSandboxEntryPoint' 'standalone CodeDOM test harness'
Assert-SourceContains 'src/LocalGPT/Services/CodeGenerationWorkflowService.WorkspacePersistence.cs' 'codeDomSource\.GenerateCSharpSource' 'existing review workflow shares CodeDOM generator'
Assert-SourceContains 'src/LocalGPT/Components/Pages/Chat.Sandbox.razor.cs' 'CodeDomSources\.GenerateCSharpSandboxProgram' 'preview requires a separate run click'
Assert-SourceContains 'src/LocalGPT/Services/SessionSandboxExecutionService.cs' 'SHA256\.HashData' 'submitted-source digest in backend receipts'
Assert-SourceContains 'src/LocalGPT/BusinessObjects/SessionSandboxModels.cs' 'SourceSha256' 'persisted source identity without executable bytes'

# Regression coverage for the 6.6.6 build failures. These assertions complement the
# repository-wide static-policy guard and the Razor/C# compiler; neither is bypassed.
Assert-SourceContains 'src/LocalGPT/Components/Pages/Chat.Sandbox.razor.cs' 'using\s+LocalGPT\.Components\.Shared\s*;' 'shared DevExpress selection-option namespace in the compiled Chat partial'
Assert-SourceContains $ui '@if\s*\(sandbox\.ExitCode\s+is\s+int\s+sandboxExitCode\)' 'exit-code pattern variable does not collide with Razor @code directive'
Assert-SourceContains $ui 'Exit code:\s*@sandboxExitCode' 'exit-code output uses a Razor-safe expression'
foreach ($service in @('src/LocalGPT/Services/CodeDomSourceService.cs', 'src/LocalGPT/Services/SessionSandboxExecutionService.cs', 'src/LocalGPT/Services/HyperlightUnikraftSessionSandboxProvider.cs')) {
    $text = [IO.File]::ReadAllText((Join-Path $root $service), [Text.Encoding]::UTF8)
    if ($text -match '(?m)^\s*(?:public|private|protected|internal)\s+(?:(?:sealed|partial|new|unsafe|readonly|abstract)\s+)*static\s+') {
        throw "Session sandbox regression: $service contains an application-level static declaration."
    }
}

Assert-SourceContains 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs' 'SandboxRuns\.Changed\s*-=' 'UI observer detachment'
Assert-SourceContains 'src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs' 'SandboxRuns\.Changed\s*\+=' 'UI observer attachment'

$providerSource = [System.IO.File]::ReadAllText((Join-Path $root $provider))
foreach ($unsafe in @('start.ArgumentList.Add("--net")', 'start.ArgumentList.Add("--mount")', 'start.ArgumentList.Add("--port")', 'UseShellExecute = true')) {
    if ($providerSource.Contains($unsafe)) { throw "Sandbox isolation regression: $unsafe" }
}
Write-Host 'Session sandbox PowerShell source contract passed: backend lifetime, host-deny isolation, cancellation, evidence, CodeDOM staging and session UI.'

if (-not $RunGuestSmoke) { return }

# Opt-in integration tests exercise the exact build/publish bundled CPIO images and CLI.
# No host Python, .NET, or PowerShell is used to interpret the submitted guest code.
if (-not $RuntimeOutputDirectory) { $RuntimeOutputDirectory = Join-Path $root 'src/LocalGPT/bin/Debug/net10.0' }
$bundle = Join-Path $RuntimeOutputDirectory 'hyperlight'
$hluk = Join-Path $bundle ('bin/' + $(if ($env:OS -eq 'Windows_NT') { 'hluk.exe' } else { 'hluk' }))
$images = Join-Path $bundle 'runtimes'
$receipt = Join-Path $images 'manifest.json'
if (-not (Test-Path -LiteralPath $hluk -PathType Leaf) -or -not (Test-Path -LiteralPath $receipt -PathType Leaf)) {
    throw 'Guest smoke tests require the build-provisioned hluk executable and runtime manifest. No fallback to the host runtime.'
}
$manifest = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
if ($manifest.Version -ne 'v0.19.1') { throw 'The bundled Hyperlight version is not the expected pinned driver version.' }
if ((Get-FileHash -LiteralPath $hluk -Algorithm SHA256).Hash -ne $manifest.BinarySha256) {
    throw 'Bundled hluk SHA-256 differs from the provisioning receipt.'
}
foreach ($id in @('python','dotnet-jit','powershell')) {
    $cpio = Join-Path $images ($id + '.cpio')
    if (-not (Test-Path -LiteralPath $cpio -PathType Leaf)) { throw "Guest rootfs is missing: $cpio" }
    if ((Get-FileHash -LiteralPath $cpio -Algorithm SHA256).Hash -ne $manifest.Images.$id) {
        throw "Guest rootfs SHA-256 mismatch: $id"
    }
}

$workspace = Join-Path ([IO.Path]::GetTempPath()) ('localgpt-hluk-smoke-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($workspace) | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding($false)
try {
    $pythonFile = Join-Path $workspace 'main.py'
    [IO.File]::WriteAllText($pythonFile, 'print("SANDBOX_PYTHON_OK")', $utf8)
    $pythonOutput = (& $hluk run --initrd (Join-Path $images 'python.cpio') --scratch-mb 256 $pythonFile 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0 -or $pythonOutput -notmatch 'SANDBOX_PYTHON_OK') {
        throw "Python did not execute successfully in a guest micro-VM: $pythonOutput"
    }
    Write-Host 'Hyperlight Python guest smoke passed.'

    $csharpFile = Join-Path $workspace 'Hello.cs'
    [IO.File]::WriteAllText($csharpFile, 'using System; class Program { static void Main() { Console.WriteLine("SANDBOX_CSHARP_OK"); } }', $utf8)
    $csharpOutput = (& $hluk run --initrd (Join-Path $images 'dotnet-jit.cpio') --scratch-mb 768 $csharpFile 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0 -or $csharpOutput -notmatch 'SANDBOX_CSHARP_OK') {
        throw "C# did not execute successfully in a guest micro-VM: $csharpOutput"
    }
    Write-Host 'Hyperlight C#/.NET JIT guest smoke passed.'

    $powershellFile = Join-Path $workspace 'Hello.ps1'
    [IO.File]::WriteAllText($powershellFile, 'Write-Output "SANDBOX_POWERSHELL_OK"', $utf8)
    $powershellOutput = (& $hluk run --initrd (Join-Path $images 'powershell.cpio') --scratch-mb 1024 $powershellFile 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0 -or $powershellOutput -notmatch 'SANDBOX_POWERSHELL_OK') {
        throw "PowerShell did not execute successfully in a guest micro-VM: $powershellOutput"
    }
    Write-Host 'Hyperlight PowerShell guest smoke passed.' 

    $networkFile = Join-Path $workspace 'no-network.py'
    [IO.File]::WriteAllText($networkFile, @'
import socket
try:
    socket.create_connection(("1.1.1.1", 80), timeout=2)
except OSError:
    print("SANDBOX_NETWORK_DENIED")
else:
    raise RuntimeError("Guest unexpectedly reached the network")
'@, $utf8)
    $networkOutput = (& $hluk run --initrd (Join-Path $images 'python.cpio') --scratch-mb 256 $networkFile 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0 -or $networkOutput -notmatch 'SANDBOX_NETWORK_DENIED') {
        throw "Guest outbound network isolation did not pass: $networkOutput"
    }
    Write-Host 'Hyperlight guest network-denial smoke passed.'
}
finally {
    if (Test-Path -LiteralPath $workspace) { [IO.Directory]::Delete($workspace, $true) }
}
