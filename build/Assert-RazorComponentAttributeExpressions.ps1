param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$componentRoots = New-Object 'System.Collections.Generic.List[string]'
foreach ($candidateRoot in @(
    (Join-Path $RepositoryRoot 'src/LocalGPT/Components'),
    (Join-Path $RepositoryRoot 'src/PublisherStudio.Web/Components')
)) {
    if (Test-Path -LiteralPath $candidateRoot -PathType Container) { [void]$componentRoots.Add([string]$candidateRoot) }
}
if ($componentRoots.Count -eq 0) {
    throw "Razor component-attribute validation could not find a maintained Components root under $RepositoryRoot"
}

$violations = New-Object System.Collections.Generic.List[object]
$genericPattern = [regex]'(?<attribute>\b[A-Z][A-Za-z0-9_]*)\s*=\s*"@(?<expression>[A-Za-z_][A-Za-z0-9_\.]*\s*<[^"\r\n>]+>\s*\([^"\r\n]*\))"'
$mixedPattern = [regex]'(?<attribute>\b[A-Z][A-Za-z0-9_]*)\s*=\s*"@(?<member>[A-Za-z_][A-Za-z0-9_\.]*)\s+(?<literal>[^"\r\n]+)"'

foreach ($componentsRoot in $componentRoots) {
    foreach ($file in @(Get-ChildItem -LiteralPath $componentsRoot -Recurse -File -Filter '*.razor' | Sort-Object FullName)) {
        $lineNumber = 0
        foreach ($line in [IO.File]::ReadLines($file.FullName)) {
            $lineNumber++
            foreach ($rule in @(
                [pscustomobject]@{ Regex = $genericPattern; Code = 'RZARCH0001'; Reason = 'Generic C# invocation/expression is embedded directly in a component attribute.' },
                [pscustomobject]@{ Regex = $mixedPattern; Code = 'RZARCH0002'; Reason = 'Component attribute mixes a C# member expression with literal markup text.' }
            )) {
                $match = $rule.Regex.Match($line)
                if (-not $match.Success) { continue }
                $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace('\','/')
                $violations.Add([pscustomobject]@{
                    File = $relative
                    Line = $lineNumber
                    Column = $match.Index + 1
                    Code = $rule.Code
                    Attribute = $match.Groups['attribute'].Value
                    Reason = $rule.Reason
                    Source = $line.Trim()
                })
            }
        }
    }
}

if ($violations.Count -gt 0) {
    foreach ($v in $violations) {
        Write-Output ("{0}({1},{2}): error {3}: {4} Attribute '{5}'. Source: {6}" -f $v.File, $v.Line, $v.Column, $v.Code, $v.Reason, $v.Attribute, $v.Source)
        Write-Output "  Architectural choices: (1) prepare the value in a typed instance property/field or lifecycle state and bind that simple member; (2) for asynchronous/stateful preparation, load it after the correct render/attachment boundary and bind the stored result; (3) for Task-returning events, bind a named async handler or a simple 'async () => await MethodAsync(context)' callback. Keep the DevExpress component; do not replace it with native HTML to avoid the Razor parser."
    }
    throw "Razor component-attribute validation failed with $($violations.Count) parser-fragile component attribute(s)."
}

Write-Host "Razor component-attribute validation passed: generic render expressions and mixed literal/C# component attributes are prepared outside Razor attributes."
