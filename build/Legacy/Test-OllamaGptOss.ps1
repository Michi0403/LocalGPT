[CmdletBinding()]
param(
    [string]$Model = "gpt-oss:20b",
    [string]$BaseUri = "http://localhost:11434",
    [switch]$PullIfMissing,
    [switch]$StartServerIfDown,
    [int]$NumPredict = 512,
    [int]$TimeoutSeconds = 180
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "== $Message =="
}

function Invoke-OllamaJson {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [string]$Method = "GET",
        [object]$Body = $null,
        [int]$Timeout = 30
    )

    $uri = $BaseUri.TrimEnd("/") + $Path
    $parameters = @{
        Uri = $uri
        Method = $Method
        UseBasicParsing = $true
        TimeoutSec = $Timeout
    }

    if ($null -ne $Body) {
        $parameters.ContentType = "application/json"
        $parameters.Body = ($Body | ConvertTo-Json -Depth 12)
    }

    $response = Invoke-WebRequest @parameters
    return $response.Content | ConvertFrom-Json
}

function Test-OllamaServer {
    try {
        $null = Invoke-OllamaJson -Path "/api/version" -Timeout 5
        return $true
    }
    catch {
        return $false
    }
}

Write-Step "Ollama executable"
try {
    & ollama --version
}
catch {
    throw "Could not run 'ollama --version'. Install Ollama or add it to PATH."
}

if (-not (Test-OllamaServer)) {
    if ($StartServerIfDown) {
        Write-Step "Starting Ollama server"
        Start-Process ollama -ArgumentList "serve" -WindowStyle Hidden
        Start-Sleep -Seconds 5
    }
    else {
        throw "Ollama is not responding at $BaseUri. Start Ollama or rerun with -StartServerIfDown."
    }
}

Write-Step "Ollama version endpoint"
$version = Invoke-OllamaJson -Path "/api/version" -Timeout 10
$version | ConvertTo-Json -Depth 5

Write-Step "Installed models"
$tags = Invoke-OllamaJson -Path "/api/tags" -Timeout 30
$tags | ConvertTo-Json -Depth 8

$installed = @($tags.models | Where-Object { $_.model -eq $Model -or $_.name -eq $Model })
if ($installed.Count -eq 0) {
    if ($PullIfMissing) {
        Write-Step "Pulling $Model"
        & ollama pull $Model
        $tags = Invoke-OllamaJson -Path "/api/tags" -Timeout 30
    }
    else {
        throw "$Model is not installed. Run 'ollama pull $Model' or rerun this script with -PullIfMissing."
    }
}

Write-Step "Running models"
try {
    $ps = Invoke-OllamaJson -Path "/api/ps" -Timeout 30
    $ps | ConvertTo-Json -Depth 8
}
catch {
    Write-Warning "/api/ps failed: $($_.Exception.Message)"
}

Write-Step "Chat endpoint smoke test"
$chatBody = @{
    model = $Model
    stream = $false
    keep_alive = "10m"
    messages = @(
        @{
            role = "user"
            content = "Reply with exactly: LocalGPT Ollama chat test passed."
        }
    )
    options = @{
        temperature = 0
        num_predict = $NumPredict
    }
}

$chatOk = $false
try {
    $chat = Invoke-OllamaJson -Path "/api/chat" -Method "POST" -Body $chatBody -Timeout $TimeoutSeconds
    $chat | ConvertTo-Json -Depth 8
    $content = [string]$chat.message.content
    $chatOk = -not [string]::IsNullOrWhiteSpace($content)
    if (-not $chatOk) {
        Write-Warning "/api/chat returned an empty assistant message. gpt-oss models can spend early tokens in the 'thinking' field; increase -NumPredict if done_reason is length."
    }
}
catch {
    Write-Warning "/api/chat failed: $($_.Exception.Message)"
}

Write-Step "Generate endpoint smoke test"
$generateBody = @{
    model = $Model
    stream = $false
    keep_alive = "10m"
    prompt = "Reply with exactly: LocalGPT Ollama generate test passed."
    options = @{
        temperature = 0
        num_predict = $NumPredict
    }
}

$generateOk = $false
try {
    $generate = Invoke-OllamaJson -Path "/api/generate" -Method "POST" -Body $generateBody -Timeout $TimeoutSeconds
    $generate | ConvertTo-Json -Depth 8
    $content = [string]$generate.response
    $generateOk = -not [string]::IsNullOrWhiteSpace($content)
    if (-not $generateOk) {
        Write-Warning "/api/generate returned an empty response."
    }
}
catch {
    Write-Warning "/api/generate failed: $($_.Exception.Message)"
}

Write-Step "Result"
if ($chatOk -or $generateOk) {
    Write-Host "Ollama is producing text for $Model. LocalGPT/DXAiChat can now be tested against the model."
    exit 0
}

Write-Warning "Ollama is reachable and $Model is installed, but the model did not produce text in the smoke tests."
Write-Warning "Recommended recovery: rerun with a larger -NumPredict, restart/update Ollama, then re-pull with: ollama pull $Model"
exit 2
