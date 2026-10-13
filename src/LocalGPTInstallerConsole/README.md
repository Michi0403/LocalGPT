# LocalGPT Installer Console

Argument-driven C# console variant of the original PowerShell workflow.

## Ollama install behavior

The default Windows setup installs/detects Ollama and LocalGPT without downloading any AI models. The first-run guide directs users to `/install`, where they choose models and install them through Ollama or LM Studio. The explicit `--pull-models` command and separate `Pull-Models-*.cmd` utilities remain available for intentional manual use. `--all` also does not download models.

This version uses the official Windows EXE installer directly:

```powershell
localgpt-setup --install-ollama
```

It downloads:

```text
https://ollama.com/download/OllamaSetup.exe
```

After install it resolves `ollama.exe` from:

1. `--ollama-exe <path>`
2. `PATH`
3. `%LOCALAPPDATA%\Programs\Ollama\ollama.exe`
4. `%ProgramFiles%\Ollama\ollama.exe`
5. `%ProgramFiles(x86)%\Ollama\ollama.exe`

The Windows EXE installer normally installs to `%LOCALAPPDATA%\Programs\Ollama` and adds that folder to the user `PATH`.

## Examples

```powershell
localgpt-setup --install-ollama
localgpt-setup --pull-models --range Slim
localgpt-setup --pull-models --range RTX3060 --ollama-exe "$env:LOCALAPPDATA\Programs\Ollama\ollama.exe"
localgpt-setup --install-localgpt --force
localgpt-setup --import-recommended --force
```

## Build

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```
