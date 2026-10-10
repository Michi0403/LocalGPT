# LocalGPT 5.4.7 — ASCII fullscreen and operator timeout repair

- Fix the runtime-policy default of `int.MaxValue` seconds causing `CancellationTokenSource.CancelAfter(TimeSpan)` to throw `ArgumentOutOfRangeException` before operator commands could reach PowerShell, bash or other detected shells. Maintain cancellation and bounded explicit timeouts.
- Restore fullscreen height across the actual DevExpress `DxPopup` / `DxFormLayout` ancestry, including both game and Operator stages. Scope layout rules to the browser-fullscreen ASCII popup only; keep the user-selected ASCII/Pixel and Fit/Width/Native rendering modes.
- Make closing the game console a single awaited renderer-owned visibility operation; remove an independently scheduled background render which could leave a blank popup after fullscreen exit.
- Preserve the human-confirmed local shell service, console output stream, job cancellation, security boundaries, database-backed policy, and running Council/chat state. No shell access is added for models by these fixes.
- No .NET build/restore/publish or GitHub calls were performed; source audits and runtime/browser smoke tests are tracked separately.
- Treat human-entered `cd` as an Operator session working-directory change, including quoted names and `~` paths, so subsequent commands use the selected directory even though each shell command is a separate supervised process.
- Added `build/audit_ascii_terminal_shell_fullscreen_5_4_7.py` to cover the timeout boundary, shell execution, renderer lifecycle and popup-height ancestry.
