param(
    [Parameter(Mandatory)][string]$LocalGptAssembly,
    [Parameter(Mandatory)][string]$NativePython,
    [Parameter(Mandatory)][string]$NativeLibrary,
    [Parameter(Mandatory)][string]$IntelOnlyPython
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::OSX) -or
    [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString() -ne 'Arm64') {
    throw 'Run this integration test on Apple silicon.'
}
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $LocalGptAssembly).Path)
$platform = [Activator]::CreateInstance($assembly.GetType('LocalGPT.Services.UnixPlatformRuntimeService', $true))
foreach ($path in @($NativePython, $NativeLibrary, $IntelOnlyPython)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing test binary: $path" }
}
if (-not $platform.IsCompatiblePythonBinary($NativePython) -or -not $platform.IsCompatiblePythonBinary($NativeLibrary)) {
    throw 'Native or universal CPython executable and library must be accepted.'
}
if ($platform.IsCompatiblePythonBinary($IntelOnlyPython)) {
    throw 'Intel-only Python must be rejected before a subprocess or embedded interpreter starts.'
}
if (-not $platform.SupportsPyTorchBackend('mps') -or $platform.SupportsPyTorchBackend('xpu')) {
    throw 'Apple silicon must offer the Apple Metal accelerator profile.'
}
Write-Host 'Verified native CPython executable and library, Intel-only rejection, and Apple Metal profile selection.'
