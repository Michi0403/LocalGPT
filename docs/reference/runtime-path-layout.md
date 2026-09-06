# Runtime storage and path layout

LocalGPT keeps mutable application state in the current user's application-data directory by default. The application detects the effective paths at startup, creates the required user-owned structure, and writes `runtime/path-layout.json` so troubleshooting and in-app knowledge can describe the actual installation instead of guessing.

## Default user roots

- **Windows:** `%LOCALAPPDATA%\LocalGPT`
- **macOS:** the current user's `~/Library/Application Support/LocalGPT` application-support location
- **Linux:** `$XDG_DATA_HOME/LocalGPT` when `XDG_DATA_HOME` is configured; otherwise the host LocalApplicationData location with `~/.local/share/LocalGPT` as the durable fallback. This policy applies equally to Debian/Ubuntu, Fedora, Arch, SteamOS and other standard desktop Linux distributions.

The per-user root owns normal mutable state such as `appsettings.user.json`, `localgpt-memory.db`, runtime endpoint/path reports, logs, localization overrides, Council logs/artifacts, workspaces, benchmark evidence, certificates and local knowledge data.

## Portable and system-wide locations

The executable directory and common system-wide locations remain supported for application/tool discovery or explicit user configuration, but LocalGPT does not silently move mutable user configuration into `/usr`, `/opt`, `/Applications`, `Program Files`, `ProgramData`, or the portable application directory.

Provider/tool discovery is separate from LocalGPT storage. Ollama, LM Studio and other runtimes may therefore be found in their normal user-, application-, package-manager-, portable-, or system-wide locations without changing where LocalGPT stores its own configuration and database.

## First boot

At startup LocalGPT records the effective layout in `runtime/path-layout.json`. The `/install` page displays the detected user root, configuration, database, runtime report and portable application root. The same detected layout is seeded into LocalGPT runtime knowledge so setup/troubleshooting answers can reference the current machine even when generated documentation is unavailable.
