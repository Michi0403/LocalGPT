param(
    [string[]]$Models = @("gpt-oss:20b", "deepseek-r1:8b"),
    [string]$OllamaBaseUri = "http://localhost:11434",
    [int]$MaxRounds = 0,
    [int]$MaxOutputTokens = 1536,
    [int]$MaxContextTokens = 8192,
    [int]$ModelTimeoutSeconds = 360,
    [string]$KeepAlive = "0s",
    [int]$MaxExcerptChars = 1100,
    [int]$GpuLayers = 20,
    [switch]$AutoGpu,
    [switch]$CpuOnly
)

$ErrorActionPreference = "Stop"

function Get-RepositoryRoot {
    $directory = Get-Item -LiteralPath (Get-Location)
    while ($null -ne $directory) {
        if (Test-Path -LiteralPath (Join-Path $directory.FullName ".git")) {
            return $directory.FullName
        }

        $directory = $directory.Parent
    }

    throw "Could not find repository root from $(Get-Location)."
}

function Get-Excerpt {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [int]$MaxCharacters
    )

    if (!(Test-Path -LiteralPath $Path)) {
        return "## $Path`n[missing]"
    }

    $text = Get-Content -LiteralPath $Path -Raw
    $text = $text -replace "`r`n", "`n"
    if ($text.Length -le $MaxCharacters) {
        return "## $Path`n``````text`n$text`n``````"
    }

    $omission = "`n...[middle trimmed for council context limit]...`n"
    $remaining = [Math]::Max($MaxCharacters - $omission.Length, 80)
    $head = [Math]::Floor($remaining / 2)
    $tail = $remaining - $head
    $trimmed = $text.Substring(0, $head).TrimEnd() + $omission + $text.Substring($text.Length - $tail).TrimStart()
    return "## $Path`n``````text`n$trimmed`n``````"
}

function ConvertTo-FlatPreview {
    param([string]$Text, [int]$MaxCharacters = 500)

    $flat = ($Text ?? "") -replace "\s+", " "
    if ($flat.Length -le $MaxCharacters) {
        return $flat
    }

    return $flat.Substring(0, $MaxCharacters).TrimEnd() + "..."
}

$repoRoot = Get-RepositoryRoot
Set-Location -LiteralPath $repoRoot

$runtimeEndpointPath = Join-Path $env:LOCALAPPDATA "LocalGPT\runtime\server.json"
if (!(Test-Path -LiteralPath $runtimeEndpointPath)) {
    throw "LocalGPT runtime endpoint file was not found at $runtimeEndpointPath. Start LocalGPT first."
}

$server = Get-Content -LiteralPath $runtimeEndpointPath -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($server.BaseUrl)) {
    throw "LocalGPT runtime endpoint file does not contain BaseUrl: $runtimeEndpointPath"
}

$health = Invoke-RestMethod -Uri "$($server.BaseUrl)/health" -TimeoutSec 30
if ($health -ne "Healthy") {
    throw "LocalGPT backend health check failed at $($server.BaseUrl)/health: $health"
}

$allSource = git ls-files |
    Where-Object {
        $_ -match "\.(cs|razor|csproj|xaml|json|ps1|md)$" -and
        $_ -notmatch "wwwroot/(images|css|switcher-resources|android|apple|favicon)"
    } |
    Sort-Object

$counts = $allSource |
    ForEach-Object { [IO.Path]::GetExtension($_).ToLowerInvariant() } |
    Group-Object |
    Sort-Object Name |
    ForEach-Object { "- $($_.Name): $($_.Count)" }

