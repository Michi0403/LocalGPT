<#
.SYNOPSIS
    Installs Ollama (if missing) and pulls a chosen set of models.
    Installs LocalGPT Windows if wanted
.PARAMETER Range
    One of: Slim, RTX3060, Full. Default is Slim.

.DESCRIPTION
    - Detects if `ollama` is already installed; if not, downloads the Windows installer.
    - Pulls the selected model list via `ollama pull`.
#>

param(
    [ValidateSet('Slim','RTX3060','Full')]
    [string]$Range,
    [switch]$InstallOllama,
    [switch]$PullOllamaModels,
    [switch]$InstallLocalGPTWin,
    [switch]$StartLocalGPT
)
function Remove-IfExists {
    <#
    .SYNOPSIS
        Checks if a file or directory exists and deletes it if the user confirms.

    .PARAMETER Path
        The path to the file or directory.

    .PARAMETER ForceDelete
        If specified, deletes without asking for confirmation.

    .EXAMPLE
        Remove-IfExists -Path ".\MyFolder"

    .EXAMPLE
        Remove-IfExists -Path "C:\Temp\archive.zip" -ForceDelete
    #>
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [switch]$ForceDelete
    )

    if (Test-Path $Path) {
        Write-Host "The path '$Path' already exists." -ForegroundColor Yellow

        $delete = $false

        if ($ForceDelete) {
            $delete = $true
        }
        else {
            $answer = Read-Host "Do you want to delete it? (Y/N)"
            if ($answer -match '^(Y|Yes|Ja|y|1|J|j)$') {
                $delete = $true
            }
        }

        if ($delete) {
            try {
                Remove-Item -Path $Path -Recurse -Force
                Write-Host "'$Path' has been deleted." -ForegroundColor Green
            }
            catch {
                Write-Host "Error deleting '$Path': $_" -ForegroundColor Red
            }
        }
        else {
            Write-Host "Keeping existing '$Path'." -ForegroundColor Cyan
        }
    }
    else {
        Write-Host "'$Path' does not exist." -ForegroundColor Gray
    }
}
function Remove-IfExists {
        param([string]$Path, [switch]$Force)
        if (Test-Path $Path) {
            Write-Host "The folder '$Path' already exists." -ForegroundColor Yellow
            if (-not $Force) {
                $answer = Read-Host "Do you want to delete it? (Y/N)"
                if ($answer -notmatch '^(Y|Yes|Ja|y|1|J|j)$') {
                    $answerSure = Read-Host "Do you want to keep going or abort? Y for keep going, Everything else for abort"
                    if ($answerSure -notmatch '^(Y|Yes|Ja|y|1|J|j)$') {
                        Write-Host "Keeping existing '$Path'. Installation aborted." -ForegroundColor Cyan
                        return $false
                    }
                    else
                    {
                        return $true;
                    }
                
                }
            }
            try {
                Remove-Item -Path $Path -Recurse -Force
                Write-Host "Deleted existing '$Path'." -ForegroundColor Green
            }
            catch {
                Write-Host "Error deleting '$Path': $_" -ForegroundColor Red
                return $false
            }
        }
        return $true
    }
function Expand-ZipRelative {
    param(
        [Parameter(Mandatory)]
        [string]$ZipPath,   # Path to the zip file (can be relative)

        [Parameter(Mandatory)]
        [string]$TargetDir  # Name of the subdirectory to extract into
    )

    try {
        # Resolve the zip path to an absolute path
        $zipFullPath = Resolve-Path $ZipPath -ErrorAction Stop

        # Create the target directory path (relative, one level deeper)
        $destPath = Join-Path -Path (Get-Location) -ChildPath $TargetDir

        # Create the directory if it doesn't exist
        if (-not (Test-Path $destPath)) {
            New-Item -ItemType Directory -Path $destPath | Out-Null
        }

        Write-Host "Extracting '$zipFullPath' to '$destPath'..." -ForegroundColor Cyan

        # Use built-in Expand-Archive (PowerShell 5+)
        Expand-Archive -Path $zipFullPath -DestinationPath $destPath -Force

        Write-Host "Extraction complete." -ForegroundColor Green
    }
    catch {
        Write-Host "Error: $_" -ForegroundColor Red
    }
}


