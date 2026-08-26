# AI provider installation profiles

This LocalGPT knowledge article contains **user-maintainable bootstrap profiles** for local AI runtimes. LocalGPT never runs an installation command automatically. Detection/listing is read-only; installation, starting a runtime, and downloading a model require fresh human confirmation through the normal DXFunction/action path.

The command profiles are data, not application policy. A user may update or replace them in the Knowledge Database when provider commands change.

## Ollama — Windows

Source/credit: [Ollama download documentation](https://ollama.com/download/windows).

```
{
  "key": "ollama-windows",
  "displayName": "Ollama",
  "providerKind": "Ollama",
  "platform": "windows",
  "shell": "PowerShell",
  "endpoint": "http://127.0.0.1:11434",
  "sourceUrl": "https://ollama.com/download/windows",
  "detectCommand": "ollama --version",
  "installCommand": "irm https://ollama.com/install.ps1 | iex",
  "startCommand": "ollama serve",
  "listModelsCommand": "ollama list",
  "installModelCommandTemplate": "ollama pull {{model}}",
  "modelAliases": {
    "gpt-oss-20b": "gpt-oss:20b",
    "qwen3-32b": "qwen3:32b",
    "phi-4-14b": "phi4:14b"
  }
}
```

## Ollama — Linux

Source/credit: [Ollama Linux installation documentation](https://ollama.com/download/linux).

```
{
  "key": "ollama-linux",
  "displayName": "Ollama",
  "providerKind": "Ollama",
  "platform": "linux",
  "shell": "Bash",
  "endpoint": "http://127.0.0.1:11434",
  "sourceUrl": "https://ollama.com/download/linux",
  "detectCommand": "ollama --version",
  "installCommand": "curl -fsSL https://ollama.com/install.sh | sh",
  "startCommand": "ollama serve",
  "listModelsCommand": "ollama list",
  "installModelCommandTemplate": "ollama pull {{model}}",
  "modelAliases": {
    "gpt-oss-20b": "gpt-oss:20b",
    "qwen3-32b": "qwen3:32b",
    "phi-4-14b": "phi4:14b"
  }
}
```

## Ollama — macOS

Source/credit: [Ollama download documentation](https://ollama.com/download).

```
{
  "key": "ollama-macos",
  "displayName": "Ollama",
  "providerKind": "Ollama",
  "platform": "macos",
  "shell": "Bash",
  "endpoint": "http://127.0.0.1:11434",
  "sourceUrl": "https://ollama.com/download",
  "detectCommand": "ollama --version",
  "installCommand": "curl -fsSL https://ollama.com/install.sh | sh",
  "startCommand": "ollama serve",
  "listModelsCommand": "ollama list",
  "installModelCommandTemplate": "ollama pull {{model}}",
  "modelAliases": {
    "gpt-oss-20b": "gpt-oss:20b",
    "qwen3-32b": "qwen3:32b",
    "phi-4-14b": "phi4:14b"
  }
}
```

## LM Studio / llmster — Windows

Source/credit: [LM Studio documentation](https://lmstudio.ai/docs/developer/core/headless).

```
{
  "key": "lmstudio-windows",
  "displayName": "LM Studio / llmster",
  "providerKind": "openai-compatible",
  "platform": "windows",
  "shell": "PowerShell",
  "endpoint": "http://127.0.0.1:1234/v1",
  "sourceUrl": "https://lmstudio.ai/docs/developer/core/headless",
  "detectCommand": "lms --help",
  "installCommand": "irm https://lmstudio.ai/install.ps1 | iex",
  "startCommand": "lms daemon up; lms server start",
  "listModelsCommand": "lms ls",
  "installModelCommandTemplate": "lms get {{model}}",
  "modelAliases": {}
}
```

## LM Studio / llmster — Linux

Source/credit: [LM Studio documentation](https://lmstudio.ai/docs/developer/core/headless).

```
{
  "key": "lmstudio-linux",
  "displayName": "LM Studio / llmster",
  "providerKind": "openai-compatible",
  "platform": "linux",
  "shell": "Bash",
  "endpoint": "http://127.0.0.1:1234/v1",
  "sourceUrl": "https://lmstudio.ai/docs/developer/core/headless",
  "detectCommand": "lms --help",
  "installCommand": "curl -fsSL https://lmstudio.ai/install.sh | bash",
  "startCommand": "lms daemon up && lms server start",
  "listModelsCommand": "lms ls",
  "installModelCommandTemplate": "lms get {{model}}",
  "modelAliases": {}
}
```

## LM Studio / llmster — macOS

Source/credit: [LM Studio documentation](https://lmstudio.ai/docs/developer/core/headless).

```
{
  "key": "lmstudio-macos",
  "displayName": "LM Studio / llmster",
  "providerKind": "openai-compatible",
  "platform": "macos",
  "shell": "Bash",
  "endpoint": "http://127.0.0.1:1234/v1",
  "sourceUrl": "https://lmstudio.ai/docs/developer/core/headless",
  "detectCommand": "lms --help",
  "installCommand": "curl -fsSL https://lmstudio.ai/install.sh | bash",
  "startCommand": "lms daemon up && lms server start",
  "listModelsCommand": "lms ls",
  "installModelCommandTemplate": "lms get {{model}}",
  "modelAliases": {}
}
```
