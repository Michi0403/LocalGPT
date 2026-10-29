# Chat/Council session code sandboxes

## Scope and guarantees

LocalGPT's original Windows/Linux/macOS build, debug, toolchain, and Council pipelines remain unchanged. A new *opt-in* code-execution surface appears under **Chat → Running session tools → Session code sandboxes**. It runs submitted Python or C# snippets via **Hyperlight Unikraft (`hluk`)**, not via `python`, `dotnet`, a shell, or a host fallback. No sandbox is created until the human selects **Run inside sandbox**. The backend requires `UserConfirmed` on each new request; source preparation or Council output does not create that confirmation. The nested **CodeDOM source generator** optionally stages a runnable C# test program via the same typed generator used by the project change-review service. Generating source never executes it: the human can inspect or edit the preview before a separate Run action.

The backend registry is a singleton hosted service. The task, cancellation source and output evidence are owned by the host process. Closing Chat, leaving the page, or disconnecting a Blazor circuit **does not stop a sandbox**. Select the original session from the session picker to inspect or stop runs. Running sandboxes are terminated when the LocalGPT host stops. Runs do *not* resume after application restart; incomplete receipts are marked `Interrupted`.

Stored receipts (run/session/project/conversation identifiers, source origin and SHA-256 digest, times, status, bounded stdout/stderr, exit code, diagnostics) are saved under the same per-user LocalGPT application-data root used by other services, in `sandbox-sessions/runs`. Submitted source is kept in a per-run temporary workspace and deleted after execution; source is **not** stored in the receipt. Per-stream outputs are capped. Sessions currently support a maximum of three concurrent runs and a two-minute execution deadline. Running processes are killed when stopped or timed out.

## Runtime installation and platform behavior

Install the maintained Hyperlight Unikraft `hluk` CLI using its official installation guidance. The CLI must be on PATH, or set `LOCALGPT_HLUK_PATH` to an **absolute path to the installed executable**. Hyperlight requires an accessible native hypervisor backend: KVM on Linux, Windows Hypervisor Platform on Windows, and Apple Hypervisor.framework on Apple Silicon macOS. The CLI may pull its Python or .NET JIT runtime image to its own cache on first use; that is **host-side image provisioning**, not guest network permission. Pre-provision the image when working offline. Platform restrictions and installation failures are displayed as failed runs without launching language tools on the host. In particular, this change is not a claim of feature parity or verified runtime availability on Intel macOS.

The provider uses `hluk run --runtime python submitted.py` or `hluk run --runtime dotnet-jit submitted.cs`. It does not pass `--net`, `--mount`, `--port`, or host functions. Only the temporary submitted source file is passed to the CLI. Ambient LocalGPT API tokens and application secrets are not forwarded to the child environment. **No runtime execution is attempted without the selected isolation provider.** Permissions can later be expanded through an explicit, reviewed capability policy; they are not inferred from a model's instructions.

## Validation

The PowerShell-only source contract is:

```powershell
./build/Assert-SessionSandboxIsolation.ps1
```

On a machine with working Hyperlight virtualization and pre-provisioned images, run the opt-in live guest tests (Python, C# and denied network):

```powershell
./build/Assert-SessionSandboxIsolation.ps1 -RunGuestSmoke
```

For a complete acceptance check, also submit failing code and confirm nonzero exit codes/stderr are visible; stop a long-running guest and verify termination; navigate away from Chat and return to its session to verify backend independence; close and restart LocalGPT and verify evidence returns with an interrupted receipt. Verify Windows, Linux and Apple Silicon macOS separately. No successful VM execution or cross-platform build is claimed from source review alone.

## Relationship to CodeDOM, C++ and browser compilers

The existing CodeGenerationWorkflowService uses the injected `ICodeDomSourceService` to emit reviewed C# class sources. The sandbox UI uses the same service and adds a small `Program.Main` test harness; the resulting `.cs` is editable and runs only when approved through the session execution service. Generation is done on the ordinary host, but **generated code never runs on the host**. This preview is *not* yet a project-file browser, a CodeDOM full-program generator, or an automatic test of approved project revisions.

**CodeDOM is an AST/source-generation abstraction, not a compiler or an isolation mechanism.** The existing .NET 10 project has a C# provider. C++ source emission needs an appropriate separately installed or implemented CodeDOM language provider; C++ compilation and execution additionally require an explicitly configured and audited guest toolchain. The Hyperlight Unikraft published `c` guest is not a promise that it accepts arbitrary C++ source: compiled languages may require compilation **outside** the guest, which is not allowed for untrusted submitted source in this workflow. Until an isolated C++ compiler is verified, the UI does not advertise C++ execution or quietly substitute host compilers.

The supplied WasmSharp reference demonstrates Roslyn compile/run with browser diagnostics; it could be an optional client-side authoring aid in the future. It does not replace backend-owned sandbox execution, persisted run receipts, cancellation or hypervisor isolation; no WasmSharp dependency was added to LocalGPT.

## Evolution

`ISessionSandboxExecutionProvider` advertises supported languages and prepares an isolated launch. New runtimes (and, later, isolated model inference) can be registered through DI without rewriting Council, project configuration or the existing trusted PowerShell build/debug infrastructure. Direct Council-triggered execution and project-linked saved build configurations require separate explicit permissions and durable workflow integration; they are **not yet implemented** here.
