# Shared native release text and macOS Installer script contracts.
# This file is safe to dot-source from both the package builder and preflight guards.
function Write-Utf8NoBom([string]$Path, [string]$Text) {
    # CRLF inside a PowerShell here-string must never enter a Unix executable or
    # Installer resource. In particular, '#!/bin/sh\r\n' fails as './preinstall'.
    $normalized = $Text.Replace("`r`n", "`n").Replace("`r", "`n")
    [IO.File]::WriteAllText($Path, $normalized, [Text.UTF8Encoding]::new($false))
}

function Assert-MacPackageTemplateFile([string]$Path,[bool]$MustHaveShebang) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing macOS package script template: $Path"
    }
    $bytes = [IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -eq 0) { throw "Empty macOS package script template: $Path" }
    if (($bytes.Length -ge 3) -and ($bytes[0] -eq 239) -and ($bytes[1] -eq 187) -and ($bytes[2] -eq 191)) {
        throw "macOS package script template has a UTF-8 BOM: $Path"
    }
    if ([Array]::IndexOf($bytes, [byte]13) -ge 0) { throw "macOS package script template contains CR/CRLF instead of LF: $Path" }
    if ([Array]::IndexOf($bytes, [byte]0) -ge 0) { throw "macOS package script template contains a NUL byte: $Path" }
    $content = [IO.File]::ReadAllText($Path)
    if ($MustHaveShebang -and -not $content.StartsWith("#!/bin/sh`n", [StringComparison]::Ordinal)) {
        throw "macOS package lifecycle template must start with an LF-terminated #!/bin/sh: $Path"
    }
    if (-not $MustHaveShebang -and $content.StartsWith('#!', [StringComparison]::Ordinal)) {
        throw "macOS package lifecycle fragment must not introduce a second interpreter directive: $Path"
    }
}

function Get-MacPackageScriptText([string]$TemplateRoot,[string]$ProductName,[string]$ExecutableName,[ValidateSet('preinstall','postinstall')][string]$Phase) {
    $commonFile = Join-Path $TemplateRoot 'lifecycle.common.sh.in'
    $phaseFile = Join-Path $TemplateRoot ($Phase + '.sh.in')
    Assert-MacPackageTemplateFile -Path $commonFile -MustHaveShebang $true
    Assert-MacPackageTemplateFile -Path $phaseFile -MustHaveShebang $false
    $common = [IO.File]::ReadAllText($commonFile)
    $common = $common.Replace('__PRODUCT__', $ProductName).Replace('__EXECUTABLE__', $ExecutableName).Replace('__PRODUCT_LOWER__', $ProductName.ToLowerInvariant())
    $phaseText = [IO.File]::ReadAllText($phaseFile)
    return $common.TrimEnd([char[]]"`r`n") + "`n" + $phaseText.TrimStart([char[]]"`r`n")
}

function Assert-PortableShellScript([string]$Path,[switch]$RequireShell,[switch]$CheckExecutable) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Missing macOS package shell script: $Path" }
    $bytes = [IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 10) { throw "macOS shell script is too short: $Path" }
    if (($bytes[0] -eq 239) -and ($bytes[1] -eq 187) -and ($bytes[2] -eq 191)) {
        throw "macOS shell script has UTF-8 BOM: $Path"
    }
    if ([Array]::IndexOf($bytes, [byte]13) -ge 0) { throw "macOS shell script has CR/CRLF (invalid #!/bin/sh interpreter): $Path" }
    if ([Array]::IndexOf($bytes, [byte]0) -ge 0) { throw "macOS shell script contains NUL: $Path" }
    $shebang = [Text.Encoding]::ASCII.GetBytes("#!/bin/sh`n")
    for ($i = 0; $i -lt $shebang.Length; $i++) {
        if ($bytes[$i] -ne $shebang[$i]) { throw "macOS shell script lacks the exact LF-terminated #!/bin/sh header: $Path" }
    }
    $runningOnMac = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::OSX)
    $runningOnLinux = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Linux)
    # Git-for-Windows may expose sh.exe, but passing an unconverted C:\ path to it
    # causes a false failure. Byte checks run everywhere; shell syntax runs on Unix.
    if ($runningOnMac -or $runningOnLinux) {
        $shell = Get-Command 'sh' -ErrorAction SilentlyContinue
        if (-not $shell) {
            if ($RequireShell) { throw "Cannot validate macOS shell syntax: 'sh' was not found ($Path)." }
        }
        else {
            & $shell.Source -n $Path
            if ($LASTEXITCODE -ne 0) { throw "macOS shell syntax validation ('sh -n') failed: $Path" }
        }
    }
    if ($CheckExecutable) {
        if ($runningOnMac -or $runningOnLinux) {
            $testExecutable = if (Test-Path -LiteralPath '/usr/bin/test' -PathType Leaf) { '/usr/bin/test' } else { '/bin/test' }
            if (-not (Test-Path -LiteralPath $testExecutable -PathType Leaf)) { throw "Cannot validate Unix executable mode: $testExecutable missing." }
            & $testExecutable '-x' $Path
            if ($LASTEXITCODE -ne 0) { throw "macOS package shell script lacks executable permission: $Path" }
        }
    }
}

function Assert-MacPackageResources([string]$ResourcesDirectory) {
    foreach ($name in @('README.txt', 'LICENSE.txt', 'background.png')) {
        $path = Join-Path $ResourcesDirectory $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "macOS Installer resource missing: $path" }
        if ((Get-Item -LiteralPath $path).Length -eq 0) { throw "macOS Installer resource empty: $path" }
    }
    $backgroundPath = Join-Path $ResourcesDirectory 'background.png'
    $header = [IO.File]::ReadAllBytes($backgroundPath)
    $pngSignature = [byte[]]@(137,80,78,71,13,10,26,10)
    if ($header.Length -lt $pngSignature.Length) { throw "macOS Installer background is not PNG: $backgroundPath" }
    for ($i=0; $i -lt $pngSignature.Length; $i++) {
        if ($header[$i] -ne $pngSignature[$i]) { throw "macOS Installer background has an invalid PNG signature: $backgroundPath" }
    }
}
