# LocalGPT 4.8.9 — toolchain auto-validation, discovery and UX

## Toolchain discovery and validation

- Automatically validates persisted discovery results with each knowledge profile's bounded local version probe.
- Keeps discovery resilient when an individual candidate cannot execute and selects new language defaults from successful probes while preserving existing defaults.
- Executes Windows command-wrapper toolchains (`.cmd` / `.bat`) through `ComSpec`, covering common package/build tools that cannot be launched directly with `UseShellExecute=false`.
- Added optional wildcard executable-name patterns to generic toolchain knowledge profiles. Python uses them for versioned executable names without introducing Python-specific discovery code.
- Broadened maintained Python roots for pyenv, Conda and virtual environments and Node.js roots for NVM, Volta, FNM, mise and asdf. PATH remains the first discovery source and arbitrary user-configured executables remain supported.

## Version correctness and approval UX

- Added `builtin.toolchain-version-token-v2`, which correctly extracts versions with common leading markers such as Node.js `v24.18.0` instead of accidentally returning `18.0`. The prior regex remains intact for compatibility; maintained profiles use the new key so an existing database receives a new seeded row.
- Successful ordinary validation now links exact-version Knowledge Database context when available but does not create a writing assignment merely because exact-version documentation is absent.
- Explicit `toolchain.knowledge.request` calls now create a one-click Human Collaboration Approve/Decline request with no required free text or documentation authoring.
- Added a data-only migration that converts pending legacy toolchain guidance requests from 4.8.8 into the new approval form.

## Responsive UI

- Toolchain card headers now wrap safely.
- Long environment variable names, values, scope/source labels and executable paths now use zero-minimum grid tracks and bounded wrapping.
- Environment rows shift from three columns to two plus a full-width scope line at constrained desktop widths, then to one column on narrow layouts.

## Release identity

- Bumped active LocalGPT, installer, wrapper, HTTP user-agent and browser cache-buster identities from 4.8.8 to 4.8.9.

## Validation note

Source-only checks were used in the handoff environment; no .NET/MSBuild/NuGet build, restore, publish or installer execution was performed.
