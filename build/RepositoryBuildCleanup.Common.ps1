function Clear-LocalGptProjectBuildState {
    param(
        [Parameter(Mandatory)][string]$SourceRoot,
        [switch]$IncludeBin,
        [switch]$BestEffort
    )

    $source = Get-Item -LiteralPath $SourceRoot -Force
    if (-not $source.PSIsContainer -or ($source.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Build cleanup requires a regular source directory: $SourceRoot"
    }

    $outputNames = @('obj')
    if ($IncludeBin) { $outputNames += 'bin' }
    $removed = 0
    foreach ($project in (Get-ChildItem -LiteralPath $source.FullName -Directory -Force)) {
        if ($project.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        if (-not (Get-ChildItem -LiteralPath $project.FullName -File -Filter '*.csproj' | Select-Object -First 1)) { continue }

        foreach ($name in $outputNames) {
            $path = Join-Path $project.FullName $name
            $output = Get-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
            if ($null -eq $output -or -not $output.PSIsContainer) { continue }
            if ($output.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                Write-Warning "Skipping linked build directory '$path'."
                continue
            }

            try {
                Remove-Item -LiteralPath $output.FullName -Recurse -Force -ErrorAction Stop
                $removed++
            }
            catch {
                if (-not $BestEffort) { throw }
                Write-Warning "Could not clear build directory '${path}': $_"
            }
        }
    }
    return $removed
}
