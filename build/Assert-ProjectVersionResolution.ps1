Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$resolver = Join-Path $PSScriptRoot 'Get-ProjectVersion.ps1'
if (-not (Test-Path -LiteralPath $resolver -PathType Leaf)) { throw "Project version resolver is missing: $resolver" }

# Check the real source projects as well as XML shapes that previously threw under StrictMode.
$projectPaths = @(
    (Join-Path $root 'src/LocalGPT/LocalGPT.csproj'),
    (Join-Path $root 'src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj')
)
foreach ($projectPath in $projectPaths) {
    [xml]$projectXml = Get-Content -LiteralPath $projectPath -Raw
    $versionNode = $projectXml.SelectSingleNode('/Project/PropertyGroup/Version')
    if ($null -eq $versionNode) { throw "Project version was not found in $projectPath" }
    $expectedVersion = [string]$versionNode.InnerText
    $resolvedVersion = @(& $resolver -ProjectPath $projectPath)
    if ($resolvedVersion.Count -ne 1 -or $resolvedVersion[0] -ne $expectedVersion) {
        throw "Project version resolver returned an unexpected value for $projectPath."
    }
}

$fixtureDirectory = Join-Path ([IO.Path]::GetTempPath()) ('LocalGptProjectVersions-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($fixtureDirectory)
try {
    $fixturePath = Join-Path $fixtureDirectory 'fixture.csproj'
    $fixtures = @(
        @{ Name = 'one group'; Xml = '<Project><PropertyGroup><Version>3.4.5</Version></PropertyGroup></Project>'; Expected = '3.4.5' },
        @{ Name = 'version in later group'; Xml = '<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup><PropertyGroup><Version>3.4.5</Version></PropertyGroup></Project>'; Expected = '3.4.5' },
        @{ Name = 'version in first group'; Xml = '<Project><PropertyGroup><Version>3.4.5</Version></PropertyGroup><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>'; Expected = '3.4.5' },
        @{ Name = 'blank version followed by declared version'; Xml = '<Project><PropertyGroup><Version> </Version></PropertyGroup><PropertyGroup><Version>3.4.5</Version></PropertyGroup></Project>'; Expected = '3.4.5' },
        @{ Name = 'first declared version takes precedence'; Xml = '<Project><PropertyGroup><Version>3.4.5</Version></PropertyGroup><PropertyGroup><Version>4.5.6</Version></PropertyGroup></Project>'; Expected = '3.4.5' },
        @{ Name = 'unrelated nested version is ignored'; Xml = '<Project><ItemGroup><Version>9.9.9</Version></ItemGroup><PropertyGroup><Version>3.4.5</Version></PropertyGroup></Project>'; Expected = '3.4.5' }
    )
    foreach ($fixture in $fixtures) {
        [IO.File]::WriteAllText($fixturePath, $fixture.Xml, (New-Object Text.UTF8Encoding($false)))
        $result = @(& $resolver -ProjectPath $fixturePath)
        if ($result.Count -ne 1 -or $result[0] -ne $fixture.Expected) {
            throw "Project version resolver failed fixture '$($fixture.Name)'."
        }
    }
    foreach ($missingVersionXml in @(
        '<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>',
        '<Project><PropertyGroup><Version> </Version></PropertyGroup></Project>'
    )) {
        [IO.File]::WriteAllText($fixturePath, $missingVersionXml, (New-Object Text.UTF8Encoding($false)))
        $failedAsExpected = $false
        try { $null = & $resolver -ProjectPath $fixturePath }
        catch {
            if ($_.Exception.Message -like 'Project version was not found*') { $failedAsExpected = $true }
            else { throw }
        }
        if (-not $failedAsExpected) { throw 'Project version resolver must reject missing and empty versions.' }
    }
}
finally {
    Remove-Item -LiteralPath $fixtureDirectory -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host 'Project version resolution passed for actual projects and 8 strict-mode XML fixtures.' -ForegroundColor Green