function Get-GitHubLatestSRC {
    param(
        [Parameter(Mandatory)]
        [string]$Repo,   # Format: owner/repository

        [string]$OutFile # Optional: where to save the ZIP
    )

    try {
        # GitHub API endpoint for latest source ZIP
        $apiUrl = "https://api.github.com/repos/$Repo/zipball"

        Write-Host "Fetching latest source for $Repo..." -ForegroundColor Cyan

        # HEAD request to get filename from Content-Disposition
        $response = Invoke-WebRequest -Uri $apiUrl -Headers @{ "User-Agent" = "PowerShell" } -Method Head -ErrorAction Stop

        # Extract filename from headers
        $contentDisp = $response.Headers["Content-Disposition"]
        $fileName = ($contentDisp -replace '.*filename="?([^"]+)"?', '$1')

        if (-not $OutFile) {
            $OutFile = Join-Path $PWD $fileName
        }

        Write-Host "Downloading latest source ZIP..." -ForegroundColor Cyan
        Write-Host "From: $apiUrl"
        Write-Host "To:   $OutFile"

        if (Test-Path $OutFile) {
            Remove-Item $OutFile -Force
        }

        Invoke-WebRequest -Uri $apiUrl -OutFile $OutFile -Headers @{ "User-Agent" = "PowerShell" } -ErrorAction Stop

        Write-Host "Downloaded latest source to: $OutFile" -ForegroundColor Green
    }
    catch {
        Write-Host "Error: $_" -ForegroundColor Red
    }
}

function Get-GitHubLatestRelease {
    param(
        [Parameter(Mandatory)]
        [string]$Repo,   # Format: owner/repository

        [string]$OutFile # Optional: where to save the file
    )

    try {
        # GitHub API URL for latest release
        $apiUrl = "https://api.github.com/repos/$Repo/releases/latest"
        # Determine OS and Architecture
        $osPlatform = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        $architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture

        # Adjust the platform and architecture for the API call
        $platform = switch -Wildcard ($osPlatform) {
            "*Windows*" { "win" }
            "*Linux*"   { "linux" }
            "*Darwin*"  { "macosx" } # MacOS is identified as Darwin
            Default     { "unknown" }
        }
        $arch = switch ($architecture) {
            "X64"  { "x64" }
            "X86"  { "x86" }
            "Arm"  { "arm" }
            "Arm64" { "arm64" }
            Default { "unknown" }
        }
        Write-Host "Architecture is osPlatform $osPlatform architecture $architecture platform $platform arch $arch"
        Write-Host "Fetching latest release info for $Repo..." -ForegroundColor Cyan

        # GitHub API requires a User-Agent header
        $releaseInfo = Invoke-RestMethod -Uri $apiUrl -Headers @{ "User-Agent" = "PowerShell" } -ErrorAction Stop

        Write-Host "Latest version: $($releaseInfo.tag_name)" -ForegroundColor Green

        if (-not $releaseInfo.assets -or $releaseInfo.assets.Count -eq 0) {
            Write-Host "No assets found for this release." -ForegroundColor Yellow
            return
        }

        # Pick the first asset (or modify to choose a specific one)
        $asset = $releaseInfo.assets |
        Where-Object {
            $_.name -match $platform -and
            $_.name -match $arch
        } |
        Select-Object -First 1  # Take the first match

        $downloadUrl = $asset.browser_download_url

        Write-Host "Downloading: $($asset.name) from $downloadUrl to $OutFile" -ForegroundColor Cyan
        Remove-IfExists($OutFile)
        if (-not $OutFile) {
            $OutFile = Join-Path $PWD $asset.name
        }

        Invoke-WebRequest -Uri $downloadUrl -OutFile $OutFile -Headers @{ "User-Agent" = "PowerShell" } -ErrorAction Stop

        Write-Host "Downloaded to: $OutFile" -ForegroundColor Green
    }
    catch {
        Write-Host "Error: $_" -ForegroundColor Red
    }
}

