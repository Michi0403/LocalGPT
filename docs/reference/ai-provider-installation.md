# AI provider installation profiles

This LocalGPT knowledge article contains **user-maintainable bootstrap profiles** for local AI runtimes. LocalGPT never runs an installation command automatically. Detection/listing is read-only; installation, starting a runtime, and downloading a model require fresh human confirmation through the normal DXFunction/action path.

The command profiles are data, not application policy. A user may update or replace them in the Knowledge Database when provider commands change. Profiles may define a separate `updateCommand`; older customized profiles remain compatible because LocalGPT falls back to their reviewed install command when no update command exists.

## Guided Ollama setup in `/install`

The interactive setup assistant keeps the individual Detect, Install, Update, Start/Stop/Restart, and Register actions, and also offers one explicit **Set up Ollama** action for the common first-run path. That click is the user's confirmation for the bounded sequence: install only when Ollama is missing, resolve the executable again, start it only when it is stopped, then register the maintained loopback endpoint in LocalGPT. Completed steps are not rolled back when a later step needs attention, so retrying is additive rather than destructive.

On macOS, LocalGPT checks the native application bundle, Homebrew locations, common local paths, and `PATH`. If the maintained installer completes but the executable is still not discoverable, the assistant stops instead of pretending setup succeeded. Open the Ollama application/provider source once when macOS requires that first launch, then run the guided action again; LocalGPT will continue from the detected state.

When an Ollama model pull explicitly reports that the installed runtime is too old for that model, LocalGPT does not silently reuse the model-install approval to mutate the runtime. The setup assistant exposes a separate **Update Ollama** action. If Ollama was running, LocalGPT stops it before the reviewed update command, re-detects the executable, and starts it again after a successful update; a runtime that was already stopped stays stopped. Older user-maintained profiles without `updateCommand` continue to use their reviewed install command as the update fallback.

The shared ASCII command console keeps provider progress useful without reproducing terminal control noise. ANSI/OSC cursor and spinner rewrites remain filtered, while bounded numeric percentage snapshots such as `42%` are retained and rendered at a coalesced rate so long model downloads remain visibly alive without flooding the InteractiveServer circuit.

Model downloads remain separate explicit actions. Hardware-fit recommendations that already have a conservative provider mapping can be installed directly. Unmapped CanIRun.ai recommendations can explicitly query the selected provider's official catalog; LocalGPT only enables installation after one unique conservative provider identity match and leaves ambiguous results review-only.

## Ollama — Windows

