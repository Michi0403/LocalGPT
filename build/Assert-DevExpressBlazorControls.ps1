param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'

$componentsRoot = Join-Path $RepositoryRoot 'src/LocalGPT/Components'
if (-not (Test-Path -LiteralPath $componentsRoot)) {
    throw "DevExpress Razor control guard could not find $componentsRoot"
}

# Only the reconnect/reload controls in App.razor are allowed to remain native because they must
# still work while the InteractiveServer circuit itself is disconnected and DevExpress cannot dispatch events.
$allowed = @{
    'src/LocalGPT/Components/App.razor' = @(
        'data-localgpt-reconnect',
        'data-localgpt-reload'
    )
}

$nativePatterns = @(
    @{ Regex = '<\s*button\b'; Replacement = 'DxButton' },
    @{ Regex = '<\s*textarea\b'; Replacement = 'DxMemo' },
    @{ Regex = '<\s*select\b'; Replacement = 'DxComboBox/DxListBox/DxTreeView' },
    @{ Regex = '<\s*datalist\b'; Replacement = 'DxComboBox/DxListBox' },
    @{ Regex = '<\s*option\b'; Replacement = 'DevExpress Data source item' },
    @{ Regex = '<\s*input\b'; Replacement = 'DxTextBox/DxCheckBox/DxSpinEdit/DxDateEdit/DxRangeSelector' },
    @{ Regex = '<\s*InputText\b'; Replacement = 'DxTextBox' },
    @{ Regex = '<\s*InputTextArea\b'; Replacement = 'DxMemo' },
    @{ Regex = '<\s*InputCheckbox\b'; Replacement = 'DxCheckBox' },
    @{ Regex = '<\s*InputNumber\b'; Replacement = 'DxSpinEdit' },
    @{ Regex = '<\s*InputDate\b'; Replacement = 'DxDateEdit' },
    @{ Regex = '<\s*InputSelect\b'; Replacement = 'DxComboBox/DxListBox' }
)

$violations = New-Object System.Collections.Generic.List[object]
$files = Get-ChildItem -LiteralPath $componentsRoot -Recurse -File -Filter '*.razor' | Sort-Object FullName
foreach ($file in $files) {
    $relative = ($file.FullName.Substring($RepositoryRoot.Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) -replace '\\', '/')
    $lineNumber = 0
    foreach ($line in [IO.File]::ReadLines($file.FullName)) {
        $lineNumber++
        foreach ($pattern in $nativePatterns) {
            $match = [regex]::Match($line, $pattern.Regex, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if (-not $match.Success) { continue }

            $isAllowed = $false
            if ($allowed.ContainsKey($relative)) {
                foreach ($marker in $allowed[$relative]) {
                    if ($line.Contains($marker)) { $isAllowed = $true; break }
                }
            }
            if ($isAllowed) { continue }

            $violations.Add([pscustomobject]@{
                File = $relative
                Line = $lineNumber
                Column = $match.Index + 1
                Replacement = $pattern.Replacement
                Source = $line.Trim()
            })
        }
    }
}

if ($violations.Count -gt 0) {
    foreach ($violation in $violations) {
        # MSBuild/Visual Studio recognizes file(line,column): error CODE: message.
        Write-Output ("{0}({1},{2}): error LGDX0001: Native/legacy Razor interactive control is forbidden. Use {3}. Source: {4}" -f $violation.File, $violation.Line, $violation.Column, $violation.Replacement, $violation.Source)
    }
    throw "DevExpress Blazor control validation failed with $($violations.Count) violation(s). Ordinary interactive Razor controls must use DevExpress Blazor components first."
}

Write-Output "DevExpress Blazor control validation passed: all ordinary Razor interactive controls use DevExpress components; only the two circuit-independent App.razor reconnect controls are native."
