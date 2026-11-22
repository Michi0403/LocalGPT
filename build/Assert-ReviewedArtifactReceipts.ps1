$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$verifier = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CouncilDevelopmentDxAiFunctions.cs'))
$generation = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CodeGenerationWorkflowService.Execution.cs'))
$findings = [System.Collections.Generic.List[string]]::new()

foreach ($token in @(
    '!review.ApprovalConsumed',
    'foreach (var file in review.Files)',
    'if (matches.Count != 1)',
    'if (actualHash != file.ContentHash)',
    'if (review.Status != CodeGenerationReviewStatuses.BuildPassed)',
    'Status = "SourceOnly"',
    'Succeeded = false',
    'Path.GetFileName(review.ZipFileName) != review.ZipFileName')) {
    if (-not $verifier.Contains($token)) { $findings.Add("Artifact verifier no longer enforces: $token") }
}
if ($verifier.Contains('if (review.IsSourceDraftReview && review.Status != CodeGenerationReviewStatuses.BuildPassed)') -or
    $verifier.Contains('if (review.IsSourceDraftReview)' + [Environment]::NewLine + '                {' + [Environment]::NewLine + '                    foreach')) {
    $findings.Add('Review source hash and non-compiled status checks must apply to direct source reviews too.')
}
$directoryFirst = $generation.IndexOf('Directory.CreateDirectory(councilArtifacts.ArtifactRoot);', [StringComparison]::Ordinal)
$zipCreation = $generation.IndexOf('ZipFile.CreateFromDirectory(workspaceRoot, zipPath', [StringComparison]::Ordinal)
if ($directoryFirst -lt 0 -or $zipCreation -le $directoryFirst) {
    $findings.Add('The CouncilArtifacts download root must be created before ZIP packaging (including project-bound workspaces).')
}
if ($findings.Count -gt 0) {
    foreach ($finding in $findings) { Write-Error $finding -ErrorAction Continue }
    throw ('Reviewed artifact receipt contract failed: ' + $findings.Count + ' finding(s).')
}
Write-Host 'Reviewed artifact approval, source-integrity, status, and download-directory contracts passed.'
