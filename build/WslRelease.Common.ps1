Set-StrictMode -Version Latest

function Test-WslReleaseWindowsHost {
    return [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)
}

function Get-WslReleaseExecutable {
    if (-not (Test-WslReleaseWindowsHost)) { return $null }
    $command = Get-Command wsl.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $command) { return $null }
    if (-not [string]::IsNullOrWhiteSpace([string]$command.Source)) { return [string]$command.Source }
    return [string]$command.Path
}

function ConvertFrom-WslReleaseNameOutput {
    param([AllowEmptyCollection()][object[]]$Lines = @())
    return @(
        $Lines |
            ForEach-Object { ([string]$_).Replace([char]0, '').Trim() } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
}

function Get-WslReleaseDistributions {
    param([Parameter(Mandatory)][string]$WslExecutable)
    $output = @(& $WslExecutable --list --quiet 2>$null)
    if ($LASTEXITCODE -ne 0) { return @() }
    return @(ConvertFrom-WslReleaseNameOutput $output)
}

function Get-WslReleaseRunningDistributions {
    param([Parameter(Mandatory)][string]$WslExecutable)
    $output = @(& $WslExecutable --list --running --quiet 2>$null)
    if ($LASTEXITCODE -ne 0) { return @() }
    return @(ConvertFrom-WslReleaseNameOutput $output)
}

function Resolve-WslReleaseDistribution {
    param(
        [Parameter(Mandatory)][string]$WslExecutable,
        [string]$RequestedDistribution = ''
    )

    $installed = @(Get-WslReleaseDistributions -WslExecutable $WslExecutable)
    if ($installed.Count -eq 0) { return $null }

    $requested = $RequestedDistribution
    if ([string]::IsNullOrWhiteSpace($requested)) { $requested = [Environment]::GetEnvironmentVariable('WSL_BUILD_DISTRO') }
    if (-not [string]::IsNullOrWhiteSpace($requested)) {
        $match = @($installed | Where-Object { [string]::Equals($_, $requested.Trim(), [StringComparison]::OrdinalIgnoreCase) }) | Select-Object -First 1
        if ($null -ne $match) { return [string]$match }
        return $null
    }

    $candidates = @($installed | Where-Object { $_ -notmatch '^docker-desktop(?:-data)?$' })
    if ($candidates.Count -eq 0) { return $null }
    $ubuntuExact = @($candidates | Where-Object { [string]::Equals($_, 'Ubuntu', [StringComparison]::OrdinalIgnoreCase) }) | Select-Object -First 1
    if ($null -ne $ubuntuExact) { return [string]$ubuntuExact }
    $ubuntu = @($candidates | Where-Object { $_ -match '^Ubuntu(?:-|$)' }) | Select-Object -First 1
    if ($null -ne $ubuntu) { return [string]$ubuntu }
    $debian = @($candidates | Where-Object { $_ -match '^Debian(?:-|$)' }) | Select-Object -First 1
    if ($null -ne $debian) { return [string]$debian }
    return [string]$candidates[0]
}

function ConvertTo-WslReleasePath {
    param(
        [Parameter(Mandatory)][string]$WslExecutable,
        [Parameter(Mandatory)][string]$Distribution,
        [Parameter(Mandatory)][string]$WindowsPath
    )
    $output = @(& $WslExecutable -d $Distribution -- wslpath -a -u $WindowsPath 2>$null)
    if ($LASTEXITCODE -ne 0) { throw "WSL could not translate Windows path '$WindowsPath' for distribution '$Distribution'." }
    $values = @(ConvertFrom-WslReleaseNameOutput $output)
    if ($values.Count -ne 1) { throw "WSL path translation for '$WindowsPath' returned $($values.Count) value(s); expected one." }
    return [string]$values[0]
}

function Get-WslReleaseBuildStatus {
    param(
        [Parameter(Mandatory)][string]$WslExecutable,
        [Parameter(Mandatory)][string]$Distribution
    )

    $probe = @'
set +e
export PATH="$HOME/.local/bin:$PATH"
printf 'linux=%s\n' "$(uname -s 2>/dev/null)"
printf 'arch=%s\n' "$(uname -m 2>/dev/null)"
printf 'kernel=%s\n' "$(uname -r 2>/dev/null)"
case "$(uname -r 2>/dev/null)" in *[Ww][Ss][Ll]2*|*microsoft-standard-WSL2*) printf 'wsl2=0\n' ;; *) printf 'wsl2=1\n' ;; esac
printf 'home=%s\n' "$HOME"
command -v pwsh >/dev/null 2>&1; printf 'pwsh=%s\n' "$?"
command -v dotnet >/dev/null 2>&1; dotnet_cmd=$?; printf 'dotnet=%s\n' "$dotnet_cmd"
if [ "$dotnet_cmd" -eq 0 ]; then dotnet --list-sdks 2>/dev/null | grep -q '^10[.]'; printf 'dotnet10=%s\n' "$?"; else printf 'dotnet10=1\n'; fi
command -v python3 >/dev/null 2>&1; printf 'python3=%s\n' "$?"
command -v rpmbuild >/dev/null 2>&1; printf 'rpmbuild=%s\n' "$?"
command -v appimagetool >/dev/null 2>&1; printf 'appimagetool=%s\n' "$?"
test -s "$HOME/.config/DevExpress/DevExpress_License.txt"; printf 'devexpressFile=%s\n' "$?"
'@
    $output = @(& $WslExecutable -d $Distribution -- bash -lc $probe 2>$null)
    if ($LASTEXITCODE -ne 0) {
        return [pscustomobject]@{ Available = $false; CoreReady = $false; Linux = ''; Architecture = ''; Kernel = ''; Wsl2 = $false; Home = ''; Pwsh = $false; DotNet = $false; DotNet10 = $false; Python3 = $false; RpmBuild = $false; AppImageTool = $false; DevExpressFile = $false; Detail = 'The distribution could not run the build-environment probe. Complete its first-launch user initialization and retry.' }
    }
    $values = @{}
    foreach ($line in $output) {
        $value = ([string]$line).Trim()
        $index = $value.IndexOf('=')
        if ($index -gt 0) { $values[$value.Substring(0, $index)] = $value.Substring($index + 1) }
    }
    $linuxName = if ($values.ContainsKey('linux')) { [string]$values['linux'] } else { '' }
    $pwshReady = $values.ContainsKey('pwsh') -and [string]$values['pwsh'] -eq '0'
    $dotnetReady = $values.ContainsKey('dotnet') -and [string]$values['dotnet'] -eq '0'
    $dotnet10Ready = $values.ContainsKey('dotnet10') -and [string]$values['dotnet10'] -eq '0'
    $pythonReady = $values.ContainsKey('python3') -and [string]$values['python3'] -eq '0'
    $linuxReady = [string]::Equals($linuxName, 'Linux', [StringComparison]::OrdinalIgnoreCase)
    $wsl2Ready = $values.ContainsKey('wsl2') -and [string]$values['wsl2'] -eq '0'
    return [pscustomobject]@{
        Available = $true
        CoreReady = ($linuxReady -and $wsl2Ready -and $pwshReady -and $dotnetReady -and $dotnet10Ready -and $pythonReady)
        Linux = $linuxName
        Architecture = $(if ($values.ContainsKey('arch')) { [string]$values['arch'] } else { '' })
        Kernel = $(if ($values.ContainsKey('kernel')) { [string]$values['kernel'] } else { '' })
        Wsl2 = $wsl2Ready
        Home = $(if ($values.ContainsKey('home')) { [string]$values['home'] } else { '' })
        Pwsh = $pwshReady
        DotNet = $dotnetReady
        DotNet10 = $dotnet10Ready
        Python3 = $pythonReady
        RpmBuild = ($values.ContainsKey('rpmbuild') -and [string]$values['rpmbuild'] -eq '0')
        AppImageTool = ($values.ContainsKey('appimagetool') -and [string]$values['appimagetool'] -eq '0')
        DevExpressFile = ($values.ContainsKey('devexpressFile') -and [string]$values['devexpressFile'] -eq '0')
        Detail = ''
    }
}

