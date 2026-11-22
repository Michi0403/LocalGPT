$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifact = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/ArtifactBuildExecutor.cs'))
$arguments = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/ProjectBuildArgumentTemplate.cs'))
$review = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/ProjectMaintenanceService.BuildReview.cs'))
$generation = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CodeGenerationWorkflowService.Execution.cs'))
$interface = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Interfaces/IArtifactBuildExecutor.cs'))
$toolchainFunctions = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/ToolchainRuntimeDxAiFunctions.cs'))
$maintenanceFunctions = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/ProjectMaintenanceDxAiFunctions.cs'))
$findings = [System.Collections.Generic.List[string]]::new()
$requirements = @(
    @{ Name = 'approved bounded builds'; Text = $artifact; Tokens = @('if (!userConfirmed)', 'IsInsideRoot(target, root)', 'IsInsideRoot(normalizedOutput, root)', 'requestedTimeout', 'KillProcessTree(process)') },
    @{ Name = 'persisted workspace and compiler resolution'; Text = $artifact; Tokens = @('GetCompilerInstallationsAsync', 'GetWorkspaceRootsAsync(projectId, cancellationToken)', 'item.LastValidationSucceeded', 'matchingWorkspace?.PreferredCompilerInstallationId', 'matchingWorkspace?.SolutionPattern', 'runtimePolicy.RegexTimeout', 'item.IsDefaultForLanguage') },
    @{ Name = 'language-neutral executable and arguments'; Text = $artifact; Tokens = @('FileName = compiler.ExecutablePath', 'matchingWorkspace?.BuildArguments', 'argumentTemplate.Expand', 'platform.PrepareToolchainProcessInvocation', 'BuildArgumentsMissing', 'CompilerNotSelected', 'IsDotNetCompiler(compiler) && dotNetTarget') },
    @{ Name = 'common argument interpolation'; Text = $arguments; Tokens = @('"target"', '"targetDirectory"', '"configuration"', '"outputDirectory"', 'QuoteArgument', 'StringComparison.OrdinalIgnoreCase') },
    @{ Name = 'project maintenance uses shared arguments'; Text = $review; Tokens = @('argumentTemplate.Expand(workspace.BuildArguments.Trim()', 'RunProcessAsync(compiler.ExecutablePath', 'targetPattern.IsMatch(item.ProjectRelativePath', 'CaptureTrackedSourceStateAsync(trackedFiles') },
    @{ Name = 'reviewed source only, project-bound selection'; Text = $generation; Tokens = @('GetWorkspaceRootsAsync(entity.ProjectId', 'reviewedSources.Where', 'configuredRoot.SolutionPattern', 'projectId: entity.ProjectId') },
    @{ Name = 'optional project binding preserves old call sites'; Text = $interface; Tokens = @('Guid? projectId = null') },
    @{ Name = 'Council dynamic approved toolchain profile capability'; Text = $toolchainFunctions; Tokens = @('"toolchain.profile.execute"', 'RequiresHumanConfirmation: true', 'SupportsDeferredApprovalRequest: true') },
    @{ Name = 'Council dynamic revision compiler capability'; Text = $maintenanceFunctions; Tokens = @('"project.revision.build.verify"', 'RequiresHumanConfirmation: true') }
)
foreach ($requirement in $requirements) {
    foreach ($token in $requirement.Tokens) {
        if (-not $requirement.Text.Contains($token)) {
            $findings.Add("$($requirement.Name): missing $token")
        }
    }
}
if ($artifact.Contains('Only .sln, .slnx and .csproj targets are allowed.') -or
    $artifact.Contains('No enabled and successfully validated .NET SDK is registered') -or
    $artifact.Contains('startInfo.ArgumentList.Add("build");') -and -not $artifact.Contains('IsDotNetCompiler(compiler) && dotNetTarget')) {
    $findings.Add('A generic build path is still constrained to the .NET-only target or compiler gate.')
}
# Verify that filenames for other languages can be described with the existing persisted regex.
$fixtures = @(
    @{ Pattern = '(?i)(^|/)Makefile$'; File = 'Makefile'; Expected = $true },
    @{ Pattern = '(?i)\.py$'; File = 'src/module.py'; Expected = $true },
    @{ Pattern = '(?i)(^|/)Cargo\.toml$'; File = 'Cargo.toml'; Expected = $true },
    @{ Pattern = '(?i)\.(sln|slnx)$'; File = 'build/example.slnx'; Expected = $true },
    @{ Pattern = '(?i)\.py$'; File = 'build/example.csproj'; Expected = $false }
)
foreach ($fixture in $fixtures) {
    $actual = [regex]::IsMatch($fixture.File, $fixture.Pattern, [Text.RegularExpressions.RegexOptions]::CultureInvariant, [TimeSpan]::FromMilliseconds(250))
    if ($actual -ne $fixture.Expected) { $findings.Add("Configured filename regex did not classify '$($fixture.File)' as expected.") }
}
if ($findings.Count -gt 0) {
    foreach ($finding in $findings) { Write-Error $finding -ErrorAction Continue }
    throw ('Compiler-agnostic artifact build source contract failed with ' + $findings.Count + ' finding(s).')
}
Write-Host 'Compiler-agnostic artifact build source contract passed.'