$keyFiles = @(
    "LocalGPTWebviewWrapper/LocalGPT/Program.cs",
    "LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Chat.razor",
    "LocalGPTWebviewWrapper/LocalGPT/Components/Pages/ModelCouncil.razor",
    "LocalGPTWebviewWrapper/LocalGPT/Components/Pages/MinecraftModBuilder.razor",
    "LocalGPTWebviewWrapper/LocalGPT/Services/CompositeChatClient.cs",
    "LocalGPTWebviewWrapper/LocalGPT/Services/OllamaThinkingChatClient.cs",
    "LocalGPTWebviewWrapper/LocalGPT/Services/MultiModelCouncilService.cs",
    "LocalGPTWebviewWrapper/LocalGPT/Services/CouncilChatClient.cs",
    "LocalGPTWebviewWrapper/LocalGPT/Services/EfChatMemoryService.cs",
    "LocalGPTWebviewWrapper/LocalGPT/Services/CouncilKnowledgeService.cs",
    "LocalGPTWebviewWrapper/LocalGPT/Services/SqliteTableEditorService.cs",
    "LocalGPTWebviewWrapper/LocalGPT/Logging/DatabaseLogger.cs",
    "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/App.xaml.cs",
    "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/MainWindow.xaml.cs",
    "LocalGPTWebviewWrapper/LocalGPT/LocalGPT.csproj",
    "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    "README.md",
    "LocalGPTWebviewWrapper/readme.md",
    "docs/ARCHITECTURE_FOR_AI.md",
    "docs/MINECRAFT_MOD_AI_BUILDER.md",
    "docs/LOCALGPT_WORKFLOW_MEMORY.md"
)

$excerpts = $keyFiles | ForEach-Object { Get-Excerpt -Path $_ -MaxCharacters $MaxExcerptChars }

$prompt = @"
You are the LocalGPT AI Council reviewing Michi0403's LocalGPT repository. Be direct, technical, kind, and honest.

Task: review the whole codebase from this compact code map and targeted excerpts. Do not claim you saw every line verbatim. Treat the complete file list as the full repository map; treat excerpts as representative hotspots. If a risk needs a deeper pass, name the exact file(s) to inspect next.

Repository purpose:
- Blazor/ASP.NET Core LocalGPT backend hosted inside WinUI WebView2 wrapper.
- DevExpress Blazor UI with DXAiChat.
- Ollama/LM Studio discovery, gpt-oss:20b, AI Council, EF Core SQLite memory, editable knowledge DB, database logger.
- Minecraft Java mod/plugin/datapack builder, code/artifact downloads, frontend smoke testing through WebView2.
- Recent fixes: Auto GPU mode, context capped for configured Ollama, streaming thinking blocks, quiet cancellation, explicit fresh-chat diagnostics, local prompt trimming.

Build status known to supervisor:
- LocalGPT and WinUI wrapper builds succeeded after latest changes.
- Direct Ollama gpt-oss:20b probe returned READY with 100% GPU and context 4096.
- WebView2 diagnostic snapshot writer still needs deeper investigation because recent smoke runs loaded gpt-oss correctly but did not write fresh JSON snapshots.

Source file counts:
$($counts -join "`n")

Complete tracked source/config/doc map considered for this review:
$($allSource -join "`n")

Targeted excerpts from architecture/runtime/frontend/council/memory files:
$($excerpts -join "`n`n")

Return Markdown with these sections:
1. Top findings: concrete bugs/risks first, with file paths.
2. Architecture feedback: what is strong, what is fragile.
3. DXAiChat/AI Council behavior: streaming, model selection, token/context management, fairness between models.
4. Memory/SQLite/knowledge database: correctness, safety, user approval model.
5. Minecraft builder/datapack flow: what likely works, what needs verification.
6. WebView2/debug/deploy pipeline: especially the diagnostic snapshot issue.
7. User-facing UX/tooltips/default presets.
8. Suggested next implementation steps, ordered by risk/impact.
9. Files you want to inspect in a deeper follow-up pass.
"@

