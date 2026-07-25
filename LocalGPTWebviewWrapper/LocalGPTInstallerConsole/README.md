# LocalGPT Installer Console

Argument-driven C# console helper for explicit LocalGPT setup and maintenance.

Running it without arguments prints help and performs no installation, download, model pull, deletion, or process start. Review every target path and option before confirming a destructive operation.

## Ollama installation

This helper can download the official Windows installer when the user explicitly requests:

```powershell
localgpt-setup --install-ollama
```

After installation it resolves `ollama.exe` from:

1. `--ollama-exe <path>`;
2. `PATH`;
3. `%LOCALAPPDATA%\Programs\Ollama\ollama.exe`;
4. `%ProgramFiles%\Ollama\ollama.exe`;
5. `%ProgramFiles(x86)%\Ollama\ollama.exe`.

No model is pulled by default. Model pulls require explicit `--pull-models` options.

## Safety behavior

- release downloads require an exact platform, architecture, and setup-mode match;
- browser download URLs must use HTTPS on GitHub;
- download and extraction failures return failure rather than continuing as success;
- ZIP traversal and symbolic-link entries are rejected;
- unsafe deletion targets and Start Menu traversal are rejected;
- uninstall removes LocalGPT application files, launchers, and shortcuts but preserves the learning base, including forced uninstall;
- no external extraction executable is launched as a fallback.

## Examples

```powershell
localgpt-setup --install-ollama
localgpt-setup --pull-models --range Slim
localgpt-setup --pull-models --range RTX3060 --ollama-exe "$env:LOCALAPPDATA\Programs\Ollama\ollama.exe"
localgpt-setup --install-localgpt --force
localgpt-setup --import-recommended --force
localgpt-setup --uninstall
localgpt-setup --uninstall --force-delete
```

## Build

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```
