param(
    [string]$Configuration = "Debug",
    [string]$Platform = "x64",
    [int]$TimeoutSeconds = 300,
    [string]$Prompt = "LocalGPT WebView2 E2E send smoke. Reply with OK.",
    [switch]$SkipPackageBuild
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$wrapperRoot = Split-Path -Parent $scriptRoot
$repoRoot = Split-Path -Parent $wrapperRoot
$runId = Get-Date -Format "yyyyMMdd-HHmmss"
$artifactRoot = Join-Path $repoRoot "artifacts\e2e\$runId"
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

$runtimeRoot = Join-Path $env:LOCALAPPDATA "LocalGPT\runtime"
New-Item -ItemType Directory -Force -Path $runtimeRoot | Out-Null
$e2eFlagPath = Join-Path $runtimeRoot "localgpt-e2e.flag"
$e2eManifestPath = Join-Path $runtimeRoot "e2e.json"
$serverPath = Join-Path $runtimeRoot "server.json"
$runtimeRoots = @($runtimeRoot)

function Write-E2ELog {
    param([string]$Message)
    $line = "[{0:O}] {1}" -f [DateTimeOffset]::Now, $Message
    Write-Host $line
    Add-Content -Path (Join-Path $artifactRoot "e2e.log") -Value $line
}

function Wait-Until {
    param(
        [scriptblock]$Condition,
        [string]$Description,
        [int]$Seconds = $TimeoutSeconds
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
            # Retry until timeout. The final error is the timeout message.
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Timed out waiting for $Description after $Seconds seconds."
}

function Invoke-JsonGet {
    param([string]$Uri)
    return Invoke-RestMethod -Uri $Uri -TimeoutSec 10
}

function Get-InstalledPackageRuntimeRoots {
    $packageRoot = Join-Path $env:LOCALAPPDATA "Packages"
    if (-not (Test-Path $packageRoot)) {
        return @()
    }

    return Get-ChildItem -Path $packageRoot -Directory -Filter "a6e38587-f17a-4a2e-8022-248694f372b3_*" -ErrorAction SilentlyContinue |
        ForEach-Object { Join-Path $_.FullName "LocalCache\Local\LocalGPT\runtime" }
}

function Read-FirstRuntimeJson {
    param(
        [string[]]$Roots,
        [string]$FileName
    )

    foreach ($root in $Roots) {
        $path = Join-Path $root $FileName
        if (Test-Path $path) {
            return Get-Content $path -Raw | ConvertFrom-Json
        }
    }

    return $null
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
    $sendBuffer = [ArraySegment[byte]]::new($bytes)
    $WebSocket.SendAsync($sendBuffer, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, [Threading.CancellationToken]::None).GetAwaiter().GetResult()

    $buffer = New-Object byte[] 1048576
    $builder = [System.Text.StringBuilder]::new()
    do {
        $receiveBuffer = [ArraySegment[byte]]::new($buffer)
        $result = $WebSocket.ReceiveAsync($receiveBuffer, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
        if ($result.Count -gt 0) {
            [void]$builder.Append([System.Text.Encoding]::UTF8.GetString($buffer, 0, $result.Count))
        }
    } while (-not $result.EndOfMessage)

    $message = $builder.ToString() | ConvertFrom-Json
    while ($message.id -ne $Id) {
        $builder.Clear() | Out-Null
        do {
            $receiveBuffer = [ArraySegment[byte]]::new($buffer)
            $result = $WebSocket.ReceiveAsync($receiveBuffer, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
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

function Wait-CdpExpression {
    param(
        [System.Net.WebSockets.ClientWebSocket]$WebSocket,
        [ref]$CommandId,
        [string]$Expression,
        [string]$Description,
        [int]$Seconds = $TimeoutSeconds
    )

    return Wait-Until -Description $Description -Seconds $Seconds -Condition {
        $value = Invoke-CdpEvaluate -WebSocket $WebSocket -CommandId $CommandId -Expression $Expression
        if ($value) {
            return $value
        }

        return $null
    }
}

try {
    Write-E2ELog "Building LocalGPT Blazor backend."
    dotnet build (Join-Path $wrapperRoot "LocalGPT\LocalGPT.csproj") -c $Configuration -p:Platform=$Platform | Tee-Object -FilePath (Join-Path $artifactRoot "dotnet-build-localgpt.log")
    if ($LASTEXITCODE -ne 0) {
        throw "LocalGPT backend build failed with exit code $LASTEXITCODE."
    }

    Write-E2ELog "Building WebView2 wrapper."
    dotnet build (Join-Path $wrapperRoot "LocalGPTWebviewWrapper\LocalGPTWebviewWrapper.csproj") -c $Configuration -p:Platform=$Platform | Tee-Object -FilePath (Join-Path $artifactRoot "dotnet-build-wrapper.log")
    if ($LASTEXITCODE -ne 0) {
        throw "WebView2 wrapper build failed with exit code $LASTEXITCODE."
    }

    if (-not $SkipPackageBuild) {
        Write-E2ELog "Building package for real WebView2 host launch."
        & (Join-Path $scriptRoot "Build-LocalGptPackage.ps1") -Configuration $Configuration -Platform $Platform | Tee-Object -FilePath (Join-Path $artifactRoot "package-build.log")
        if ($LASTEXITCODE -ne 0) {
            throw "Package build failed with exit code $LASTEXITCODE."
        }

        $package = Get-ChildItem (Join-Path $env:LOCALAPPDATA "Temp\LocalGPTWebviewWrapper\AppPackages") -Recurse -Filter "*.msix" |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
        if (-not $package) {
            throw "Could not find the built LocalGPT MSIX package."
        }

        Write-E2ELog "Installing package $($package.FullName)."
        Add-AppxPackage -ForceApplicationShutdown -Path $package.FullName
    }

    Write-E2ELog "Stopping old LocalGPT WebView2 wrapper processes."
    Stop-Process -Name LocalGPTWebviewWrapper -Force -ErrorAction SilentlyContinue

    $runtimeRoots = @($runtimeRoot) + (Get-InstalledPackageRuntimeRoots)
    $runtimeRoots = $runtimeRoots |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object -Unique

    foreach ($root in $runtimeRoots) {
        New-Item -ItemType Directory -Force -Path $root | Out-Null
        Remove-Item -Path (Join-Path $root "e2e.json") -Force -ErrorAction SilentlyContinue
        Remove-Item -Path (Join-Path $root "e2e-error.json") -Force -ErrorAction SilentlyContinue
        Remove-Item -Path (Join-Path $root "server.json") -Force -ErrorAction SilentlyContinue
        Set-Content -Path (Join-Path $root "localgpt-e2e.flag") -Value "1" -Encoding utf8
    }

    Write-E2ELog "Prepared E2E runtime roots: $($runtimeRoots -join '; ')."

    $app = Get-StartApps | Where-Object { $_.Name -like "LocalGPT*" } | Select-Object -First 1
    if (-not $app) {
        throw "Could not find installed LocalGPT package app id."
    }

    Write-E2ELog "Launching real installed WebView2 host: $($app.AppID)."
    Start-Process "shell:AppsFolder\$($app.AppID)"

    $e2eManifest = Wait-Until -Description "LocalGPT E2E manifest" -Seconds 60 -Condition {
        return Read-FirstRuntimeJson -Roots $runtimeRoots -FileName "e2e.json"
    }
    $e2eManifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $artifactRoot "e2e-manifest.json") -Encoding utf8

    $server = Wait-Until -Description "LocalGPT server.json" -Seconds 60 -Condition {
        return Read-FirstRuntimeJson -Roots $runtimeRoots -FileName "server.json"
    }
    $server | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $artifactRoot "server.json") -Encoding utf8

    Write-E2ELog "Waiting for LocalGPT health endpoint at $($server.BaseUrl)."
    Wait-Until -Description "LocalGPT /health" -Seconds 60 -Condition {
        try {
            return (Invoke-WebRequest -Uri "$($server.BaseUrl)/health" -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200
        }
        catch {
            return $false
        }
    } | Out-Null

    Write-E2ELog "Attaching to WebView2 CDP endpoint 127.0.0.1:9222."
    $targets = Wait-Until -Description "WebView2 remote debugging targets" -Seconds 60 -Condition {
        try {
            $items = Invoke-JsonGet "http://127.0.0.1:9222/json"
            return $items | Where-Object { $_.type -eq "page" -and $_.webSocketDebuggerUrl } | Select-Object -First 1
        }
        catch {
            return $null
        }
    }

    $ws = [System.Net.WebSockets.ClientWebSocket]::new()
    $ws.ConnectAsync([Uri]$targets.webSocketDebuggerUrl, [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
    $commandId = 0

    try {
        $commandId++
        Send-CdpCommand -WebSocket $ws -Id $commandId -Method "Runtime.enable" | Out-Null
        $commandId++
        Send-CdpCommand -WebSocket $ws -Id $commandId -Method "Page.enable" | Out-Null

        Write-E2ELog "Waiting for E2E helper inside the real WebView2 page."
        Wait-CdpExpression -WebSocket $ws -CommandId ([ref]$commandId) -Description "window.localGptE2e" -Seconds 60 -Expression "!!window.localGptE2e && window.localGptE2e.ping().ready" | Out-Null

        $mainText = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.collectVisibleText()"
        if (-not ($mainText -match "LocalGPT|Navigation|Home")) {
            throw "Main page did not expose expected LocalGPT text."
        }

        Write-E2ELog "Navigating through the real UI to the Chat page."
        $clickedChat = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.click('[data-testid=""nav-chat""], a[href*=""/Chat""], a[href*=""/chat""]')"
        if (-not $clickedChat) {
            Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "location.href = '/Chat'; true" | Out-Null
        }

        Wait-CdpExpression -WebSocket $ws -CommandId ([ref]$commandId) -Description "Chat page" -Seconds 90 -Expression "window.localGptE2e.queryExists('[data-testid=""chat-page""]')" | Out-Null
        Wait-CdpExpression -WebSocket $ws -CommandId ([ref]$commandId) -Description "DXAiChat input" -Seconds 90 -Expression "window.localGptE2e.queryExists('[data-testid=""chat-input""]')" | Out-Null
        Wait-CdpExpression -WebSocket $ws -CommandId ([ref]$commandId) -Description "interactive DXAiChat controls" -Seconds 90 -Expression "window.localGptE2e.waitForChatInteractive(60000)" | Out-Null

        $diagLogsBefore = Invoke-JsonGet "$($server.BaseUrl)/__diag/logs?minimumLevel=Debug&take=30"
        $diagLogsBefore | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $artifactRoot "diag-logs-before-send.json") -Encoding utf8

        $promptJson = $Prompt | ConvertTo-Json -Compress
        Write-E2ELog "Typing into the real DXAiChat input."
        $stateBeforeInput = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.chatState()"
        $stateBeforeInput | ConvertTo-Json -Depth 8 | Set-Content -Path (Join-Path $artifactRoot "chat-state-before-input.json") -Encoding utf8

        $inputOk = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.setValue('[data-testid=""chat-input""]', $promptJson)"
        if (-not $inputOk) {
            throw "Could not set DXAiChat input value."
        }

        $inputValue = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "document.querySelector('[data-testid=""chat-input""]')?.value"
        if ($inputValue -ne $Prompt) {
            throw "DXAiChat input value was not retained."
        }

        $stateBeforeSend = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.chatState()"
        $stateBeforeSend | ConvertTo-Json -Depth 8 | Set-Content -Path (Join-Path $artifactRoot "chat-state-before-send.json") -Encoding utf8

        $sendRect = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.sendButtonRect()"
        if (-not $sendRect) {
            throw "DXAiChat send button was not available or was disabled."
        }

        Write-E2ELog "Pressing the real DXAiChat send button."
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

        $submitted = Wait-CdpExpression -WebSocket $ws -CommandId ([ref]$commandId) -Description "DXAiChat submitted message" -Seconds 30 -Expression "(() => { const state = window.localGptE2e.chatState(); return state.inputValue !== $promptJson || state.visibleText.includes($promptJson); })()"
        if (-not $submitted) {
            throw "DXAiChat send did not submit the typed prompt."
        }

        $stateAfterSend = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.chatState()"
        $stateAfterSend | ConvertTo-Json -Depth 8 | Set-Content -Path (Join-Path $artifactRoot "chat-state-after-send.json") -Encoding utf8

        if ($stateAfterSend.overlayVisible -or -not $stateAfterSend.interactiveReady) {
            throw "DXAiChat submitted, but the interactive startup overlay stayed visible; Blazor readiness is broken."
        }

        $sendExists = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.queryExists('[data-testid=""send-button""]')"
        $consoleErrors = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.collectConsoleErrors()"
        $visibleText = Invoke-CdpEvaluate -WebSocket $ws -CommandId ([ref]$commandId) -Expression "window.localGptE2e.collectVisibleText()"

        $commandId++
        $screenshotResult = Send-CdpCommand -WebSocket $ws -Id $commandId -Method "Page.captureScreenshot" -Params @{
            format = "png"
            captureBeyondViewport = $true
        }
        $screenshotPath = Join-Path $artifactRoot "webview2-chat.png"
        [IO.File]::WriteAllBytes($screenshotPath, [Convert]::FromBase64String($screenshotResult.data))

        $diagLogs = Invoke-JsonGet "$($server.BaseUrl)/__diag/logs?minimumLevel=Warning&take=30"
        $diagLogsAfter = Invoke-JsonGet "$($server.BaseUrl)/__diag/logs?minimumLevel=Debug&take=50"
        $diagLogs | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $artifactRoot "diag-logs-warning.json") -Encoding utf8
        $diagLogsAfter | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $artifactRoot "diag-logs-after-send.json") -Encoding utf8

        $criticalConsoleErrors = @($consoleErrors | Where-Object { $_ -and ($_ -notmatch "favicon") })
        $visibleTextValue = if ($null -eq $visibleText) { "" } else { [string]$visibleText }
        $result = [ordered]@{
            passed = $true
            runId = $runId
            baseUrl = $server.BaseUrl
            e2eManifest = $e2eManifest
            pageTarget = $targets.url
            mainPageTextMatched = $true
            chatInputSet = $true
            sendButtonClicked = $true
            messageSubmitted = [bool]$submitted
            interactiveReady = -not $stateAfterSend.overlayVisible -and [bool]$stateAfterSend.interactiveReady
            sendButtonFound = [bool]$sendExists
            stateBeforeInput = $stateBeforeInput
            stateBeforeSend = $stateBeforeSend
            stateAfterSend = $stateAfterSend
            consoleErrors = $consoleErrors
            criticalConsoleErrorCount = $criticalConsoleErrors.Count
            warningLogCount = $diagLogs.count
            screenshot = $screenshotPath
            visibleTextPreview = $visibleTextValue.Substring(0, [Math]::Min(1200, $visibleTextValue.Length))
        }

        if ($criticalConsoleErrors.Count -gt 0) {
            $result.passed = $false
            $result.failure = "Critical console errors were captured."
        }

        $result | ConvertTo-Json -Depth 12 | Set-Content -Path (Join-Path $artifactRoot "result.json") -Encoding utf8
        if (-not $result.passed) {
            throw $result.failure
        }

        Write-E2ELog "Real WebView2 E2E passed. Screenshot: $screenshotPath"
    }
    finally {
        if ($ws.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
            $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "done", [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
        }

        $ws.Dispose()
    }
}
catch {
    $failure = [ordered]@{
        passed = $false
        runId = $runId
        error = $_.Exception.Message
        artifactRoot = $artifactRoot
    }
    $failure | ConvertTo-Json -Depth 8 | Set-Content -Path (Join-Path $artifactRoot "result.json") -Encoding utf8
    Write-E2ELog "FAILED: $($_.Exception.Message)"
    throw
}
