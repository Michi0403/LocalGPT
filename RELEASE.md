# LocalGPT 4.1.0

LocalGPT 4.1.0 turns the existing black/green ASCII game console into a reusable Matrix-style operator control plane and brings the same operator vocabulary to the setup console.

Inside LocalGPT, the existing `ChatGameConsole` can switch between GAME and MATRIX OPERATOR without destroying the active game snapshot or background command state. The operator row discovers host shells through the existing platform-service boundary, supports Auto/zsh/bash/sh/pwsh/PowerShell/cmd where actually available, and keeps LocalGPT-owned meta controls above the selected shell. Normal command lines execute through the bounded supervised console service; active jobs expose stable operation IDs and PIDs without writing command text to diagnostics.

The operator vocabulary includes `:help`, `:shells`, `:shell`, `:jobs`, `:cancel`, `:signal`, `:cd`, and `:clear`. Host-supported signals are delivered through platform services. The LocalGPT UI remains the visible terminal surface; shell processes use redirected I/O and do not open an external terminal window.

The setup console now exposes the same Matrix-style control idea while installation work is running. `:operator on|off` controls ordinary shell forwarding while meta controls remain reachable, Ctrl+C routes into cooperative setup cancellation, setup-owned child processes are tracked, downloads/retry waits observe the setup token, and `:signal` can target tracked children or an explicitly requested `pid:<n>`. Human-started operator shell jobs are tracked separately from setup-owned children so `:cancel` does not silently kill unrelated user repair work.

The 4.0.9 macOS entitlement/PDF-render protections, 4.0.8 documentation PDF hygiene, and 4.0.7 installer compile preflight remain intact.

See `CHANGELOG-v4.1.0-MATRIX-OPERATOR-CONTROL-PLANE.md` and `VALIDATION-v4.1.0-source.md`.
