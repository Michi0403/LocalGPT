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

    $extensions = @(
        ".cs",
        ".razor",
        ".md",
        ".ps1",
        ".json",
        ".yml",
        ".yaml",
        ".csproj",
        ".wapproj"
    )

    $excludedPrefixes = @(
        "artifacts/",
        ".git/",
        ".vs/",
        ".idea/",
        "node_modules/",
        "LocalGPTWebviewWrapper/LocalGPT/bin/",
        "LocalGPTWebviewWrapper/LocalGPT/obj/",
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/bin/",
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/obj/",
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)/bin/",
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)/obj/"
    )

    $criticalMinimumLines = @{
        "README.md" = 80
        "SECURITY.md" = 60
        "AGENTS.md" = 120
        "llms.txt" = 30
        ".github/workflows/source-hygiene.yml" = 15
        "LocalGPTWebviewWrapper/LocalGPT/Program.cs" = 120
        "LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Database.razor" = 400
        "LocalGPTWebviewWrapper/LocalGPT/Services/NativeCommandRunner.cs" = 120
        "LocalGPTWebviewWrapper/LocalGPT/Services/AiContextBootstrapService.cs" = 100
        "LocalGPTWebviewWrapper/LocalGPT/LocalGPT.csproj" = 100
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)/LocalGPTWebviewWrapper (Package).wapproj" = 180
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

    Write-Host "Source formatting guard passed for tracked human-maintained source, project, workflow, and docs files."
}
finally {
    Pop-Location
}
