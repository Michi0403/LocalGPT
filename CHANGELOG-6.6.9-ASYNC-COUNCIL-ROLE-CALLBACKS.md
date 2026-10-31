# LocalGPT 6.6.9 — Awaitable Council role editor callbacks

## Change

- The team ComboBox and role ListBox now use explicitly typed `async` lambdas that `await` their delegated `Task` handlers. The component's actual selection updates remain in those handlers, not the Razor attribute.
- The `TeamChangedAsync(string)` and `SelectedChangedAsync(IEnumerable<string>)` handlers currently perform synchronous state updates and return `Task.CompletedTask`. This makes later asynchronous service work possible without changing the DevExpress callback contract.
- Keep the Blazor renderer synchronization context inside component event handlers; do **not** use `ConfigureAwait(false)` in these UI event callbacks. Service-only code can use it where returning to a renderer context is unnecessary.
- Updated the existing version-independent PowerShell Council wiring guard for the exact callback contract.

## Preserved

- EF migration and indexing, all saved Council role bindings, Ollama inventory, model identities, protected team templates and confirmation policy.
- Hyperlight, CodeDOM, runtime provisioning, Windows/Linux/macOS RIDs and the existing application build guards. No Python or Node regression tests added.

## Validation limits

This source-package change was statically checked against 6.6.8 and the ZIP integrity-tested. The host used to assemble it has no .NET SDK or PowerShell; the actual Blazor compilation and Windows/Linux/macOS builds remain unverified.
