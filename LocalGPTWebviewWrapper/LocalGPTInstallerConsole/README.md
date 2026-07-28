# LocalGPT Installer Console

C# console helper for LocalGPT setup, update, startup and maintenance.

Running it without arguments starts the default preservation-first install and update routine. On Windows it installs or verifies Ollama, checks and pulls the Slim minimal model set, restores the maintained shortcuts, installs or updates LocalGPT, and starts the application. It does not delete the existing LocalAppData installation. Destructive deletion remains limited to an explicit uninstall/force command.

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

The no-command routine checks and pulls the Slim minimal model set. Other ranges require an explicit `--pull-models --range ...` selection.

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
localgpt-setup --install-localgpt
localgpt-setup --import-recommended
localgpt-setup --uninstall
localgpt-setup --uninstall --force-delete
```

## Build

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:PublishReadyToRun=false
```
