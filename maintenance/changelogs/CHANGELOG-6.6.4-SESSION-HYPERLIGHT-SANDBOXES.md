# LocalGPT 6.6.4 — Session-owned Hyperlight code sandboxes

Additive, explicitly human-approved source execution for debugging/tests:

- Pluggable isolated provider contract; initial Hyperlight Unikraft CLI provider for Python and C#/.NET JIT.
- Process-wide hosted execution registry keyed to Chat or Council session, not to the Blazor render circuit. Cancels sandbox processes on stop/timeout/host shutdown.
- Running session tools: session picker, language picker, editable source, CodeDOM-based runnable C# source preview, launch/stop/refresh actions, bounded live stdout/stderr, status, errors and exit codes.
- Cross-platform per-user evidence records including a source SHA-256 fingerprint and source origin, interrupted-run recovery, and source-file cleanup.
- Fresh execution confirmation is required for every run; generation of CodeDOM source never starts a guest.
- Network, project mounts, host-function exposure and silent host-side fallback all excluded from this phase.
- PowerShell-only source contract and opt-in real guest smoke checks; platform and runtime installation requirements documented.
- **Unchanged:** prior Council backend orchestration, trusted Windows/Linux/macOS build and debug systems, Project Maintenance and team configuration.
- **Not delivered as completed:** host-platform compilation, Hyperlight guest smoke execution, C++ provider or isolated compiler, automatic execution of approved project workspaces, full interactive debugger, Council auto-execution tools, durable project/chat build configurations, persistent running VM state across application restarts, or sandboxed model inference.
