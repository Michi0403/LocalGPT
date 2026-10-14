# LocalGPT 4.1.0 - Matrix operator control plane

## Added

- Extended the existing black/green `ChatGameConsole` with a toggleable **MATRIX OPERATOR** mode instead of opening or embedding an operating-system terminal window.
- Added `IConsoleOperatorService` as the service-owned parser for LocalGPT meta commands, keeping control commands above the selected shell backend.
- Extended shell discovery with Auto, zsh, bash, POSIX sh, pwsh/PowerShell, and cmd backends where the current host actually provides them.
- Added supervised background console operations with stable operation IDs, process IDs when available, bounded status, cooperative cancellation, and host-aware signal delivery.
- Added LocalGPT meta commands for `:help`, `:shells`, `:shell`, `:jobs`, `:cancel`, `:signal`, `:cd`, and `:clear`.
- Preserved active ASCII-game state while switching the shared wall into operator mode; game keyboard/gamepad capture is suspended only while the operator row owns the surface.
- Added a dependency-light Matrix operator layer to `LocalGPTInstallerConsole` with `:operator on|off`, shell selection/discovery, job inspection, setup cancellation, process signaling, and redirected shell output.

## Cancellation and process ownership

- Ctrl+C and `:cancel` request one setup-wide cancellation token rather than merely changing console text.
- Installer downloads, retry delays, startup stages, and spawned setup tools now observe that token.
- Cancellation now bypasses legacy generic error/retry catches in install, model-pull, learning-source, release-download, file-move, and process-wait paths, preserving the dedicated setup-cancel exit instead of reporting cancellation as an ordinary failure.
- Setup-owned child processes are registered in the operator process table and are terminated on setup cancellation on a best-effort basis.
- Human-started operator shell jobs are marked separately and remain independently signalable; setup cancellation does not automatically classify them as installer children.
- Explicit `pid:<n>` signal targeting remains an intentional operator action, while bare numeric PIDs must already be tracked by the setup session.
- Redirected shell jobs use `CreateNoWindow=true`; no external Terminal, PowerShell, cmd, or shell window is launched by the operator layer.

## Architecture

- Host shell executable resolution and OS signal semantics remain behind `ILocalConsolePlatformService`; common operator parsing contains no OS branching.
- `ConsoleCommandService` retains bounded/sanitized output and uses `ISupervisedTaskRunner` for background ownership.
- The cross-platform audit now explicitly recognizes `LocalConsolePlatformServices.cs` as the concrete Windows/Unix platform implementation boundary while continuing to reject OS branching elsewhere.
- Command text remains omitted from operational diagnostics.

## Preserved

- InteractiveServer renderer-affinity policy remains intact; new non-lifecycle operator handlers resume off-renderer and marshal UI state updates through `InvokeAsync`.
- The existing ASCII games, game snapshots, console history limits, runtime policy, application architecture, Council wiring, localization and persistence behavior remain unchanged.
- The 4.0.9 macOS signing/PDF-render repair, 4.0.8 documentation-PDF hygiene, and 4.0.7 installer preflight remain present.
