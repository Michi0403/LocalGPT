param(
    [Parameter(Mandatory = $false)]
    [string]$PromptPath,

    [int]$WaitSeconds = 600,

    [string]$DebuggerJsonUrl = "http://127.0.0.1:9222/json",

    [switch]$CollectOnly
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
$runId = Get-Date -Format "yyyyMMdd-HHmmss"
$artifactRoot = Join-Path $repoRoot "artifacts\e2e\$runId-webview2-prompt"
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

function Write-E2ELog {
    param([string]$Message)

    $line = "[{0:O}] {1}" -f [DateTimeOffset]::Now, $Message
    Write-Host $line
    Add-Content -Path (Join-Path $artifactRoot "send-prompt.log") -Value $line
}

function Wait-Until {
    param(
        [scriptblock]$Condition,
        [string]$Description,
        [int]$Seconds = 60
    )

    $deadline = [DateTimeOffset]::Now.AddSeconds($Seconds)
    while ([DateTimeOffset]::Now -lt $deadline) {
        try {
            $value = & $Condition
            if ($value) {
                return $value
            }
        }
        catch {
            # Retry until timeout.
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Timed out waiting for $Description after $Seconds seconds."
}

function Send-CdpCommand {
    param(
        [System.Net.WebSockets.ClientWebSocket]$WebSocket,
        [int]$Id,
        [string]$Method,
        [hashtable]$Params = @{}
    )

    $payload = @{
        id = $Id
        method = $Method
    }

    if ($Params.Count -gt 0) {
        $payload.params = $Params
    }

    $json = $payload | ConvertTo-Json -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $WebSocket.SendAsync(
        [ArraySegment[byte]]::new($bytes),
        [System.Net.WebSockets.WebSocketMessageType]::Text,
        $true,
        [Threading.CancellationToken]::None).GetAwaiter().GetResult()

    $buffer = New-Object byte[] 1048576
    $builder = [System.Text.StringBuilder]::new()

    do {
        $result = $WebSocket.ReceiveAsync(
            [ArraySegment[byte]]::new($buffer),
            [Threading.CancellationToken]::None).GetAwaiter().GetResult()

        if ($result.Count -gt 0) {
            [void]$builder.Append([System.Text.Encoding]::UTF8.GetString($buffer, 0, $result.Count))
        }
    } while (-not $result.EndOfMessage)

    $message = $builder.ToString() | ConvertFrom-Json
    while ($message.id -ne $Id) {
        $builder.Clear() | Out-Null
        do {
            $result = $WebSocket.ReceiveAsync(
                [ArraySegment[byte]]::new($buffer),
                [Threading.CancellationToken]::None).GetAwaiter().GetResult()

            if ($result.Count -gt 0) {
                [void]$builder.Append([System.Text.Encoding]::UTF8.GetString($buffer, 0, $result.Count))
            }
        } while (-not $result.EndOfMessage)

        $message = $builder.ToString() | ConvertFrom-Json
    }

    if ($message.error) {
        throw "CDP $Method failed: $($message.error | ConvertTo-Json -Compress)"
    }

    return $message.result
}

function Invoke-CdpEvaluate {
    param(
        [System.Net.WebSockets.ClientWebSocket]$WebSocket,
        [ref]$CommandId,
        [string]$Expression
    )

    $CommandId.Value++
    $result = Send-CdpCommand -WebSocket $WebSocket -Id $CommandId.Value -Method "Runtime.evaluate" -Params @{
        expression = $Expression
        awaitPromise = $true
        returnByValue = $true
    }

    if ($result.exceptionDetails) {
        throw "Runtime.evaluate failed: $($result.exceptionDetails.text)"
    }

    return $result.result.value
}

if (-not $CollectOnly -and -not (Test-Path $PromptPath)) {
    throw "Prompt file not found: $PromptPath"
}

$prompt = if ($CollectOnly) { "" } else { Get-Content -Path $PromptPath -Raw }
$promptJson = $prompt | ConvertTo-Json -Compress

Write-E2ELog "Attaching to existing WebView2 debugger endpoint."
$target = Wait-Until -Description "WebView2 page target" -Seconds 60 -Condition {
    $targets = Invoke-RestMethod -Uri $DebuggerJsonUrl -TimeoutSec 5
    $localGptTarget = $targets |
        Where-Object { $_.type -eq "page" -and $_.webSocketDebuggerUrl -and $_.url -match "127\.0\.0\.1|localhost" } |
        Select-Object -First 1

    if ($localGptTarget) {
        return $localGptTarget
    }

    return $targets |
        Where-Object { $_.type -eq "page" -and $_.webSocketDebuggerUrl } |
        Select-Object -First 1
}

$ws = [System.Net.WebSockets.ClientWebSocket]::new()
$commandId = 0

try {
    $ws.ConnectAsync([Uri]$target.webSocketDebuggerUrl, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
    $commandId++
    Send-CdpCommand -WebSocket $ws -Id $commandId -Method "Runtime.enable" | Out-Null
    $commandId++
    Send-CdpCommand -WebSocket $ws -Id $commandId -Method "Page.enable" | Out-Null

    Write-E2ELog "Ensuring the real visible WebView2 page is on Chat."
    $isOnChat = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "location.pathname.toLowerCase() === '/chat'"
    if (-not $isOnChat) {
        $currentUrl = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "location.href"
        Write-E2ELog "Current visible page is $currentUrl; navigating that same WebView2 host to /Chat."
        Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "location.href = '/Chat'; true" | Out-Null
    }

    Wait-Until -Description "visible WebView2 Chat route" -Seconds 90 -Condition {
        Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "location.pathname.toLowerCase() === '/chat'"
    } | Out-Null

    Wait-Until -Description "LocalGPT E2E helper" -Seconds 90 -Condition {
        Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "!!window.localGptE2e && window.localGptE2e.ping().ready"
    } | Out-Null

    $isChatReady = Wait-Until -Description "interactive DXAiChat" -Seconds 120 -Condition {
        Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.waitForChatInteractive(5000)"
    }

    if (-not $isChatReady) {
        $notReadyState = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "(() => ({ url: location.href, visibleText: window.localGptE2e ? window.localGptE2e.collectVisibleText().slice(0, 2000) : document.body.innerText.slice(0, 2000) }))()"
        $notReadyState | ConvertTo-Json -Depth 8 | Set-Content -Path (Join-Path $artifactRoot "chat-not-ready-state.json") -Encoding utf8
        throw "DXAiChat did not become interactive on the visible WebView2 Chat route. See chat-not-ready-state.json."
    }

    if (-not $CollectOnly) {
        $beforeState = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.chatState()"
        $beforeState | ConvertTo-Json -Depth 8 | Set-Content -Path (Join-Path $artifactRoot "chat-state-before.json") -Encoding utf8

        Write-E2ELog "Writing the prompt into the real DXAiChat input."
        $inputOk = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.setValue('[data-testid=""chat-input""]', $promptJson)"
        if (-not $inputOk) {
            throw "Could not write prompt to DXAiChat input."
        }

        $sendRect = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.sendButtonRect()"
        if (-not $sendRect) {
            throw "DXAiChat send button is unavailable or disabled."
        }

        Write-E2ELog "Clicking the real DXAiChat send button."
        $commandId++
        Send-CdpCommand -WebSocket $ws -Id $commandId -Method "Input.dispatchMouseEvent" -Params @{
            type = "mousePressed"
            x = [double]$sendRect.centerX
            y = [double]$sendRect.centerY
            button = "left"
            clickCount = 1
        } | Out-Null

        $commandId++
        Send-CdpCommand -WebSocket $ws -Id $commandId -Method "Input.dispatchMouseEvent" -Params @{
            type = "mouseReleased"
            x = [double]$sendRect.centerX
            y = [double]$sendRect.centerY
            button = "left"
            clickCount = 1
        } | Out-Null

        Wait-Until -Description "submitted prompt visible or input cleared" -Seconds 30 -Condition {
            Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "(() => { const s = window.localGptE2e.chatState(); return s.inputValue.length === 0 || s.visibleText.includes($promptJson); })()"
        } | Out-Null

        Write-E2ELog "Waiting for council response to make visible progress."
        Start-Sleep -Seconds ([Math]::Min($WaitSeconds, 180))
    }

    $afterState = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.chatState()"
    $afterState | ConvertTo-Json -Depth 8 | Set-Content -Path (Join-Path $artifactRoot "chat-state-after.json") -Encoding utf8

    $visibleText = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.collectVisibleText()"
    $visibleText | Set-Content -Path (Join-Path $artifactRoot "visible-text.txt") -Encoding utf8

    $consoleErrors = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.collectConsoleErrors()"
    $consoleErrors | ConvertTo-Json -Depth 8 | Set-Content -Path (Join-Path $artifactRoot "console-errors.json") -Encoding utf8

    $commandId++
    $screenshot = Send-CdpCommand -WebSocket $ws -Id $commandId -Method "Page.captureScreenshot" -Params @{
        format = "png"
        captureBeyondViewport = $true
    }

    $screenshotPath = Join-Path $artifactRoot "webview2-prompt.png"
    [IO.File]::WriteAllBytes($screenshotPath, [Convert]::FromBase64String($screenshot.data))

    $result = [ordered]@{
        passed = $true
        artifactRoot = $artifactRoot
        promptPath = if ($CollectOnly) { $null } else { (Resolve-Path $PromptPath).Path }
        collectOnly = [bool]$CollectOnly
        pageTarget = $target.url
        screenshot = $screenshotPath
        afterState = $afterState
        visibleTextPreview = ([string]$visibleText).Substring(0, [Math]::Min(2000, ([string]$visibleText).Length))
    }

    $result | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $artifactRoot "result.json") -Encoding utf8
    if ($CollectOnly) {
        Write-E2ELog "WebView2 state collected. Artifact root: $artifactRoot"
    }
    else {
        Write-E2ELog "Prompt sent. Artifact root: $artifactRoot"
    }
}
finally {
    if ($ws.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
        $ws.CloseAsync(
            [System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure,
            "done",
            [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
    }

    $ws.Dispose()
}
