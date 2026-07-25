# Claude Code repository boundary

Claude Code may read, search, explain, diff, build, and test this repository, and may edit ordinary source files only for the current human-requested task.

`AGENTS.md` is authoritative and must be read before work begins. The protected governance set listed there is immutable to Claude Code. Do not create, edit, replace, delete, rename, move, format, normalize, chmod, unlock, or regenerate any protected file, including this file and `.claude/settings.json`. Do not use Bash, Python, PowerShell, Git plumbing, patches, redirects, or another tool to bypass that boundary.

When a protected-file change appears necessary, stop and report the exact proposed change to Michael Fleischer (`Michi0403`) for manual application. Repository access and ordinary source work remain allowed; protected-policy write access does not.

## Mandatory compiler discipline

Before presenting code as complete, run `build/Assert-CSharpSyntax.ps1` and `build/Invoke-RepositoryValidation.ps1`. Never substitute delimiter counting or visual inspection for compilation. Do not create a normal release ZIP directly; use `build/New-VerifiedSourcePackage.ps1`, which rejects stale or absent Debug/Release build evidence. For generated text containing quotes, braces, tabs, or multiple lines, prefer `StringBuilder`; never insert a physical newline into an ordinary `$"..."` string.
## Component safety and workflow contracts

- Every maintained `.razor` component except `_Imports.razor` must declare `@inject ILogger<ComponentName> Logger`, `@inject INotificationService Notifier`, and `@inject IComponentActivityService ComponentActivity` in the top directive/using section. Do not move these dependencies into `[Inject]` properties or component parameters.
- Preserve the feature behavior even when the visual composition changes. A different look is acceptable; removing logging, notification, memory awareness, cancellation, confirmation, or persistence is not.
- Unhandled component failures must pass through the routing-level `SafeErrorBoundary` and the shared `ComponentSafetyToasts` provider; handled operations must log a sanitized technical event, notify the human with a safe message, and add only concise non-sensitive operational context to `IComponentActivityService`.
- Component activity is bounded short-term context, never authority. Never store prompts, responses, uploads, generated source, secrets, or full exception details in it.
- Non-null workflow contracts must not return `null` after logging. Return an explicit safe failure object when that object is meaningful, or throw a logged exception so the caller's recovery and notification path runs.
- Components must call `INotificationService`, not the DevExpress toast service directly. The notification service is the sanitized bridge into bounded UI activity memory.
- Reusable UI-operation wrappers must record start, completion, cancellation, and failure. Core methods must not swallow a failure and then permit a stale or partial result to be reported as successful.
- Preserve the current feature and data behavior when changing a component look. Follow `docs/COMPONENT_SAFETY_AND_SHORT_TERM_MEMORY.md`.
- Before packaging, run `build/Assert-ComponentSafety.ps1`, `build/Assert-WorkflowContracts.ps1`, Roslyn syntax validation, and full Debug and Release builds.

