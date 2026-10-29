# LocalGPT 6.6.6 — Sandbox static/Razor compile repair

## Fixed against the complete 6.6.5 source

1. `Services/CodeDomSourceService.cs`: eliminate all three new `private static` methods. The existing injected, singleton-safe CodeDOM emitter now uses instance methods and still emits identical reviewed C# source.
2. `Services/SessionSandboxExecutionService.cs`: remove two new private static helpers, preserving the backend-owned run registry, cancellation, guest cleanup, and evidence persistence.
3. `Services/HyperlightUnikraftSessionSandboxProvider.cs`: remove two new static SHA-256 verification helpers. The integrity checks still run and remain fail-closed.
4. `Components/Pages/Chat.razor`: rename the C# pattern variable `code` to `sandboxExitCode`, avoiding Razor treating `@code` as its code-block directive (RZ2005/RZ1017).
5. `Components/Pages/Chat.Sandbox.razor.cs`: import `LocalGPT.Components.Shared` so the existing internal `LocalGptSelectionOption<string>` type resolves (CS0246).
6. `build/Assert-SessionSandboxIsolation.ps1`: add PowerShell-only source contract assertions for those regression points. Existing required static-policy and compiler guards stay enabled.
7. Increment application version to 6.6.6.

## Preserved

No changes to Hyperlight guest runtime provisioning, Powershell build/publish targets, cross-platform RIDs, binary artifacts, Council backend lifecycle, benchmark configuration, team role bindings, CodeDOM behavior, permissions, or PublisherStudio. No Python or Node validation code added.

## Validation limitations

Source-level checks and package integrity were performed offline. Neither .NET 10 nor PowerShell is installed in this execution environment, so the repository's PowerShell validators, compilation, native Hyperlight guest execution, and cross-platform smoke tests **have not been executed**. The three reported Visual Studio errors have targeted source corrections but successful compilation remains to be verified on a supported build machine.
