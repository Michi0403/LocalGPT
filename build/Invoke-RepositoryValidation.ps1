param(
    [string]$Solution = 'LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln',
    [ValidateSet('Debug', 'Release', 'Both')]
    [string]$Configuration = 'Both',
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RepositoryValidation.Common.ps1')

$repoRoot = Get-LocalGptRepositoryRoot
Push-Location $repoRoot
try {
    & ./build/Assert-JavaScriptDiagnostics.ps1
    & ./build/Assert-LocalizationIntegrity.ps1
    & ./build/Assert-OperationalDiagnostics.ps1
    & ./build/Assert-InteractiveServerRenderModes.ps1
    & ./build/Assert-AsyncContinuationPolicy.ps1
    & ./build/Assert-MethodDiagnostics.ps1
    & ./build/Assert-ApplicationStaticPolicy.ps1
    & ./build/Assert-TextServiceOwnership.ps1
    & ./build/Assert-RuntimeValueOwnership.ps1
    & ./build/Assert-IteratorExceptionPolicy.ps1
    & ./build/Assert-GitSourceVisibility.ps1
    & ./build/Assert-ProjectClosure.ps1
    & ./build/Assert-CSharpSyntax.ps1
    & ./build/Assert-ComponentSafety.ps1
    & ./build/Assert-ServiceArchitecture.ps1
    & ./build/Assert-WorkflowContracts.ps1
    & ./build/Assert-HumanCollaboration.ps1
    & ./build/Assert-ArchitectureTasks.ps1
    & ./build/Assert-EfSnapshotArchitecture.ps1
    & ./build/Assert-DatabaseMigrationBootstrap.ps1
    & ./build/Assert-ThemeArchitecture.ps1

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        throw 'A real compiler build is mandatory. dotnet was not found, so no validation stamp or verified package can be produced.'
    }

    $solutionPath = Join-Path $repoRoot $Solution
    if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
        throw "Solution not found: $Solution"
    }

    $logRoot = Join-Path $repoRoot 'artifacts/validation'
    New-Item -ItemType Directory -Path $logRoot -Force | Out-Null

    if (-not $NoRestore) {
        & dotnet restore $solutionPath 2>&1 | Tee-Object -FilePath (Join-Path $logRoot 'restore.log')
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore failed with exit code $LASTEXITCODE."
        }
    }

    $configurations = if ($Configuration -eq 'Both') { @('Debug', 'Release') } else { @($Configuration) }
    foreach ($item in $configurations) {
        $arguments = @('build', $solutionPath, '-c', $item, '--no-restore', '-p:ContinuousIntegrationBuild=true')
        & dotnet @arguments 2>&1 | Tee-Object -FilePath (Join-Path $logRoot ("build-{0}.log" -f $item.ToLowerInvariant()))
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build -c $item failed with exit code $LASTEXITCODE."
        }
    }

    $stamp = [ordered]@{
        SchemaVersion = 1
        Succeeded = $true
        SourceFingerprint = Get-RepositorySourceFingerprint -RepositoryRoot $repoRoot
        DotNetSdk = (& dotnet --version).Trim()
        Configurations = $configurations
        ValidatedAtUtc = [DateTime]::UtcNow.ToString('O')
        Solution = $Solution.Replace('\', '/')
    }
    $stampPath = Join-Path $logRoot 'compile-success.json'
    $stamp | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $stampPath -Encoding utf8
    Write-Host "Repository validation passed. Build stamp: $stampPath"
}
finally {
    Pop-Location
}