Source/credit: [Ollama download documentation](https://ollama.com/download/windows).

On Windows, the guided Ollama install/update path intentionally downloads the official `OllamaSetup.exe` directly with a streaming `HttpClient` transfer, a 4 MiB buffer, and bounded integer percentage output before launching the vendor installer with `/SILENT`. This mirrors the fast HTTP pattern already used by `LocalGPTInstallerConsole` and avoids delegating the large-file transfer to the vendor PowerShell script path that was slow in the reported runtime test. The public vendor URL remains `https://ollama.com/download/OllamaSetup.exe`; redirects stay vendor-controlled. Manual users can still use Ollama's documented `irm https://ollama.com/install.ps1 | iex` command.

```localgpt-provider-profile
{
  "key": "ollama-windows",
  "displayName": "Ollama",
  "providerKind": "Ollama",
  "platform": "windows",
  "shell": "PowerShell",
  "endpoint": "http://127.0.0.1:11434",
  "sourceUrl": "https://ollama.com/download/windows",
  "modelCatalogUrl": "https://ollama.com/search",
  "detectCommand": "ollama --version",
  "installCommand": "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; $url='https://ollama.com/download/OllamaSetup.exe'; $target=Join-Path ([IO.Path]::GetTempPath()) ('OllamaSetup-'+[Guid]::NewGuid().ToString('N')+'.exe'); $handler=$null; $client=$null; $request=$null; $response=$null; $input=$null; $output=$null; try { Write-Output '>>> Downloading Ollama for Windows with LocalGPT fast HTTP...'; $handler=[Net.Http.HttpClientHandler]::new(); $handler.AllowAutoRedirect=$true; $client=[Net.Http.HttpClient]::new($handler); $client.Timeout=[TimeSpan]::FromMinutes(30); $client.DefaultRequestHeaders.UserAgent.ParseAdd('LocalGPT/4.0.1'); $request=[Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Get,$url); $response=$client.SendAsync($request,[Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult(); $response.EnsureSuccessStatusCode(); [long]$total=0; if ($null -ne $response.Content.Headers.ContentLength) { $total=[long]$response.Content.Headers.ContentLength }; $input=$response.Content.ReadAsStreamAsync().GetAwaiter().GetResult(); $output=[IO.FileStream]::new($target,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None,4194304,[IO.FileOptions]::SequentialScan); $buffer=[byte[]]::new(4194304); [long]$downloaded=0; [int]$lastPercent=-1; while (($read=$input.Read($buffer,0,$buffer.Length)) -gt 0) { $output.Write($buffer,0,$read); $downloaded += $read; if ($total -gt 0) { $percent=[Math]::Min(100,[int][Math]::Floor(($downloaded*100.0)/$total)); if ($percent -ne $lastPercent) { Write-Output ('>>> Ollama download {0}%' -f $percent); $lastPercent=$percent } } }; $output.Flush(); if ($total -gt 0 -and $downloaded -ne $total) { throw ('Incomplete Ollama download: {0} of {1} bytes.' -f $downloaded,$total) }; Write-Output ('>>> Ollama download complete: {0:N0} bytes' -f $downloaded); $output.Dispose(); $output=$null; $input.Dispose(); $input=$null; $response.Dispose(); $response=$null; $request.Dispose(); $request=$null; $client.Dispose(); $client=$null; $handler.Dispose(); $handler=$null; Write-Output '>>> Installing Ollama...'; $installer=Start-Process -FilePath $target -ArgumentList '/SILENT' -Wait -PassThru; if ($installer.ExitCode -ne 0) { throw ('Ollama installer exited with code {0}.' -f $installer.ExitCode) }; Write-Output '>>> Ollama installation/update completed.' } finally { if ($output) { $output.Dispose() }; if ($input) { $input.Dispose() }; if ($response) { $response.Dispose() }; if ($request) { $request.Dispose() }; if ($client) { $client.Dispose() }; if ($handler) { $handler.Dispose() }; if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Force -ErrorAction SilentlyContinue } }",
  "updateCommand": "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; $url='https://ollama.com/download/OllamaSetup.exe'; $target=Join-Path ([IO.Path]::GetTempPath()) ('OllamaSetup-'+[Guid]::NewGuid().ToString('N')+'.exe'); $handler=$null; $client=$null; $request=$null; $response=$null; $input=$null; $output=$null; try { Write-Output '>>> Downloading Ollama for Windows with LocalGPT fast HTTP...'; $handler=[Net.Http.HttpClientHandler]::new(); $handler.AllowAutoRedirect=$true; $client=[Net.Http.HttpClient]::new($handler); $client.Timeout=[TimeSpan]::FromMinutes(30); $client.DefaultRequestHeaders.UserAgent.ParseAdd('LocalGPT/4.0.1'); $request=[Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Get,$url); $response=$client.SendAsync($request,[Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult(); $response.EnsureSuccessStatusCode(); [long]$total=0; if ($null -ne $response.Content.Headers.ContentLength) { $total=[long]$response.Content.Headers.ContentLength }; $input=$response.Content.ReadAsStreamAsync().GetAwaiter().GetResult(); $output=[IO.FileStream]::new($target,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None,4194304,[IO.FileOptions]::SequentialScan); $buffer=[byte[]]::new(4194304); [long]$downloaded=0; [int]$lastPercent=-1; while (($read=$input.Read($buffer,0,$buffer.Length)) -gt 0) { $output.Write($buffer,0,$read); $downloaded += $read; if ($total -gt 0) { $percent=[Math]::Min(100,[int][Math]::Floor(($downloaded*100.0)/$total)); if ($percent -ne $lastPercent) { Write-Output ('>>> Ollama download {0}%' -f $percent); $lastPercent=$percent } } }; $output.Flush(); if ($total -gt 0 -and $downloaded -ne $total) { throw ('Incomplete Ollama download: {0} of {1} bytes.' -f $downloaded,$total) }; Write-Output ('>>> Ollama download complete: {0:N0} bytes' -f $downloaded); $output.Dispose(); $output=$null; $input.Dispose(); $input=$null; $response.Dispose(); $response=$null; $request.Dispose(); $request=$null; $client.Dispose(); $client=$null; $handler.Dispose(); $handler=$null; Write-Output '>>> Installing Ollama...'; $installer=Start-Process -FilePath $target -ArgumentList '/SILENT' -Wait -PassThru; if ($installer.ExitCode -ne 0) { throw ('Ollama installer exited with code {0}.' -f $installer.ExitCode) }; Write-Output '>>> Ollama installation/update completed.' } finally { if ($output) { $output.Dispose() }; if ($input) { $input.Dispose() }; if ($response) { $response.Dispose() }; if ($request) { $request.Dispose() }; if ($client) { $client.Dispose() }; if ($handler) { $handler.Dispose() }; if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Force -ErrorAction SilentlyContinue } }",
  "startCommand": "ollama serve",
  "listModelsCommand": "ollama list",
  "installModelCommandTemplate": "ollama pull {{model}}",
  "modelAliases": {
    "gpt-oss-20b": "gpt-oss:20b",
    "gpt-oss-120b": "gpt-oss:120b",
    "qwen3-32b": "qwen3:32b",
    "qwen3.5-9b": "qwen3.5:9b",
    "qwen2.5-coder-32b": "qwen2.5-coder:32b",
    "llama3.1-8b": "llama3.1:8b",
    "llama3.3-70b": "llama3.3:70b",
    "deepseek-r1-7b": "deepseek-r1:7b",
    "deepseek-r1-32b": "deepseek-r1:32b",
    "gemma3-27b": "gemma3:27b",
    "phi-4-14b": "phi4:14b"
  }
}
```

## Ollama — Linux

Source/credit: [Ollama Linux installation documentation](https://ollama.com/download/linux).

```localgpt-provider-profile
{
  "key": "ollama-linux",
  "displayName": "Ollama",
  "providerKind": "Ollama",
  "platform": "linux",
  "shell": "Bash",
  "endpoint": "http://127.0.0.1:11434",
  "sourceUrl": "https://ollama.com/download/linux",
  "modelCatalogUrl": "https://ollama.com/search",
  "detectCommand": "ollama --version",
  "installCommand": "curl -fsSL https://ollama.com/install.sh | sh",
  "updateCommand": "curl -fsSL https://ollama.com/install.sh | sh",
  "startCommand": "ollama serve",
  "listModelsCommand": "ollama list",
  "installModelCommandTemplate": "ollama pull {{model}}",
  "modelAliases": {
    "gpt-oss-20b": "gpt-oss:20b",
    "gpt-oss-120b": "gpt-oss:120b",
    "qwen3-32b": "qwen3:32b",
    "qwen3.5-9b": "qwen3.5:9b",
    "qwen2.5-coder-32b": "qwen2.5-coder:32b",
    "llama3.1-8b": "llama3.1:8b",
    "llama3.3-70b": "llama3.3:70b",
    "deepseek-r1-7b": "deepseek-r1:7b",
    "deepseek-r1-32b": "deepseek-r1:32b",
    "gemma3-27b": "gemma3:27b",
    "phi-4-14b": "phi4:14b"
  }
}
```

## Ollama — macOS

Source/credit: [Ollama download documentation](https://ollama.com/download).

```localgpt-provider-profile
{
  "key": "ollama-macos",
  "displayName": "Ollama",
  "providerKind": "Ollama",
  "platform": "macos",
  "shell": "Bash",
  "endpoint": "http://127.0.0.1:11434",
  "sourceUrl": "https://ollama.com/download",
  "modelCatalogUrl": "https://ollama.com/search",
  "detectCommand": "ollama --version",
  "installCommand": "curl -fsSL https://ollama.com/install.sh | sh",
  "updateCommand": "curl -fsSL https://ollama.com/install.sh | sh",
  "startCommand": "ollama serve",
  "listModelsCommand": "ollama list",
  "installModelCommandTemplate": "ollama pull {{model}}",
  "modelAliases": {
    "gpt-oss-20b": "gpt-oss:20b",
    "gpt-oss-120b": "gpt-oss:120b",
    "qwen3-32b": "qwen3:32b",
    "qwen3.5-9b": "qwen3.5:9b",
    "qwen2.5-coder-32b": "qwen2.5-coder:32b",
    "llama3.1-8b": "llama3.1:8b",
    "llama3.3-70b": "llama3.3:70b",
    "deepseek-r1-7b": "deepseek-r1:7b",
    "deepseek-r1-32b": "deepseek-r1:32b",
    "gemma3-27b": "gemma3:27b",
    "phi-4-14b": "phi4:14b"
  }
}
```

## LM Studio / llmster — Windows

Source/credit: [LM Studio documentation](https://lmstudio.ai/docs/developer/core/headless).

```localgpt-provider-profile
{
  "key": "lmstudio-windows",
  "displayName": "LM Studio / llmster",
  "providerKind": "openai-compatible",
  "platform": "windows",
  "shell": "PowerShell",
  "endpoint": "http://127.0.0.1:1234/v1",
  "sourceUrl": "https://lmstudio.ai/docs/developer/core/headless",
  "modelCatalogUrl": "https://lmstudio.ai/models",
  "detectCommand": "lms --help",
  "installCommand": "irm https://lmstudio.ai/install.ps1 | iex",
  "startCommand": "lms daemon up; lms server start",
  "listModelsCommand": "lms ls",
  "installModelCommandTemplate": "lms get {{model}}; if ($LASTEXITCODE -eq 0) { lms load {{model}} }",
  "modelAliases": {
    "gpt-oss-20b": "openai/gpt-oss-20b",
    "gpt-oss-120b": "openai/gpt-oss-120b",
    "qwen3.5-2b": "qwen/qwen3.5-2b",
    "qwen3.5-4b": "qwen/qwen3.5-4b",
    "qwen3.5-9b": "qwen/qwen3.5-9b",
    "qwen3.5-27b": "qwen/qwen3.5-27b",
    "qwen3.5-35b": "qwen/qwen3.5-35b-a3b",
    "qwen3-4b": "qwen/qwen3-4b-2507",
    "qwen3-30b": "qwen/qwen3-30b-a3b-2507",
    "deepseek-r1-7b": "deepseek/deepseek-r1-distill-qwen-7b",
    "deepseek-r1-8b": "deepseek/deepseek-r1-distill-llama-8b",
    "deepseek-r1-14b": "deepseek/deepseek-r1-distill-qwen-14b",
    "deepseek-r1-32b": "deepseek/deepseek-r1-distill-qwen-32b"
  }
}
```

## LM Studio / llmster — Linux

Source/credit: [LM Studio documentation](https://lmstudio.ai/docs/developer/core/headless).

```localgpt-provider-profile
{
  "key": "lmstudio-linux",
  "displayName": "LM Studio / llmster",
  "providerKind": "openai-compatible",
  "platform": "linux",
  "shell": "Bash",
  "endpoint": "http://127.0.0.1:1234/v1",
  "sourceUrl": "https://lmstudio.ai/docs/developer/core/headless",
  "modelCatalogUrl": "https://lmstudio.ai/models",
  "detectCommand": "lms --help",
  "installCommand": "curl -fsSL https://lmstudio.ai/install.sh | bash",
  "startCommand": "lms daemon up && lms server start",
  "listModelsCommand": "lms ls",
  "installModelCommandTemplate": "lms get {{model}} && lms load {{model}}",
  "modelAliases": {
    "gpt-oss-20b": "openai/gpt-oss-20b",
    "gpt-oss-120b": "openai/gpt-oss-120b",
    "qwen3.5-2b": "qwen/qwen3.5-2b",
    "qwen3.5-4b": "qwen/qwen3.5-4b",
    "qwen3.5-9b": "qwen/qwen3.5-9b",
    "qwen3.5-27b": "qwen/qwen3.5-27b",
    "qwen3.5-35b": "qwen/qwen3.5-35b-a3b",
    "qwen3-4b": "qwen/qwen3-4b-2507",
    "qwen3-30b": "qwen/qwen3-30b-a3b-2507",
    "deepseek-r1-7b": "deepseek/deepseek-r1-distill-qwen-7b",
    "deepseek-r1-8b": "deepseek/deepseek-r1-distill-llama-8b",
    "deepseek-r1-14b": "deepseek/deepseek-r1-distill-qwen-14b",
    "deepseek-r1-32b": "deepseek/deepseek-r1-distill-qwen-32b"
  }
}
```

## LM Studio / llmster — macOS

Source/credit: [LM Studio documentation](https://lmstudio.ai/docs/developer/core/headless).

```localgpt-provider-profile
{
  "key": "lmstudio-macos",
  "displayName": "LM Studio / llmster",
  "providerKind": "openai-compatible",
  "platform": "macos",
  "shell": "Bash",
  "endpoint": "http://127.0.0.1:1234/v1",
  "sourceUrl": "https://lmstudio.ai/docs/developer/core/headless",
  "modelCatalogUrl": "https://lmstudio.ai/models",
  "detectCommand": "lms --help",
  "installCommand": "curl -fsSL https://lmstudio.ai/install.sh | bash",
  "startCommand": "lms daemon up && lms server start",
  "listModelsCommand": "lms ls",
  "installModelCommandTemplate": "lms get {{model}} && lms load {{model}}",
  "modelAliases": {
    "gpt-oss-20b": "openai/gpt-oss-20b",
    "gpt-oss-120b": "openai/gpt-oss-120b",
    "qwen3.5-2b": "qwen/qwen3.5-2b",
    "qwen3.5-4b": "qwen/qwen3.5-4b",
    "qwen3.5-9b": "qwen/qwen3.5-9b",
    "qwen3.5-27b": "qwen/qwen3.5-27b",
    "qwen3.5-35b": "qwen/qwen3.5-35b-a3b",
    "qwen3-4b": "qwen/qwen3-4b-2507",
    "qwen3-30b": "qwen/qwen3-30b-a3b-2507",
    "deepseek-r1-7b": "deepseek/deepseek-r1-distill-qwen-7b",
    "deepseek-r1-8b": "deepseek/deepseek-r1-distill-llama-8b",
    "deepseek-r1-14b": "deepseek/deepseek-r1-distill-qwen-14b",
    "deepseek-r1-32b": "deepseek/deepseek-r1-distill-qwen-32b"
  }
}
```
