param(
    [int]$MaxLineLength = 600
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
. (Join-Path $PSScriptRoot "RepositoryValidation.Common.ps1")
Push-Location $repoRoot
try {
    # Validate the physical source tree, including newly created files that are
    # not yet tracked by Git. Source ZIPs intentionally do not contain .git.
    $trackedFiles = @(Get-MaintainedRepositoryFiles -RepositoryRoot $repoRoot | ForEach-Object {
        Get-RelativePathPortable -BasePath $repoRoot -TargetPath $_.FullName
    })

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
        ".wapproj",
        ".js"
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
        "LocalGPTWebviewWrapper/LocalGPT/Services/Formatting/ChatContentRenderer.cs",
        "LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Chat.razor",
        "LocalGPTWebviewWrapper/LocalGPT/wwwroot/js/chat-details-state.js",
        "LocalGPTWebviewWrapper/LocalGPT/LocalGPT.csproj",
        "build/RepositoryValidation.Common.ps1",
        "build/Assert-CSharpSyntax.ps1",
        "build/Invoke-RepositoryValidation.ps1",
        "build/New-VerifiedSourcePackage.ps1",
        "docs/engineering/build-validation.md"
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

    $streamingStateContracts = @(
        @{ Path = "LocalGPTWebviewWrapper/LocalGPT/Services/Formatting/ChatContentRenderer.cs"; Pattern = "data-localgpt-panel-key"; Description = "stable streamed panel keys" },
        @{ Path = "LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Chat.razor"; Pattern = "data-localgpt-details-host"; Description = "per-message streamed panel state host" },
        @{ Path = "LocalGPTWebviewWrapper/LocalGPT/Components/App.razor"; Pattern = "js/chat-details-state.js"; Description = "streamed panel state browser helper registration" }
    )

    foreach ($contract in $streamingStateContracts) {
        $contractPath = Join-Path $repoRoot $contract.Path
        if (-not (Test-Path -LiteralPath $contractPath -PathType Leaf)) {
            $violations.Add("Streaming-state contract file is missing: $($contract.Path)")
            continue
        }

        $contractContent = Get-Content -LiteralPath $contractPath -Raw
        if (-not ($contractContent.IndexOf($contract.Pattern, [StringComparison]::Ordinal) -ge 0)) {
            $violations.Add("$($contract.Path) no longer contains $($contract.Description).")
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
