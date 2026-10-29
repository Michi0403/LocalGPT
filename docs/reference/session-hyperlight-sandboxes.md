# Hyperlight guest runtime build and session execution

LocalGPT's isolated session execution is supplied by Hyperlight Unikraft `hluk` and three pinned guest rootfs images: `python`, `dotnet-jit` (C#) and `powershell`. They are **guest** runtimes. They do not replace or install Python, .NET, PowerShell or compilers on the host. The normal PowerShell build and architecture guards remain untouched.

## Build-time provisioning

After a successful LocalGPT `dotnet build`, `ProvisionLocalGptHyperlightGuestsForBuild` runs `build/Provision-HyperlightGuestRuntimes.ps1`. It checks the output directory and uses a persistent per-user cache to avoid repeated downloads. When a bundle is missing, it downloads an upstream `hluk v0.19.1` release archive, verifies its SHA-256 against the upstream checksum file or GitHub release asset SHA-256 digest, then uses `hluk init --template ... --image-version 0.19.1` to download the matching OCI guest images. The Hyperlight CLI checks OCI layer digests. It then copies the images into the app output and writes an integrity receipt.

A `dotnet publish` of LocalGPT also runs `ProvisionLocalGptHyperlightGuestsForPublish` on the **native build host**. Supported native releases must have all three images. Missing downloads make this publish fail so an incomplete sandbox feature is not silently shipped. Developer builds that are offline continue with a clear warning; the sandbox itself refuses to launch without verified images.

Only the LocalGPT application project has these new targets; PublisherStudio and repository-owned PowerShell build validation remain separate.

Built application layout:

```text
LocalGPT application directory/
  hyperlight/
    bin/hluk[.exe]
    runtimes/
      python.cpio
      dotnet-jit.cpio
      powershell.cpio
      manifest.json
```

To provision manually from the repository without running guest code:

```powershell
./build/Provision-HyperlightGuestRuntimes.ps1 -OutputDirectory ./src/LocalGPT/bin/Debug/net10.0 -Mode Release
```

To run the **actual isolated guest acceptance smoke tests** on a supported machine with virtualization enabled:

```powershell
./build/Assert-SessionSandboxIsolation.ps1 -RunGuestSmoke -RuntimeOutputDirectory ./src/LocalGPT/bin/Debug/net10.0
```

This verifies the bundled CLI and CPIO hashes and executes distinct Python, C# and PowerShell guest programs, plus a Python no-network test. Those tests are **opt-in**; they are not evidence of successful VM execution until run on the target host.

## Target compatibility

- Windows x64 and ARM64: Windows Hypervisor Platform (WHP); native provisioning and guest execution require the Windows hypervisor to be enabled.
- Linux x64 and ARM64: hardware virtualization with accessible KVM (or a verified alternative Hyperlight backend); use `pwsh` for the LocalGPT repository build pipeline.
- macOS Apple Silicon: Hypervisor.framework with an entitlement-signed `hluk` binary, applied during provisioning. Each `hluk` invocation is a separate subprocess; Apple's one-VM-per-process restriction still applies.
- Windows x86 and Intel macOS: the LocalGPT application targets remain, but this Hyperlight guest provider is unavailable. No host-code execution fallback is allowed.
- Cross-compiled application bundles are not falsely labeled as provisioned on another architecture. Run native provisioning on the target to add the correct guest images; ordinary LocalGPT cross-platform packaging remains available.

## Trust and execution

The backend owns sandbox runs by Chat/Council session ID, not by a Blazor component. A navigation/disconnect only stops observing. Only the explicit Stop command or timeout terminates guest execution. No guest network access, host project mounts, or host functions are supplied. Only the explicitly confirmed snippet enters the guest; sandbox execution receipts include source SHA-256, status, bounded stdout/stderr and exit code. Artifact hashes are validated again before runtime start.

The guest images are large and are **not stored in source control**. The pinned release binaries and rootfs images are fetched during build and are bundled into the runtime payload, not attached to the source archive. Third-party licenses and provenance should be reviewed before distributing a bundled release. Runtime inference and language-provider extensions can be integrated later without changing Council lifetime semantics.
