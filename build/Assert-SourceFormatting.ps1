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
        ".props",
        ".wapproj"
    )

    $excludedPrefixes = @(
        "artifacts/",
        ".git/",
        ".vs/",
        ".cr/",
        ".idea/",
        "node_modules/",
        "LocalGPTWebviewWrapper/LocalGPT/bin/",
        "LocalGPTWebviewWrapper/LocalGPT/obj/",
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/bin/",
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/obj/",
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)/bin/",
        "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)/obj/"
    )

    $requiredFiles = @(
        "README.md",
        "SECURITY.md",
        "AGENTS.md",
        "llms.txt",
        ".github/workflows/source-hygiene.yml",
        "Directory.Build.props",
        "LocalGPTWebviewWrapper/LocalGPT/Program.cs",
        "LocalGPTWebviewWrapper/LocalGPT/Services/NativeCommandRunner.cs",
        "LocalGPTWebviewWrapper/LocalGPT/Services/AiContextBootstrapService.cs",
        "LocalGPTWebviewWrapper/LocalGPT/LocalGPT.csproj"
    )

    $violations = [System.Collections.Generic.List[string]]::new()

    foreach ($requiredFile in $requiredFiles) {
        $fullRequiredPath = Join-Path $repoRoot $requiredFile
        if (-not (Test-Path -LiteralPath $fullRequiredPath -PathType Leaf)) {
            $violations.Add("Required maintained file is missing: $requiredFile")
            continue
        }

        if ((Get-Item -LiteralPath $fullRequiredPath).Length -eq 0) {
            $violations.Add("Required maintained file is empty: $requiredFile")
        }
    }

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

        $lines = @(Get-Content -LiteralPath $fullPath)
        for ($index = 0; $index -lt $lines.Count; $index++) {
            $line = $lines[$index]
            if ($line.Length -gt $MaxLineLength) {
                $violations.Add("${normalized}:$($index + 1) has $($line.Length) characters; limit is $MaxLineLength.")
            }

            if ($extension -eq ".cs" -and $line -match '^\s*using\s+static\s+System\.Net\.WebRequestMethods\s*;') {
                $violations.Add("${normalized}:$($index + 1) imports System.Net.WebRequestMethods statically. This exposes WebRequestMethods.File and can conflict with System.IO.File.")
            }
        }
    }

    if ($violations.Count -gt 0) {
        Write-Error ("Source formatting guard failed:`n" + ($violations -join "`n"))
    }

    Write-Host "Source formatting guard passed for tracked maintained files."
}
finally {
    Pop-Location
}
