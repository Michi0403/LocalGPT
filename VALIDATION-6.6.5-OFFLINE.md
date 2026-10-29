# LocalGPT 6.6.5 — offline validation

**Status: source integration, NOT a verified native/virtualized release.**

This environment has no `dotnet`, `powershell`/`pwsh`, `hluk`, or usable virtualization device, and the runtime rootfs binaries have not been downloaded. Hence it is not possible to certify installation, compile, execute, or test an actual micro-VM here. Built-in application compatibility requires host tests.

Review points:

- Build and publish hooks call one PowerShell guest runtime provisioner in `src/LocalGPT/LocalGPT.csproj` without changing the repository PowerShell architecture guards.
- Provisioner is pinned to `hluk v0.19.1`, checks version+platform, requires SHA-256 verified release assets, obtains matching OCI runtime images, and emits a digest receipt for Python/.NET JIT/PowerShell rootfs.
- Provider uses the bundled absolute `hluk` binary and each image's absolute `--initrd` path. Neither source text nor a filename is sent to a host Python/.NET/PowerShell interpreter.
- Source execution still requires user confirmation, a backend-owned session and bounded execution. The UI reports unprovisioned guest runtimes explicitly.
- The PowerShell guest smoke tests exercise all 3 images and network denial when explicitly invoked on a native host.

## Required host acceptance

1. Run the normal Debug build on a networked supported native Windows/Linux/macOS host. Confirm `hyperlight/bin/hluk[.exe]`, three `.cpio` images and `manifest.json` are created and SHA-256s match.
2. Run the repository's existing PowerShell build guards and compile `.NET 10` without regressions.
3. Execute `build/Assert-SessionSandboxIsolation.ps1 -RunGuestSmoke` on each supported platform. Check Python, C#, PowerShell and no-network tests.
4. Start/stop a guest from Chat, navigate away/reconnect and verify receipts; confirm no host runtime is called on a missing/corrupt image.
5. Publish native platform bundles and verify post-publish app contains the guest assets, with working Hypervisor/KVM/WHP and official distributable licenses.
6. Verify standard Win x86/Intel macOS builds and cross-host release behavior remain available with the sandbox visibly unsupported/unavailable.
