# Logging integrity policy

Logging removal is not cleanup. Structured diagnostics are part of the application contract and may only be replaced by diagnostics that preserve or improve the same operational evidence.

- Existing `ILogger<T>` dependencies, log calls and catch/log boundaries must not be reduced as a side effect of refactoring.
- Operational service and controller boundaries log start/completion where useful and log exceptions before rethrowing or converting them into a documented recoverable result.
- Expected cancellation is logged at Debug/Trace level when it is diagnostically useful; it is not presented as an application failure.
- Iterator and async-iterator methods containing `yield` are not wrapped in broad catch blocks because C# iterator exception timing makes such wrappers misleading. Their consuming operational boundary owns the catch/log behavior.
- Pure codecs, immutable records and deterministic formatters may be marked with `// logging-policy: pure-helper`; they must remain exception-transparent.
- `build/Assert-LoggingIntegrity.ps1` compares maintained service/controller files against `build/logging-baseline.json`. Counts may increase, but decreases fail validation. New operational files must contain structured logging and a catch boundary unless they are an explicitly marked pure helper or iterator.
- Updating the baseline is a deliberate maintainer action. Run with `ALLOW_LOGGING_BASELINE_REFRESH=1` and review the complete diff; never refresh it merely to make a regression pass.
- The build guard is compatible with the Windows PowerShell 5.1 host used by Visual Studio/MSBuild as well as modern PowerShell. It normalizes Windows paths before comparing them with the repository baseline.
- To diagnose a guard failure directly, run `powershell -NoProfile -ExecutionPolicy Bypass -File .\build\Assert-LoggingIntegrity.ps1 -RepositoryRoot .`; the script prints every concrete regression before returning a failing exit code.

## Visual Studio / Windows PowerShell compatibility

Direct MSBuild builds invoke the guard by script path and set the repository as the working directory. The guard derives the repository root from its own `build` directory instead of passing an MSBuild path ending in `\` through `powershell.exe`. This avoids native quoting failures and remains compatible with Windows PowerShell 5.1.
