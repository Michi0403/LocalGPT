# LocalGPT 3.8.4 — per-user data and first-boot path contract

LocalGPT 3.8.4 centralizes mutable application storage behind the existing platform boundary while preserving the established Windows `%LOCALAPPDATA%\LocalGPT` default.

- Per-user writable state is authoritative by default on Windows, macOS and Linux.
- Windows keeps `%LOCALAPPDATA%\LocalGPT`; macOS resolves the user's Application Support location; Linux honors the host LocalApplicationData/XDG data location with `~/.local/share` as the durable fallback.
- Provider/tool discovery remains independent from LocalGPT storage, so Ollama/LM Studio user, portable, package-manager and system-wide locations are still discoverable.
- First boot creates and records the effective folder layout in `runtime/path-layout.json` and surfaces it on `/install`.
- The detected layout is added to initial LocalGPT knowledge so setup/help can answer path questions without depending on generated documentation.
- Existing mutable LocalGPT services now use the canonical per-user root rather than repeating raw `LocalApplicationData` combinations.
- The database path remains configurable; the detected report records the effective configured database path.
