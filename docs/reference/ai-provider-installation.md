# AI provider installation profiles

This LocalGPT knowledge article contains **user-maintainable bootstrap profiles** for local AI runtimes. LocalGPT never runs an installation command automatically. Detection/listing is read-only; installation, starting a runtime, and downloading a model require fresh human confirmation through the normal DXFunction/action path.

The command profiles are data, not application policy. A user may update or replace them in the Knowledge Database when provider commands change.

## Guided Ollama setup in `/install`

The interactive setup assistant keeps the individual Detect, Install, Start/Stop/Restart, and Register actions, and also offers one explicit **Set up Ollama** action for the common first-run path. That click is the user's confirmation for the bounded sequence: install only when Ollama is missing, resolve the executable again, start it only when it is stopped, then register the maintained loopback endpoint in LocalGPT. Completed steps are not rolled back when a later step needs attention, so retrying is additive rather than destructive.

On macOS, LocalGPT checks the native application bundle, Homebrew locations, common local paths, and `PATH`. If the maintained installer completes but the executable is still not discoverable, the assistant stops instead of pretending setup succeeded. Open the Ollama application/provider source once when macOS requires that first launch, then run the guided action again; LocalGPT will continue from the detected state.

Model downloads remain separate explicit actions. Hardware-fit recommendations that already have a conservative provider mapping can be installed directly. Unmapped CanIRun.ai recommendations can explicitly query the selected provider's official catalog; LocalGPT only enables installation after one unique conservative provider identity match and leaves ambiguous results review-only.

## Ollama — Windows

Source/credit: [Ollama download documentation](https://ollama.com/download/windows).

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
  "installCommand": "irm https://ollama.com/install.ps1 | iex",
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