function Get-WslReleaseWindowsDevExpressLicenseDirectory {
    $configured = [Environment]::GetEnvironmentVariable('DevExpress_LicensePath')
    if (-not [string]::IsNullOrWhiteSpace($configured)) {
        $expanded = [Environment]::ExpandEnvironmentVariables($configured.Trim())
        if (Test-Path -LiteralPath $expanded -PathType Leaf) { $expanded = Split-Path -Parent ([IO.Path]::GetFullPath($expanded)) }
        $candidate = Join-Path $expanded 'DevExpress_License.txt'
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            if ((Get-Item -LiteralPath $candidate).Length -gt 0) { return [IO.Path]::GetFullPath($expanded) }
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($env:APPDATA)) {
        $defaultDirectory = Join-Path $env:APPDATA 'DevExpress'
        $defaultFile = Join-Path $defaultDirectory 'DevExpress_License.txt'
        if (Test-Path -LiteralPath $defaultFile -PathType Leaf) {
            if ((Get-Item -LiteralPath $defaultFile).Length -gt 0) { return [IO.Path]::GetFullPath($defaultDirectory) }
        }
    }
    return $null
}

function Enable-WslReleaseDevExpressBridge {
    $previousWslEnv = $env:WSLENV
    $previousLicensePath = [Environment]::GetEnvironmentVariable('DevExpress_LicensePath')
    $entries = [System.Collections.Generic.List[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($previousWslEnv)) {
        foreach ($entry in $previousWslEnv.Split([char]':')) {
            if (-not [string]::IsNullOrWhiteSpace($entry)) { $entries.Add($entry) }
        }
    }

    # Normalize any caller-provided DevExpress WSLENV entries before installing the
    # Windows -> WSL bridge. /w means include the value when WSL is launched from
    # Win32; /p additionally translates a Windows path to its WSL path. /u is the
    # opposite direction and must not be used by this coordinator.
    $normalizedEntries = [System.Collections.Generic.List[string]]::new()
    foreach ($entry in $entries) {
        if ($entry -notmatch '^DevExpress_License(?:Path)?(?:/|$)') { $normalizedEntries.Add($entry) }
    }
    $entries = $normalizedEntries

    $licenseValue = [Environment]::GetEnvironmentVariable('DevExpress_License')
    if (-not [string]::IsNullOrWhiteSpace($licenseValue)) { $entries.Add('DevExpress_License/w') }

    $licenseDirectory = Get-WslReleaseWindowsDevExpressLicenseDirectory
    if (-not [string]::IsNullOrWhiteSpace($licenseDirectory)) {
        [Environment]::SetEnvironmentVariable('DevExpress_LicensePath', $licenseDirectory, [EnvironmentVariableTarget]::Process)
        $entries.Add('DevExpress_LicensePath/pw')
    }

    if ($entries.Count -gt 0) { $env:WSLENV = ($entries -join ':') } else { Remove-Item Env:WSLENV -ErrorAction SilentlyContinue }
    return [pscustomobject]@{ PreviousWslEnv = $previousWslEnv; PreviousLicensePath = $previousLicensePath; WindowsLicenseDirectory = $licenseDirectory; HasLicenseValue = (-not [string]::IsNullOrWhiteSpace($licenseValue)) }
}

function Disable-WslReleaseDevExpressBridge {
    param([Parameter(Mandatory)]$State)
    if ([string]::IsNullOrWhiteSpace([string]$State.PreviousWslEnv)) { Remove-Item Env:WSLENV -ErrorAction SilentlyContinue } else { $env:WSLENV = [string]$State.PreviousWslEnv }
    $previousPath = if ($null -eq $State.PreviousLicensePath -or [string]::IsNullOrWhiteSpace([string]$State.PreviousLicensePath)) { $null } else { [string]$State.PreviousLicensePath }
    [Environment]::SetEnvironmentVariable('DevExpress_LicensePath', $previousPath, [EnvironmentVariableTarget]::Process)
}

function Test-WslReleaseDevExpressLicenseAvailable {
    param(
        [Parameter(Mandatory)][string]$WslExecutable,
        [Parameter(Mandatory)][string]$Distribution,
        [Parameter(Mandatory)]$Status
    )
    if ($Status.DevExpressFile) { return $true }
    if (-not [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable('DevExpress_License'))) { return $true }
    if (-not [string]::IsNullOrWhiteSpace((Get-WslReleaseWindowsDevExpressLicenseDirectory))) { return $true }
    return $false
}

function Get-WslReleaseReadinessMessage {
    param([Parameter(Mandatory)]$Status)
    $missing = [System.Collections.Generic.List[string]]::new()
    if (-not $Status.Wsl2) { $missing.Add('WSL2 (convert the distro with wsl.exe --set-version <name> 2)') }
    if (-not $Status.Pwsh) { $missing.Add('PowerShell (pwsh)') }
    if (-not $Status.DotNet10) { $missing.Add('.NET 10 SDK') }
    if (-not $Status.Python3) { $missing.Add('Python 3') }
    if ($missing.Count -eq 0) { return 'core build tools are ready' }
    return 'missing ' + ($missing -join ', ')
}