function Install-LocalGPT {
    <#
    .SYNOPSIS
        Installs LocalGPT by unzipping a pre-downloaded ZIP into %AppData%\Local\LocalGPT.

    .DESCRIPTION
        - Checks if LocalGPT folder already exists and asks before deleting.
        - Handles long path issues by extracting to a temp folder first.
        - Falls back to 7-Zip if built-in unzip fails.
        - Works in both Windows PowerShell 5.1 and PowerShell 7+.

    .PARAMETER ZipPath
        Full path to the already downloaded LocalGPT ZIP file.

    .PARAMETER ForceDelete
        If specified, deletes existing LocalGPT folder without asking.

    .EXAMPLE
        Install-LocalGPT -ZipPath "C:\Downloads\LocalGPT.zip"

    .EXAMPLE
        Install-LocalGPT -ZipPath "C:\Downloads\LocalGPT.zip" -ForceDelete
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$ZipPath,

        [switch]$ForceDelete
    )
    Remove-IfExists("LocalGPTByMichi0403.zip",$false)
    Get-GitHubLatestRelease -Repo "Michi0403/LocalGPT" -OutFile "LocalGPTByMichi0403.zip"
    $TargetPath = Join-Path $env:LOCALAPPDATA "LocalGPT"

    

    # Step 1: Check ZIP exists
    if (-not (Test-Path $ZipPath)) {
        Write-Host "ERROR: ZIP file not found at '$ZipPath'" -ForegroundColor Red
        return
    }

    # Step 2: Remove old installation if needed
    if (-not (Remove-IfExists -Path $TargetPath -Force:$ForceDelete)) {
        return
    }

    # Step 3: Extract ZIP
    Write-Host "Extracting '$ZipPath' to Final folder...$TargetPath" -ForegroundColor Cyan
    try {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipPath, $TargetPath)
        #Expand-Archive -Path $ZipPath -DestinationPath $TempExtractPath -Force
    }
    catch {
        Write-Host "Built-in unzip failed (possibly due to long paths)." -ForegroundColor Yellow
        $sevenZip = "${env:ProgramFiles}\7-Zip\7z.exe"
        if (Test-Path $sevenZip) {
             Write-Host "trying .$sevenZip x $ZipPath "-o$TargetPath" -y"
            .$sevenZip x $ZipPath "-o$TargetPath" -y
            if ($LASTEXITCODE -ne 0) {
                throw "7-Zip extraction failed with exit code $LASTEXITCODE"
            }
            #& $sevenZip x $ZipPath -o" $TargetPath" -y
        }
        else {
            Write-Host "7-Zip not found. Please install it or enable long path support." -ForegroundColor Red
            return
        }
    }
    Write-Host "✅ LocalGPT installed successfully to '$TargetPath'" -ForegroundColor Green
}

function Install-Ollama {
    $installPath = "$env:ProgramFiles\Ollama"
    if (Test-Path "$installPath\ollama.exe") { return }

    Write-Host "Downloading Ollama installer..."
    $url = 'https://ollama.com/install.ps1'
    $officialOllamaInstallpshscript = Join-Path -Path $env:TEMP -ChildPath 'officialOllamaInstallpshscript.ps1'
    Remove-IfExists($officialOllamaInstallpshscript)
    Invoke-WebRequest -Uri $url -OutFile $officialOllamaInstallpshscript
    #Executing official powershell script
    powershell  "$officialOllamaInstallpshscript"

    #Remove-Item $zip
    Write-Host "Ollama should be installed. Better check I just checked the script for semantic errors"
}

function Pull-LocalGPTByMichi0403 {
  Install-LocalGPT("LocalGPTByMichi0403.zip")
}

