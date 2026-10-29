# LocalGPT 6.6.5 — Reproducible Hyperlight guest runtime provisioning

## Added

- Automatically prepare **Python**, **.NET JIT (C#)**, and **PowerShell** micro-VM guest root filesystems on native supported build hosts. A PowerShell-only `build/Provision-HyperlightGuestRuntimes.ps1` builds the local application payload, not the host's system Python/.NET/PowerShell installation.
- Pin the `hluk` driver and guest image protocol at upstream `v0.19.1`; select signed-release assets by host OS/architecture; require and verify upstream release SHA-256 checksums or release asset digest; import runtime CPIOs via the upstream `hluk init` OCI image pull, which verifies the OCI layer digest. Avoid remote installer-script execution and Docker prerequisites.
- Stage the signed/verified host `hluk` CLI and all three guest images into `hyperlight/bin` and `hyperlight/runtimes`, with a checked hash manifest. On subsequent builds, verify existing bundles and reuse local caches.
- Add build and publish MSBuild targets in LocalGPT only. Developer builds attempt provisioning but emit a visible warning if offline; supported **native release publish** refuses missing runtime images instead of silently shipping an apparently supported sandbox. Unsupported `win-x86` and `osx-x64` remain buildable with an explicit sandbox-unavailable diagnostic; cross-host publish does not break existing OS packaging.
- Hyperlight execution now runs with explicit packaged `--initrd` images and bounded per-language scratch memory, never host-side interpreters. The backend validates bundled image and executable hashes before starting a guest.
- Add PowerShell to the existing session-owned runtime picker and surface provisioned/unprovisioned status beside running session tools.
- Expand the PowerShell guest smoke test to use the **exact bundled images** for Python, C#, PowerShell and network denial. This opt-in test cannot fall back to host execution.

## Compatibility

- Existing CodeDOM C# generation, project and chat session ownership, Council backend execution, role configurations, 1-Wire, Windows/Linux/macOS targets and protected PowerShell validation rules remain in place.
- No LLM guest inference and no host filesystem mounts/network capabilities are introduced.
- This source package does **not** contain predownloaded runtime images or a prebuilt `hluk` binary. Those artifacts are downloaded and verified on a supported native builder the first time the project builds/publishes with Internet access. Air-gapped builds need a previously provisioned cache/output or explicit offline artifact seeding.
- Hyperlight Unikraft's current published guest support does not cover Windows x86 or Intel macOS; LocalGPT itself remains supported there but does not offer a fake micro-VM capability.

## Validation status

- Offline source/archive checks only in this environment. Full PowerShell execution, .NET 10 build, host virtualization, actual guest runtime execution and signed multi-platform publishing must be run on real supported machines; see `docs/reference/session-hyperlight-sandboxes.md`.
