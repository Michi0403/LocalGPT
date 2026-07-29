# Safe static, runtime-value, and diagnostics policy

This policy protects architecture without rewriting valid framework code or forcing logging into data objects.

## Static boundary

Application-owned mutable state, regular-expression catalogs, collection catalogs, and runtime policy values must not be static.

The only maintained exceptions are:

- the application bootstrap in `Program.cs`;
- framework syntax such as Blazor's `@using static ...RenderMode`;
- the named dependency-injection extension boundaries in PublisherStudio. Their extension methods must accept an `ILogger`, use `try/catch`, log success/failure, and rethrow failures.

P/Invoke and native exports belong behind injected lifetime services. Namespace changes, artificial wrapper services, and changes made only to satisfy a text scanner are forbidden.

## Runtime-value boundary

Regex source text, regex options/timeouts, mutable allowlists, protocol limits, retry values, identifiers, and deploy/runtime settings must come from a serializable data boundary:

- LocalGPT uses its persisted runtime-policy seed/store/data services and runtime-policy controller.
- PublisherStudio uses `PublisherStudio.RuntimePolicy` configuration, typed policy models/services, and its runtime-policy controller.

A service may compile a regex obtained from the data boundary. It may not own the regex source text as a hidden field. Pure document templates and user-visible product content are not runtime policy merely because they are strings.

## Diagnostics boundary

`try/catch` and structured logging are mandatory at maintained operational boundaries: runtime-policy loading/compilation, security/replay decisions, native interop, and runtime-policy controllers. Iterator boundaries use `try/finally` where required.

Records, DTOs, constructors, value objects, pure geometry/formatting calculations, and framework-generated members are not operational boundaries and must not receive artificial logger dependencies or meaningless exception wrappers.

## Enforcement

- `build/Assert-ApplicationStaticPolicy.ps1`
- `build/Assert-RuntimeValueOwnership.ps1`
- `build/Assert-MethodDiagnostics.ps1`
- `build/audit_application_architecture.py`

The static, runtime-value, and maintained-method checks use no legacy debt baseline. The corresponding baseline JSON files are intentionally empty. Python is preferred for syntax-aware masking; the PowerShell entry point includes a Windows PowerShell fallback so compilation is not blocked when Python is unavailable.
