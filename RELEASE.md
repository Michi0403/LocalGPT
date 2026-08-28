# LocalGPT 3.4.5

LocalGPT 3.4.5 is the **DocFX Console Progress Repair** release.

It keeps the complete documentation and PDF build unchanged. The only functional change is in DocFX console rendering: raw carriage-return progress records remain captured for diagnostics, while the redirected `Removed ... files` / `Copied ... files` redraws that become impossible-looking counters in PowerShell are no longer written back to the release terminal.

No application, UI, service, documentation content, PDF requirement, deployment, or packaging behavior was changed. This handoff is source-only; no .NET build and no GitHub/network source access were used while preparing it. See `CHANGELOG-v3.4.5-DOCFX-CONSOLE-PROGRESS-REPAIR.md` and `VALIDATION-v3.4.5-source.md`.
