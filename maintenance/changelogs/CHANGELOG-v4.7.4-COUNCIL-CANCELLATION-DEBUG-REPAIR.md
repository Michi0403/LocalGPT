# LocalGPT 4.7.4

## Council overnight cancellation/debugger repair

- Prevents expected Ollama stream cancellation from escaping `StreamReader.ReadLineAsync(CancellationToken)` as a framework `TaskCanceledException` that Visual Studio can classify as user-unhandled and pause a debugger-attached overnight Council.
- Keeps the provider adapter cancellation-aware without converting ordinary participant timeout, round-skip, live-input interruption, or explicit run-stop signals into a successful full response.
- Returns cleanly from the provider iterator when the caller-owned stream token is canceled, then immediately re-surfaces the same caller-owned cancellation from LocalGPT Council code so the existing timeout/recovery, round-skip, and explicit-stop policies retain their previous semantics.
- Normal Council participant timeouts therefore continue through the existing bounded timeout/recovery path instead of becoming a debugger-breaking framework read exception.
- Live user input still cancels/restarts only the currently claimed model stream and does not become a participant timeout.

## Diagnostic clarification

- The repeated `System.Text.Json.JsonReaderException` lines visible under the debugger are first-chance parser diagnostics from malformed/partial provider JSON frames; the Ollama stream parser already catches and skips those frames. They are noisy but are not the event that stopped the supplied run.
- The supplied overnight log ends on `System.Threading.Tasks.TaskCanceledException` after the Ollama stream finally block, with Visual Studio explicitly reporting that the exception was not handled in user code. This release moves expected read cancellation back behind the LocalGPT provider boundary so that condition no longer leaks from framework stream reading during normal Council timeout/control flow.

## Compatibility and scope

- Preserves 4.7.3 ASCII popup dropdown, scrolling, scaling, large-Council projection and responsive modal repairs.
- Preserves 4.7.2 Council provider/model binding repair and Kernel Creature Tournament improvements.
- Preserves existing `@rendermode InteractiveServer` coverage.
- PublisherStudio source is unchanged in this release.
- No GitHub/network repository access or .NET build tooling was used while preparing this source package.
