param(
    [int]$MaxLineLength = 600
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $repoRoot
try {
    $trackedFiles = git ls-files
    if ($LASTEXITCODE -ne 0) {
        throw "git ls-files failed with exit code $LASTEXITCODE."
    }

    $extensions = @(".cs", ".razor", ".md", ".ps1", ".json")
    $excludedPrefixes = @(
        "artifacts/",
        "LocalGPTWebviewWrapper/LocalGPT/bin/",
        "LocalGPTWebviewWrapper/LocalGPT/obj/",
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/bin/",
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/obj/",
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)/bin/",
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)/obj/"
    )

    $criticalMinimumLines = @{
        "README.md" = 80
        "LocalGPTWebviewWrapper/LocalGPT/Program.cs" = 120
        "LocalGPTWebviewWrapper/LocalGPT/Services/NativeCommandRunner.cs" = 120
        "LocalGPTWebviewWrapper/LocalGPT/Services/AiContextBootstrapService.cs" = 100
    }

    $violations = New-Object System.Collections.Generic.List[string]
    foreach ($relativePath in $trackedFiles) {
        $normalized = $relativePath.Replace("\", "/")
        if ($excludedPrefixes | Where-Object { $normalized.StartsWith($_, [StringComparison]::OrdinalIgnoreCase) }) {
            continue
        }

        $extension = [IO.Path]::GetExtension($normalized)
        if (-not ($extensions -contains $extension)) {
            continue
        }

        $fullPath = Join-Path $repoRoot $relativePath
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            continue
        }

        $lines = Get-Content -LiteralPath $fullPath
        if ($null -eq $lines) {
            $lines = @()
        }

        for ($index = 0; $index -lt $lines.Count; $index++) {
            $lineLength = $lines[$index].Length
            if ($lineLength -gt $MaxLineLength) {
                $violations.Add("${normalized}:$($index + 1) has $lineLength characters; limit is $MaxLineLength.")
            }
        }

        if ($criticalMinimumLines.ContainsKey($normalized) -and $lines.Count -lt $criticalMinimumLines[$normalized]) {
            $violations.Add("$normalized has only $($lines.Count) physical lines; expected at least $($criticalMinimumLines[$normalized]).")
        }
    }

    if ($violations.Count -gt 0) {
        Write-Error ("Source formatting guard failed:`n" + ($violations -join "`n"))
    }

    Write-Host "Source formatting guard passed for tracked .cs/.razor/.md/.ps1/.json files."
}
finally {
    Pop-Location
}
