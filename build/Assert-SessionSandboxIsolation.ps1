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
Assert-SourceContains $provider 'platformRuntime\.GetNativeExecutableName\("hluk"\)' 'platform-native CLI in provider launch'
Assert-SourceContains $backend 'platformRuntime\.GetNativeExecutableName\("hluk"\)' 'platform-native CLI in runtime readiness'
# Check each platform implementation separately. A matching method in the wrong
# class must never satisfy the Windows or Unix sandbox contract.
$platformSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/PlatformRuntimeServices.cs'))
$windowsStart = $platformSource.IndexOf('public sealed class WindowsPlatformRuntimeService', [StringComparison]::Ordinal)
$unixStart = $platformSource.IndexOf('public sealed class UnixPlatformRuntimeService', [StringComparison]::Ordinal)
if ($windowsStart -lt 0 -or $unixStart -le $windowsStart) {
    throw 'Session sandbox platform contract: Windows and Unix platform implementations are missing or out of order.'
}
$windowsSource = $platformSource.Substring($windowsStart, $unixStart - $windowsStart)
$unixSource = $platformSource.Substring($unixStart)
if (-not [regex]::IsMatch($windowsSource, 'GetNativeExecutableName\(string baseName\) => baseName \+ "\.exe"')) {
    throw 'Session sandbox platform contract: WindowsPlatformRuntimeService must resolve hluk.exe.'
}
if (-not [regex]::IsMatch($unixSource, 'GetNativeExecutableName\(string baseName\) => baseName;')) {
    throw 'Session sandbox platform contract: UnixPlatformRuntimeService must resolve hluk without a Windows extension.'
}
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
# Client-side compilation is a distinct execution location, never an alias for Hyperlight.
$browser = 'src/LocalGPT/wwwroot/browser-compiler/'
Assert-SourceContains 'src/LocalGPT/Program.Middleware.cs' 'StartsWithSegments\("/browser-compiler"\)' 'cross-origin isolation for browser workbench assets'
Assert-SourceContains 'src/LocalGPT/Program.Middleware.cs' 'Cross-Origin-Opener-Policy' 'browser worker COOP'
Assert-SourceContains 'src/LocalGPT/Program.Middleware.cs' 'Cross-Origin-Embedder-Policy' 'browser worker COEP'
Assert-SourceContains 'src/LocalGPT/Program.Middleware.cs' 'connect-src \{assetScope\} blob:' 'browser compiler network policy limited to workbench assets'
Assert-SourceContains $ui 'browser-compiler/index.html' 'distinct browser workbench link'
$chatSandbox = 'src/LocalGPT/Components/Pages/Chat.Sandbox.razor.cs'
Assert-SourceContains $chatSandbox 'browser-wasix-c"' 'C appears in the shared manual runtime selector'
Assert-SourceContains $chatSandbox 'browser-wasix-cpp"' 'C++ appears in the shared manual runtime selector'
Assert-SourceContains $chatSandbox 'if \(IsBrowserSandboxSelected\)[\s\S]*?return Task\.CompletedTask;[\s\S]*?var request = new SessionSandboxRunRequest' 'client selections cannot be queued as server guest executions'
Assert-SourceContains $chatSandbox 'browser-compiler/index\.html\?embedded=1&language=c' 'manual runtime routes to embedded client document'
Assert-SourceContains $ui '<iframe[^>]+data-testid="session-sandbox-browser-frame"' 'compiler stays inside running session tools'
Assert-SourceContains $ui 'allow="cross-origin-isolated"' 'delegate isolation to same-origin compiler frame'
Assert-SourceContains $ui 'sandbox="allow-scripts allow-same-origin allow-downloads"' 'compiler frame has explicit bounded browser permissions'
Assert-SourceContains 'src/LocalGPT/Program.Middleware.cs' 'Request\.Path\.Equals\(new PathString\("/chat"\)\)[\s\S]*?TryGetValue\("browserSandbox"' 'only opted-in Chat documents request isolation headers'
Assert-SourceContains 'src/LocalGPT/Program.Middleware.cs' "frame-ancestors 'self'" 'compiler CSP permits only same-origin embedding'
Assert-SourceContains ($browser + 'workbench.mjs') 'worker\.postMessage\(request\)' 'compile and execute requests are actually handed to the worker'
Assert-SourceContains ($browser + 'workbench.mjs') 'fixedLanguage \?\?' 'embedded source imports preserve the selected language'
Assert-SourceContains ($browser + 'workbench.mjs') 'policy = descriptor;[\s\S]*?replaceChildren[\s\S]*?toolchain/manifest\.json' 'language selection precedes installed toolchain check'
Assert-SourceContains ($browser + 'workbench.mjs') 'if \(!ready \|\| activeWorker\) return;' 'client execution remains disabled until verified readiness'

Assert-SourceContains ($browser + 'compiler-worker.mjs') 'crossOriginIsolated' 'fail closed without browser isolation'
Assert-SourceContains ($browser + 'compiler-worker.mjs') 'Wasmer\.fromFile\(compilerBytes\)' 'local compiler package, not host interpreter'
Assert-SourceContains ($browser + 'compiler-worker.mjs') 'WebAssembly\.validate' 'validate compiled and imported modules'
Assert-SourceContains ($browser + 'compiler-worker.mjs') 'verifiedByServer: false' 'client receipt is not trusted server verification'
Assert-SourceContains ($browser + 'compiler-worker.mjs') 'compilerSha256\.toLowerCase' 'validate installed compiler hash before loading'
Assert-SourceContains ($browser + 'workbench.mjs') 'activeWorker\.terminate\(\)' 'browser worker cancellation'
Assert-SourceContains ($browser + 'guest-export.mjs') 'wasi_snapshot_preview1' 'WASI Preview 1 guest transfer only'
Assert-SourceContains ($browser + 'guest-export.mjs') 'preopens: \{\}' 'guest wrapper does not mount directories'
Assert-SourceContains ($browser + 'guest-export.mjs') 'maximumSourceCharacters' 'guest source size checked before export'
Assert-SourceContains 'build/Provision-BrowserCompilerToolchain.ps1' 'ExpectedClangSha256' 'administrator-reviewed offline toolchain identity'
$browserDescriptor = Get-Content -LiteralPath (Join-Path $root ($browser + 'runtime.json')) -Raw | ConvertFrom-Json
$backendSource = [IO.File]::ReadAllText((Join-Path $root $backend))
$sourceLimit = [regex]::Match($backendSource, 'MaximumSourceCharacters\s*=\s*([0-9_]+)\s*;')
if (-not $sourceLimit.Success -or [int]($sourceLimit.Groups[1].Value.Replace('_', '')) -ne [int]$browserDescriptor.maximumHyperlightSnippetCharacters) {
    throw 'Browser WASI guest export limit must match the existing backend submission limit.'
}
$workerSource = [IO.File]::ReadAllText((Join-Path $root ($browser + 'compiler-worker.mjs')))
if ($workerSource -match 'Wasmer\.fromRegistry\s*\(|https?://|wispUrl\s*:') {
    throw 'Browser compiler must not resolve runtime registry/CDN packages or supply a guest network bridge.'
}
Write-Host 'Session sandbox PowerShell source contract passed: backend isolation and separately identified browser-owned compilation.'

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