function Pull-GitsToLearningBaseImporter {    
param(
        [string]$inputLearnBase,   # Format: owner/repository
        [bool]$forceDeleteSwitch = $true
    )
    $input = $inputLearnBase
    Write-Host "Pulling Git $input" -ForegroundColor Yellow
    Write-Host "Starting Learning Base Example installation." -ForegroundColor Yellow
 
    if ([string]::IsNullOrEmpty($input)) 
    {
        $input = Read-Host "Enter command (type 'exit' to quit)"
    }

    Write-Host "If you leave the string Empty and press Enter you pull LocalGPT's Repo for selfawareness teachings." -ForegroundColor Yellow
    Write-Host "The downloaded repo will gets places in the default learningbase Path and extracted there to a sanitized subfoldername." -ForegroundColor Yellow
    if ([string]::IsNullOrEmpty($input) -or $input -eq "0") {
        $input = "Michi0403/LocalGPT"
        Write-Host "Input $input was empty set default Michi0403/LocalGPT"
        Write-Host "Also recommended TelegramBots/Telegram.Bot by https://github.com/wiz0u to train generate telegram bots or and control them"
        Write-Host "Also recommended Michi0403/TacosPortalOpen Dx WebApi Including WASM Client and Server + backend structure adaptable to microservices"
        Write-Host "Also recommended Michi0403/OpenMorph.NET To train 3D Open Scad generation"
        Write-Host "Michi0403/AutomatedDiscordLogin"
        Write-Host "Michi0403/3DOpenScad To train 3D Open Scad generation"
        Write-Host "ollama/ollama by Ollama To help it get it along with host and protocol"
        Write-Host "MicrosoftDocs/PowerShell-Docs by Microsoft"
        Write-Host "MicrosoftDocs/learn by Microsoft"
        Write-Host "dotnet/docs by Dotnet Microsoft"
        Write-Host "MicrosoftDocs/windows-dev-docs by Microsoft"
        Write-Host "MicrosoftDocs/microsoftgraph-docs-powershell"
        Write-Host "MicrosoftDocs/windowsserverdocs by Microsoft"
    }
    Write-Host "You typed: $input"
    $cleanforDirectory = $input -replace '[\\\/:\*\?"<>\|]', '_'
    $ZipPath = Join-Path "C:\learnbaseforlocalgpt" $cleanforDirectory
    $TargetPath = Join-Path "C:\learnbaseforlocalgpt" $cleanforDirectory
    $ZipWithEnding = "$ZipPath.zip"
    Get-GitHubLatestSRC -Repo $input -OutFile $ZipWithEnding
     # Step 2: Remove old installation if needed
    if (-not (Remove-IfExists -Path $TargetPath -Force:$forceDeleteSwitch)) {
        return
    }
     # Step 3: Extract ZIP
    Write-Host "Extracting '$ZipWithEnding' to learnbase importer default folder (for now)...$TargetPath" -ForegroundColor Cyan
    try {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipWithEnding, $TargetPath)
        #Expand-Archive -Path $ZipPath -DestinationPath $TempExtractPath -Force
    }
    catch {
        Write-Host "Built-in unzip failed (possibly due to long paths)." -ForegroundColor Yellow
        $sevenZip = "${env:ProgramFiles}\7-Zip\7z.exe"
        if (Test-Path $sevenZip) {
             Write-Host "trying .$sevenZip x $ZipWithEnding "-o$TargetPath" -y"
            .$sevenZip x $ZipWithEnding "-o$TargetPath" -y
            if ($LASTEXITCODE -ne 0) {
                throw "7-Zip extraction failed with exit code $LASTEXITCODE"
            }
            #& $sevenZip x $ZipPath -o" $TargetPath" -y
        }
        else {
            Write-Host "7-Zip not found. Please install it or enable long path support." -ForegroundColor Red
            return
        }
    }
    Remove-IfExists($cleanforDirectory,$false)
    
}

function Pull-Models {
    param([string[]]$ModelList)
    foreach ($model in $ModelList) {
        if ($Host.Name -eq 'Windows PowerShell ISE Host'){
            Start-Process powershell -ArgumentList "-Command", "ollama pull $model"
        }
        else
        {
            Write-Host "=== Pulling $model ===" -ForegroundColor Cyan
            powershell ollama pull $model | Write-Host
        }
    }
}

# Define model sets
$slim = @(
  "gpt-oss:20b",
  "gemma3:27b",
  "deepseek-r1:8b",
  "qwen3-coder:30b",
  "llama2-uncensored:7b"
)

$rtx3060 = @(
  "qwen3.5:0.8b","qwen3.5:2b","qwen3.5:4b","qwen3.5:9b",
  "gpt-oss:20b",
  "llama3.1:8b","llama3.2:1b","llama3.2:3b",
  "gemma3:4b","gemma3:12b",
  "qwen3:1.7b","qwen3:4b","qwen3:8b","qwen3:14b",
  "phi3:3.8b","phi3:14b",
  "deepseek-coder:6.7b",
  "dolphin3:8b",
  "codegemma:2b","codegemma:7b",
  "gemma4:e2b","gemma4:e4b","gemma4:12b",
  "llama3:8b","llama3.2-vision:11b",
  "llama2:7b","llama2:13b","llama2-uncensored:7b",
  "llama-guard3:1b","llama-guard3:8b",
  "deepseek-ocr:3b",
  "deepseek-r1:1.5b","deepseek-r1:7b","deepseek-r1:8b",
  "deepseek-r1:14b",
  "deepseek-coder-v2:16b","deepseek-v2:16b",
  "deepscaler:1.5b",
  "openthinker:7b"
)

