param(
    [string]$OutputRoot = "./publish-output",
    [string]$AddBrain = "y", 
    [string]$answerPushWildlyToGithub = "y", 
    [string]$inputversion = "0.0.8-alpha1" 

)
function New-DevMicroserviceCertificate {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [string[]]$DnsNames = @("localhost"),

        [string]$Password = "3xtremelytycruelSaaS4bus4l!",

        [int]$ValidYears = 3,

        [switch]$TrustLocally,

        [ValidateSet("CurrentUser", "LocalMachine")]
        [string]$CertStoreLocation = "CurrentUser",

        [switch]$Force
    )
    try
    {
        $ErrorActionPreference = "Stop"

    if (-not (Test-Path $OutputRoot)) {
        New-Item -ItemType Directory -Path $OutputRoot | Out-Null
    }

    $storePath = "Cert:\$CertStoreLocation\My"
    $subject = "CN=$Name"

    $existingCert = Get-ChildItem $storePath |
        Where-Object {
            $_.Subject -eq $subject -and
            $_.NotAfter -gt (Get-Date) -and
            $_.HasPrivateKey
        } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if ($existingCert -and -not $Force) {
        $safeName = $Name -replace '[^a-zA-Z0-9\-_]', '_'
        $pfxPath = Join-Path $OutputRoot "$safeName.pfx"
        $cerPath = Join-Path $OutputRoot "$safeName.cer"

        [PSCustomObject]@{
            Name              = $Name
            Subject           = $existingCert.Subject
            Thumbprint        = $existingCert.Thumbprint
            DnsNames          = $DnsNames
            StoreLocation     = $storePath
            PfxPath           = $pfxPath
            CerPath           = $cerPath
            TrustedLocally    = $false
            Password          = $Password
            ValidUntil        = $existingCert.NotAfter
            ReusedExisting    = $true
        }

        return
    }

    $securePassword = ConvertTo-SecureString `
        -String $Password `
        -Force `
        -AsPlainText

    # Enhanced Key Usages:
    # 1.3.6.1.5.5.7.3.1 = Server Authentication
    # 1.3.6.1.5.5.7.3.2 = Client Authentication
    # 1.3.6.1.5.5.7.3.3 = Code Signing
    $ekuExtension = @(
        "2.5.29.37={text}1.3.6.1.5.5.7.3.1,1.3.6.1.5.5.7.3.2,1.3.6.1.5.5.7.3.3"
    )

    $cert = New-SelfSignedCertificate `
        -Subject $subject `
        -DnsName $DnsNames `
        -CertStoreLocation $storePath `
        -KeyAlgorithm RSA `
        -KeyLength 3072 `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -KeySpec Signature `
        -NotAfter (Get-Date).AddYears($ValidYears) `
        -TextExtension $ekuExtension `
        -FriendlyName $Name

    $safeName = $Name -replace '[^a-zA-Z0-9\-_]', '_'

    $pfxPath = Join-Path $OutputRoot "$safeName.pfx"
    $cerPath = Join-Path $OutputRoot "$safeName.cer"

    Export-PfxCertificate `
        -Cert $cert `
        -FilePath $pfxPath `
        -Password $securePassword | Out-Null

    Export-Certificate `
        -Cert $cert `
        -FilePath $cerPath | Out-Null

    if ($TrustLocally) {
        $rootStore = "Cert:\$CertStoreLocation\Root"

        Import-Certificate `
            -FilePath $cerPath `
            -CertStoreLocation $rootStore | Out-Null
    }

    [PSCustomObject]@{
        Name              = $Name
        Subject           = $cert.Subject
        Thumbprint        = $cert.Thumbprint
        DnsNames          = $DnsNames
        StoreLocation     = $storePath
        PfxPath           = $pfxPath
        CerPath           = $cerPath
        TrustedLocally    = [bool]$TrustLocally
        Password          = $Password
        ValidUntil        = $cert.NotAfter
        ReusedExisting    = $false
        EnhancedKeyUsages = @(
            "Server Authentication",
            "Client Authentication",
            "Code Signing"
        )
    }
    }
    catch
    {

    }

}
function Assert-ReleaseVersionIsNewerThanPublished {
    param([string]$ReleaseVersion)

    $candidate = ConvertTo-ReleaseVersionParts $ReleaseVersion
    $published = @()

    $localTags = & git -C $repoRoot tag --list "v*" 2>$null
    foreach ($tag in $localTags) {
        try {
            $published += ConvertTo-ReleaseVersionParts $tag
        }
        catch {
            Write-Warning "Ignoring non-semantic local tag $tag"
        }
    }

    if ($CreateGitHubRelease) {
        $ghCommand = Resolve-GitHubCli
        if ([string]::IsNullOrWhiteSpace($ghCommand)) {
            throw "GitHub CLI 'gh' was not found. Cannot prove release version ordering."
        }

        $releaseLines = & $ghCommand release list --limit 100 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw "Could not list GitHub releases. Cannot prove release version ordering."
        }

        foreach ($line in $releaseLines) {
            $match = [regex]::Match($line, "v(?<version>\d+\.\d+\.\d+[^\s]*)")
            if ($match.Success) {
                try {
                    $published += ConvertTo-ReleaseVersionParts $match.Groups["version"].Value
                }
                catch {
                    Write-Warning "Ignoring non-semantic GitHub release entry: $line"
                }
            }
        }
    }

    $highest = $null
    foreach ($publishedVersion in $published) {
        if ($null -eq $highest -or (Compare-ReleaseVersionParts $publishedVersion $highest) -gt 0) {
            $highest = $publishedVersion
        }
    }

    if ($null -ne $highest -and (Compare-ReleaseVersionParts $candidate $highest) -le 0) {
        throw "Release version $ReleaseVersion must be higher than existing public release $($highest.Original)."
    }

    if ($null -ne $highest) {
        Write-Host "Release version $ReleaseVersion is higher than existing release $($highest.Original)."
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
function Assert-PublicReleasePayloadArtifacts {
    $expectedFiles = @(
        "./publish-output/linux64.zip",
        "./publish-output/linuxarm64.zip",
        "./publish-output/maxosx64.zip",
        "./publish-output/maxosx64arm.zip",
        "./publish-output/winarm64.zip",
        "./publish-output/winx64.zip",
        "LICENSE.MD",
        "README.md",
        "SECURITY.md",
        "images.zip",
        "localgpt-memory.db-shm",
        "localgpt-memory.db-wal",
        "localgpt-memory.db"
    )

    $missing = $expectedFiles |
        Where-Object { -not (Test-Path (Join-Path $releaseRoot $_)) }

    if ($missing.Count -gt 0) {
        throw "Public GitHub release payload files are missing: $($missing -join ', ')."
    }
}

function Invoke-CheckedNative {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function PublishThingsToGithub {
    if ([string]::IsNullOrEmpty($inputversion)) 
    {
        $inputversion = Read-Host "inputversion for github release tag"
    }
    # Release Notes erzeugen
    $notesFile = Join-Path $OutputRoot "RELEASE_NOTES.txt"
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "Automated publish on $timestamp" | Out-File $notesFile -Encoding utf8 -Force

    # GitHub CLI finden
    $releaseRoot = Join-Path "..\" $OutputRoot
    $ghCommand = Resolve-GitHubCli
    if ([string]::IsNullOrWhiteSpace($ghCommand)) {
        throw "GitHub CLI 'gh' was not found. Install it or upload the zip files from $releaseRoot manually."
    }

    # Tag
    $tag = "v$inputversion"

    # Assets sammeln
    $assets = @()
    $assets += (Get-ChildItem $OutputRoot -Filter "*.zip").FullName
    $assets += (Get-ChildItem $OutputRoot -Filter "*.ps1").FullName
    $assets += "LICENSE.md"
    $assets += "SECURITY.md"
    $assets += "README.md"

    # Argumente korrekt aufbauen
    $ghArgs = @(
        "release", "create", $tag,
        "--latest",
        "--title", ,#"--title LocalGPT $Version"
        "LocalGPT $inputversion",
        "--notes-file", $notesFile
    )

    # Assets anhängen (korrekte Position!)
    $ghArgs += $assets

    # Debug-Ausgabe
    Write-Host "`n--- GH ARGUMENTS ---" -ForegroundColor Cyan
    $ghArgs | ForEach-Object { Write-Host $_ }
    Write-Host "----------------------`n" -ForegroundColor Cyan

    # Ausführen
    Invoke-CheckedNative $ghCommand $ghArgs
}
Set-ExecutionPolicy Bypass -Scope Process
New-DevMicroserviceCertificate `
    -Name "localgptbymichi0403.local" `
    -DnsNames @("localhost", "localgptbymichi0403.local","127.0.0.1") `
    -TrustLocally
# -----------------------------
# 1. Liste der Publish-Profile
# -----------------------------
$profiles = @(
    # linux64
    @{
        Name = "linux64"
        Path = "../LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/linux64.pubxml"
    }

    # linuxarm64
    @{
        Name = "linuxarm64"
        Path = "../LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/linuxarm64.pubxml"
    }

    # maxosx64
    @{
        Name = "maxosx64"
        Path = "../LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/maxosx64.pubxml"
    }

    # maxosx64arm
    @{
        Name = "maxosx64arm"
        Path = "../LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/maxosx64arm.pubxml"
    }

    # winarm64
    @{
        Name = "winarm64"
        Path = "../LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/winarm64.pubxml"
    }

    # linuxarm64
    @{
        Name = "winx64"
        Path = "../LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/x64.pubxml"
    }
)

# -----------------------------
# 2. Start
# -----------------------------
Write-Host "Starting multi-profile publish..." -ForegroundColor Cyan

foreach ($profile in $profiles) {

    $name = $profile.Name
    $profilePath = $profile.Path

    if (-not (Test-Path $profilePath)) {
        Write-Host "❌ Profile not found: $profilePath" -ForegroundColor Red
        continue
    }

    # Zielordner pro Profil
    $targetDir = Join-Path $OutputRoot $name

    if ($Overwrite -and (Test-Path $targetDir)) {
        Remove-Item -Recurse -Force $targetDir
    }
    $projectPath = "../LocalGPTWebviewWrapper/LocalGPT/LocalGPT.csproj"
    # dotnet publish Befehl
    $cmd = "dotnet publish $projectPath -p:PublishProfileFullPath=$profilePath -o:$targetDir"


    Write-Host "📦 Publishing $name..." -ForegroundColor Yellow
    Write-Host "→ $($cmd -join ' ')" -ForegroundColor DarkGray
   
    # Ausführen
    $result = powershell $cmd

    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed: $name" -ForegroundColor Red
        continue
    }

    Write-Host "✅ Done: $name → $targetDir" -ForegroundColor Green
    $targetZip = Join-Path $OutputRoot "$name.zip"
    Compress-Archive -Path "$targetDir\*" -DestinationPath $targetZip -Force
    Write-Host "✅ Done ZIP: $name → $targetZip" -ForegroundColor Green
}

Write-Host "🎉 All profiles processed." -ForegroundColor Cyan
if ($AddBrain -notmatch '^(Y|y|1|J|j)$') 
{
    $AddBrain = Read-Host "Do you want to add brain to Release?"
}
if ($AddBrain -match '^(Y|y|1|J|j)$') {
   
    try
    {
        $dbfile = Join-Path $env:LOCALAPPDATA "LocalGPT\localgpt-memory.db"
        $destinationPathDbFile  = Join-Path $OutputRoot "localgpt-memory.db"
        Copy-Item -Path $dbfile -Destination $destinationPathDbFile
    }
    catch
    {
        Write-Host "Copyerror for copying  $dbfile"
    }
    try
    {
        $dbfileshm = Join-Path $env:LOCALAPPDATA "LocalGPT\localgpt-memory.db-shm"
        $destinationPathDbFile  = Join-Path $OutputRoot "localgpt-memory.db-shm"
        Copy-Item -Path $dbfileshm -Destination $destinationPathDbFile
    }
    catch
    {
         Write-Host "Copyerror for copying  $dbfileshm"
    }
    try
    {
        $dbfilewal = Join-Path $env:LOCALAPPDATA "LocalGPT\localgpt-memory.db-wal"
        $destinationPathDbFile  = Join-Path $OutputRoot "localgpt-memory.db-wal"
        Copy-Item -Path $dbfilewal -Destination $destinationPathDbFile
    }
    catch
    {
          Write-Host "Copyerror for copying  $dbfilewal"
    }
    
    
     Write-Host "✅ Done Copying : $dbfile $dbfileshm $dbfilewal → $OutputRoot" -ForegroundColor Green
    } 
    else 
    {
    Write-Host "No Brain in Release" -ForegroundColor Yellow
}
try 
{
    Copy-Item ".\*.ps1" "$OutputRoot"
    try
    {
        $cert = Get-ChildItem Cert:\CurrentUser\My |
        Where-Object {
            $_.Subject -eq "CN=$localgptbymichi0403.local" -and
            $_.HasPrivateKey -and
            $_.NotAfter -gt (Get-Date) -and
            $_.EnhancedKeyUsageList.FriendlyName -contains "Code Signing"
        } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

        if (-not $cert) {
            throw "No valid code-signing certificate found for CN=$certName"
        }

        Get-ChildItem -Path $OutputRoot -Recurse -Include *.ps1, *.psm1, *.psd1, *.ps1xml |
            ForEach-Object {
            Write-Host "Signing $($_.FullName)"

            $signature = Set-AuthenticodeSignature `
                -FilePath $_.FullName `
                -Certificate $cert `
                -TimestampServer "http://timestamp.digicert.com"

            if ($signature.Status -ne "Valid") {
                Write-Warning "Signing may have failed for $($_.FullName): $($signature.Status)"
            }
        }
    }
    catch
    {
    
    }
}
catch
{
     Write-Host "Copyerror for copying the powershell skripts"
}
try 
{
    try
    {
        Copy-Item "..\README.md" "$OutputRoot\README.md"
    }
    catch
    {
        Write-Host "Copy Error Reame..."
    }
    try
    {
        Copy-Item "..\SECURITY.md" "$OutputRoot\SECURITY.md"
    }
    catch
    {
        Write-Host "Copy Error Security..."
    }
    try
    {
        Copy-Item "..\LICENSE.MD" "$OutputRoot\LICENSE.MD"
    }
    catch
    {
        Write-Host "Copy Error License..."
    }
    try
    {
        $targetZip = Join-Path $OutputRoot "images.zip"
        Write-Host "Resolved path:" (Resolve-Path "..\images\*.png")
        Write-Host "Resolved destpathzip: $targetZip"
        Compress-Archive -Path "..\images\*.png" -DestinationPath $targetZip -Force
    }
    catch
    {
        Write-Host "Zip Error Images..."
    }
    
}
catch
{
     Write-Host "Copyerror for Readme License Security..."
}
#
if ($answerPushWildlyToGithub -notmatch '^(Y|y|1|J|j)$') 
{
    $answerPushWildlyToGithub = Read-Host "Publish things blind drastically? Seriously"
}
try
{
    if ($answerPushWildlyToGithub -match '^(Y|Ja|Yes|y|1|J|j)$') 
    {
        PublishThingsToGithub
    } 
    else 
    {
        Write-Host "Ok good" -ForegroundColor Yellow

    }
}
catch
{

}