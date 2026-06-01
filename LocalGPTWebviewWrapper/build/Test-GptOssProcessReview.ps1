[CmdletBinding()]
param(
    [string]$Model = "gpt-oss:20b",
    [string]$BaseUri = "http://localhost:11434",
    [string[]]$Facts = @(),
    [string]$Question = "Review the current LocalGPT process and suggest grounded risks, next checks, and feature ideas.",
    [int]$NumPredict = 4096,
    [int]$TimeoutSeconds = 240
)

$ErrorActionPreference = "Stop"

function Invoke-OllamaJson {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [Parameter(Mandatory)]
        [object]$Body
    )

    $uri = $BaseUri.TrimEnd("/") + $Path
    $response = Invoke-WebRequest -Uri $uri -Method POST -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 16) -UseBasicParsing -TimeoutSec $TimeoutSeconds
    return $response.Content | ConvertFrom-Json
}

$evidenceLines = @(
    "LocalGPT is a Blazor/ASP.NET Core app hosted by a WinUI WebView2 shell.",
    "The preferred local debug model is Ollama gpt-oss:20b.",
    "Treat missing evidence as unknown, not as permission to invent details."
) + $Facts

$evidence = ($evidenceLines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { "- $($_.Trim())" }) -join "`n"

$prompt = @"
You are a grounded second reviewer for LocalGPT implementation work.

Rules:
- Use only the evidence below for factual claims.
- If something is plausible but not in the evidence, put it under "Needs verification".
- Do not invent file paths, commits, tests, UI results, or user decisions.
- Be kind, concise, and useful.
- Keep private reasoning brief enough to leave room for the visible review.
- Return Markdown with exactly these sections: Verified facts, Risks, Next checks, Feature ideas, Needs verification.

Evidence:
$evidence

Question:
$Question
"@

$body = @{
    model = $Model
    stream = $false
    keep_alive = "10m"
    messages = @(
        @{
            role = "user"
            content = $prompt
        }
    )
    options = @{
        temperature = 0
        num_predict = $NumPredict
    }
}

$result = Invoke-OllamaJson -Path "/api/chat" -Body $body
$content = [string]$result.message.content
$thinking = [string]$result.message.thinking

if (-not [string]::IsNullOrWhiteSpace($thinking)) {
    Write-Host "== gpt-oss thinking =="
    Write-Host $thinking
    Write-Host ""
}

Write-Host "== grounded process review =="
Write-Host $content

if ([string]::IsNullOrWhiteSpace($content)) {
    Write-Warning "gpt-oss returned no visible process review. Increase -NumPredict or restart Ollama."
    exit 2
}
