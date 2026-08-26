# LocalGPT 3.3.2

LocalGPT 3.3.2 is a focused PowerShell 7 portability repair on top of 3.3.1.

The DevExpress license helpers no longer assign to `$isWindows`, which is the same variable as PowerShell 7's read-only `$IsWindows` because PowerShell variable names are case-insensitive. Both license initialization and manual license registration now use a repository-owned `$runningOnWindows` variable.

The PowerShell compatibility validator also rejects future assignments to the read-only platform automatic variables `IsWindows`, `IsLinux`, `IsMacOS` and `IsCoreCLR`, and the release build executes that guard before invoking the DevExpress license helper.

All 3.3.1 cross-platform install, build, Ollama/LM Studio setup and DevExpress licensing work is preserved. InteractiveServer render-mode boundaries remain unchanged. DevExpress remains **25.2.9**. PublisherStudio is unchanged by this archive.

See `CHANGELOG-v3.3.2-POWERSHELL-PLATFORM-AUTOMATIC-VARIABLE-REPAIR.md` and `VALIDATION-v3.3.2-source.md`.
