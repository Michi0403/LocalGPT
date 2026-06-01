[CmdletBinding()]
param(
    [switch]$Install,
    [switch]$InstallGradle,
    [switch]$InstallEclipse,
    [string]$GradleVersion = "8.14.2"
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "== $Message =="
}

function Get-JavaHome {
    $candidates = @(
        $env:JAVA_HOME,
        "$env:ProgramFiles\Microsoft\jdk-21.0.11.10-hotspot",
        "$env:ProgramFiles\Eclipse Adoptium\jdk-21*"
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($candidate in $candidates) {
        if (Test-Path (Join-Path $candidate "bin\java.exe")) {
            return $candidate
        }

        $resolved = Get-ChildItem $candidate -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -ne $resolved -and (Test-Path (Join-Path $resolved.FullName "bin\java.exe"))) {
            return $resolved.FullName
        }
    }

    return $null
}

function Install-Jdk21 {
    Write-Step "Installing Microsoft OpenJDK 21"
    & winget install --id Microsoft.OpenJDK.21 --source winget --accept-package-agreements --accept-source-agreements
}

function Install-EclipseJava {
    Write-Step "Installing Eclipse IDE for Java Developers"
    & winget install --id EclipseFoundation.Eclipse.Java --source winget --accept-package-agreements --accept-source-agreements
}

function Install-LocalGradle {
    param([string]$Version)

    $toolRoot = Join-Path $env:LOCALAPPDATA "LocalGPT\Tools"
    $installRoot = Join-Path $toolRoot "gradle-$Version"
    $gradleExe = Join-Path $installRoot "bin\gradle.bat"

    if (Test-Path $gradleExe) {
        Write-Host "Gradle $Version already installed at $gradleExe"
        return $gradleExe
    }

    Write-Step "Installing Gradle $Version into LocalGPT tools"
    New-Item -ItemType Directory -Force -Path $toolRoot | Out-Null
    $zipPath = Join-Path $env:TEMP "gradle-$Version-bin.zip"
    Invoke-WebRequest -Uri "https://services.gradle.org/distributions/gradle-$Version-bin.zip" -OutFile $zipPath
    Expand-Archive -Path $zipPath -DestinationPath $toolRoot -Force

    if (-not (Test-Path $gradleExe)) {
        throw "Gradle install did not produce $gradleExe"
    }

    return $gradleExe
}

Write-Host "LocalGPT Minecraft mod toolchain"

$javaHome = Get-JavaHome
if ($null -eq $javaHome) {
    Write-Warning "JDK 21 was not found."
    if ($Install) {
        Install-Jdk21
        $javaHome = Get-JavaHome
    }
}

if ($null -ne $javaHome) {
    Write-Step "Java"
    $env:JAVA_HOME = $javaHome
    $env:Path = "$(Join-Path $javaHome "bin");$env:Path"
    & (Join-Path $javaHome "bin\java.exe") -version
}

$gradleExe = Join-Path $env:LOCALAPPDATA "LocalGPT\Tools\gradle-$GradleVersion\bin\gradle.bat"
if (-not (Test-Path $gradleExe)) {
    Write-Warning "Local Gradle $GradleVersion was not found."
    if ($Install -or $InstallGradle) {
        $gradleExe = Install-LocalGradle -Version $GradleVersion
    }
}

if (Test-Path $gradleExe) {
    Write-Step "Gradle"
    & $gradleExe -v
}

if ($InstallEclipse) {
    Install-EclipseJava
}

Write-Step "Minecraft Java mod/plugin/datapack setup checklist"
Write-Host "1. Install Minecraft Java Edition and run the target version once."
Write-Host "2. For Fabric, NeoForge, and Paper, import generated workspaces in Eclipse: File > Import > Gradle > Existing Gradle Project."
Write-Host "3. For datapacks, no Java build is needed. Use the generated .\build-local.ps1 to validate JSON and create a zip."
Write-Host "4. For Java targets, build from the workspace with: .\build-local.ps1"
Write-Host "5. Copy Fabric/NeoForge jars into a matching mods folder, Paper jars into a plugins folder, or datapack zips into a world's datapacks folder."
Write-Host "6. Keep Ollama running so the LocalGPT AI Council can help with setup choices and missing-feature reports."