$full = @(
 "qwen3.5:0.8b",
  "qwen3.5:2b",
  "qwen3.5:4b",
  "qwen3.5:9b",
  "qwen3.5:27b",
  "qwen3.5:35b",
  "gpt-oss:20b",
  "llama3.1:8b",
  "llama3.2:1b",
  "llama3.2:3b",
  "gemma3:4b",
  "gemma3:12b",
  "gemma3:27b",
  "qwen3:1.7b",
  "qwen3:4b",
  "qwen3:8b",
  "qwen3:14b",
  "qwen3:30b",
  "qwen3:32b",
  "phi3:3.8b",
  "phi3:14b",
  "deepseek-coder:6.7b",
  "deepseek-coder:33b",
  "dolphin3:8b",
  "codegemma:2b",
  "codegemma:7b",
  "laguna-xs.2:nvfp4",
  "laguna-xs.2:q4_K_M",
  "qwen3.6:27b",
  "qwen3.6:35b",
  "gemma4:e2b",
  "gemma4:e4b",
  "gemma4:12b",
  "gemma4:26b",
  "gemma4:31b",
  "llama3:8b",
  "llama3.2-vision:11b",
  "llama2:7b",
  "llama2:13b",
  "llama2-uncensored:7b",
  "llama-guard3:1b",
  "llama-guard3:8b",
  "deepseek-ocr:3b",
  "deepseek-r1:1.5b",
  "deepseek-r1:7b",
  "deepseek-r1:8b",
  "deepseek-r1:14b",
  "deepseek-r1:32b",
  "deepseek-coder-v2:16b",
  "deepseek-v2:16b",
  "deepscaler:1.5b",
  "openthinker:7b",
  "qwen3-coder:30b",
  "openthinker:32b"
)
if($PSVersionTable.PSVersion -lt '5.0'){
   Add-Type -Path ([Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem")).Location;
}else{
   Add-Type -AssemblyName System.IO.Compression.FileSystem
}
Set-ExecutionPolicy Bypass -Scope Process
# Ask the user if they want to install Ollama
$answer = Read-Host "Do you want to install Ollama? (Y/N)"
if ($answer -match '^(Y|Yes|Ja|y|1|J|j)$') {
    Install-Ollama
} else {
    Write-Host "Skipping Ollama installation." -ForegroundColor Yellow
}
# Ask the user if they want to pull models?
$answer = Read-Host "Do you want to Pull Ollama Models? (Y/N)"
if ($answer -match '^(Y|Yes|Ja|y|1|J|j)$') {
    $Range = Read-Host "Select model range (slim / rtx3060 / full)"
    
    # Default Ollama install path (adjust if different)
    $ollamaDefaultPath = "C:\Program Files\Ollama"

    # Check if 'ollama' is in PATH
    $ollamaInPath = $false
    foreach ($dir in $env:PATH -split ";"| Where-Object { $_ -and $_.Trim() -ne "" }) {
        if (Test-Path (Join-Path $dir "ollama.exe")) {
            $ollamaInPath = $true
            break
        }
    }

    # If not in PATH, add it for the current user
    if (-not $ollamaInPath) {
        if (Test-Path (Join-Path $ollamaDefaultPath "ollama.exe")) {
            Write-Host "Ollama not found in PATH. Adding..."
            $currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
            if (-not $currentPath) { $currentPath = "" }
            $newPath = $currentPath + ";" + $ollamaDefaultPath
            [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
            $env:PATH = $newPath  # Update current session
            Write-Host "Ollama path added: $ollamaDefaultPath"
        }
        else {
            Write-Error "Ollama not found at $ollamaDefaultPath. Please install it first."
            exit 1
        }
    }

    # Start Ollama server in background
    Start-Process -FilePath "ollama" -ArgumentList "serve" -NoNewWindow
    Start-Sleep -Seconds 3  # Wait for server to start

    switch ($Range) {
    'slim'     { Pull-Models -ModelList $slim }
    'rtx3060'  { Pull-Models -ModelList $rtx3060 }
    'full'     { Pull-Models -ModelList $full }
    }
} else {
    Write-Host "Skipping Ollama Model pulling." -ForegroundColor Yellow
}

# Ask the user if they want to install Ollama
$answer = Read-Host "Do you want to install LocalGPTWin by Michi0403 ? (Y/N)"
if ($answer -match '^(Y|Yes|Ja|y|1|J|j)$') {
    Pull-LocalGPTByMichi0403
} else {
    Write-Host "Skipping LocalGPTWin installation." -ForegroundColor Yellow
}
# Ask the user if they want to install Ollama
$answer = Read-Host "Do you want to setup default learning base (hardcoded path now due to the accident) for the learning base importer C:\learnbaseforlocalgpt for LocalGPTWin by Michi0403 ? (Y/N)"
if ($answer -match '^(Y|Yes|Ja|y|1|J|j)$') {
    $answerGitDefaultListImport = Read-Host "Do you want import recommended Gits ? (Y/N)"
    if ($answerGitDefaultListImport -match '^(Y|Yes|Ja|y|1|J|j)$')
    {
        try
        {
            Pull-GitsToLearningBaseImporter "Michi0403/LocalGPT"
            Pull-GitsToLearningBaseImporter "TelegramBots/Telegram.Bot"
            Pull-GitsToLearningBaseImporter "Michi0403/TacosPortalOpen"
            Pull-GitsToLearningBaseImporter "Michi0403/OpenMorph.NET"
            Pull-GitsToLearningBaseImporter "Michi0403/AutomatedDiscordLogin"
            Pull-GitsToLearningBaseImporter "Michi0403/3DOpenScad"
            #Pull-GitsToLearningBaseImporter "MicrosoftDocs/learn"
            Pull-GitsToLearningBaseImporter "dotnet/docs"
            Pull-GitsToLearningBaseImporter "MicrosoftDocs/windows-dev-docs"
            Pull-GitsToLearningBaseImporter "MicrosoftDocs/microsoftgraph-docs-powershell"
            Pull-GitsToLearningBaseImporter "MicrosoftDocs/windows-dev-docs"
            Pull-GitsToLearningBaseImporter "Mojang/bedrock-samples"
            Pull-GitsToLearningBaseImporter "Mojang/bedrock-protocol-docs"
            Pull-GitsToLearningBaseImporter "Mojang/minecraft-editor"
            Pull-GitsToLearningBaseImporter "Mojang/minecraft-debugger"
            Pull-GitsToLearningBaseImporter "Mojang/minecraft-editor-extension-starter-kit"
            Pull-GitsToLearningBaseImporter "Mojang/minecraft-editor-extension-samples"
            Pull-GitsToLearningBaseImporter "Mojang/minecraft-scripting-libraries"
            Pull-GitsToLearningBaseImporter "Mojang/minecraft-creator-tools"
            Pull-GitsToLearningBaseImporter "Mojang/bedrock-schemas"
            Pull-GitsToLearningBaseImporter "DevExpress/Blazor"
            Pull-GitsToLearningBaseImporter "DevExpress/DevExtreme"
            Pull-GitsToLearningBaseImporter "DevExpress/devextreme-documentation"
            Pull-GitsToLearningBaseImporter "DevExpress-Examples/XAF_Security_E4908"
            Write-Host "Remember to still teach it to the AI Council and import it via the learnbase importer" -ForegroundColor Yellow
        }
        catch
        {

        }
         
    }
    else
    {
        Pull-GitsToLearningBaseImporter("");
    }
    
} else {
    Write-Host "Skipping Learning Base Example installation." -ForegroundColor Yellow
}



# Ask the user if they want to install Ollama
$answer = Read-Host "Do you want to start LocalGPTWin by Michi0403 ? (Y/N)"
if ($answer -match '^(Y|Yes|Ja|y|1|J|j)$') {
    Write-Host "`nStarting LocalGPT now."
     $LocalGptExe = Join-Path $env:LOCALAPPDATA "LocalGPT\LocalGPT.exe"
    Start-Process powershell -ArgumentList "-Command", ".\Start‑LocalGPT.ps1 $LocalGPTexepath"
    #powershell -NoExit -File .\Start‑LocalGPT.ps1 $LocalGPTexepath 
} else {
    Write-Host "Skipping Start LocalGPT." -ForegroundColor Yellow
}
exit