$body = [ordered]@{
    Prompt = $prompt
    ModelNames = $Models
    BaseUri = $OllamaBaseUri
    MaxRounds = $MaxRounds
    MaxOutputTokens = $MaxOutputTokens
    MaxParallelModels = 1
    MaxContextTokens = $MaxContextTokens
    ModelTimeoutSeconds = $ModelTimeoutSeconds
    OllamaKeepAlive = $KeepAlive
    OllamaNumGpu = if ($CpuOnly) { 0 } elseif ($AutoGpu) { $null } else { $GpuLayers }
    IncludeMemory = $false
    SaveToMemory = $true
    Title = "Whole-code AI Council repository review"
    GenerateImplementationArtifact = $false
}

$gpuMode = if ($CpuOnly) { "CPU-only num_gpu=0" } elseif ($AutoGpu) { "Auto GPU" } else { "Balanced GPU num_gpu=$GpuLayers" }
Write-Host "Calling LocalGPT AI Council at $($server.BaseUrl) with models: $($Models -join ', ')"
Write-Host "GPU mode: $gpuMode"
$result = Invoke-RestMethod -Uri "$($server.BaseUrl)/__diag/council" -Method Post -ContentType "application/json" -Body ($body | ConvertTo-Json -Depth 8) -TimeoutSec ([Math]::Max($ModelTimeoutSeconds * ($Models.Count + 2), 300))

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportPath = Join-Path $repoRoot "docs\AI_COUNCIL_CODE_REVIEW_$timestamp.md"
$stepMarkdown = foreach ($step in @($result.steps)) {
    @"
### $($step.modelName) - $($step.phase) / $($step.role)

- Duration: $([Math]::Round($step.durationSeconds, 1)) seconds
- Error: $($step.error ?? "none")

$($step.visibleContent)

"@
}

$report = @"
# AI Council Whole-Code Review - $timestamp

Generated by `LocalGPTWebviewWrapper/build/Invoke-AiCouncilCodeReview.ps1`.

## Run Metadata

- Run id: $($result.runId)
- Models: $($result.modelNames -join ", ")
- Max rounds: $MaxRounds
- Max output tokens: $MaxOutputTokens
- Max context tokens: $MaxContextTokens
- Parallel models: 1
- Ollama keep-alive: $KeepAlive
- CPU only: $CpuOnly
- Auto GPU: $AutoGpu
- GPU layers: $(if ($CpuOnly) { 0 } elseif ($AutoGpu) { "auto" } else { $GpuLayers })
- Memory conversation id: $($result.memoryConversationId)
- Knowledge entry id: $($result.knowledgeEntryId)
- Council log path: $($result.logPath)

## Council Warnings

$((@($result.warnings) | ForEach-Object { "- $_" }) -join "`n")

## Final Answer

$($result.finalAnswer)

## Council Steps

$($stepMarkdown -join "`n")

## Prompt Source Summary

The council was shown:

- A complete tracked source/config/doc file map for source-like files.
- Source file counts by extension.
- Targeted excerpts from the main runtime, DXAiChat, council, memory, SQLite, logging, Minecraft builder, WebView2 wrapper, project, README, and AI guidance files.

It was explicitly told not to claim it saw every source line verbatim and to request deeper file passes where needed.
"@

Set-Content -LiteralPath $reportPath -Value $report -Encoding UTF8

[pscustomobject]@{
    RunId = $result.runId
    Models = ($result.modelNames -join ", ")
    Warnings = (@($result.warnings) -join " ; ")
    MemoryConversationId = $result.memoryConversationId
    KnowledgeEntryId = $result.knowledgeEntryId
    LogPath = $result.logPath
    ReportPath = $reportPath
    FinalAnswerPreview = ConvertTo-FlatPreview -Text $result.finalAnswer -MaxCharacters 1200
    Steps = @($result.steps | ForEach-Object {
        [pscustomobject]@{
            Model = $_.modelName
            Phase = $_.phase
            Role = $_.role
            Seconds = [Math]::Round($_.durationSeconds, 1)
            Error = $_.error
            Preview = ConvertTo-FlatPreview -Text $_.visibleContent -MaxCharacters 300
        }
    })
} | ConvertTo-Json -Depth 8
