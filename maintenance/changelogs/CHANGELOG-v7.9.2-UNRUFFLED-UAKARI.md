# LocalGPT 7.9.2 — Unruffled Uakari

The uakari tidied up the toolbox.

## Since 7.8.2

- **Chat and Council.** A message in a new Chat starts its own session. Default Council presets resolve roles from the models discovered on each installation. The compiler Council uses the selected .NET toolchain to generate `.slnx` solution files.
- **Setup.** Saving toolchains in `/install` registers discovered SDKs and reports upgrades. Hugging Face model downloads show progress during setup.
- **PublisherStudio.** Approved 1-Wire work returns files, including PDFs, to the originating Chat. Chat shows previews for supported media and reports when PublisherStudio completes a request without returning a file.
- **NUTS and LoRA.** NUTS ships 71 benchmark scenarios across 33 default Council teams. Applying a measured role recommendation requires approval and preserves existing team members. `/install` exposes LoRA backends, package profiles, and worker settings. A trained adapter must reload before separate benchmarking and Council role approval.
- **Build and installer.** macOS restore uses the configured NuGet feeds, and builds can use a fresh cache when the existing cache is damaged. Cleanup targets project `bin` and `obj` directories and skips linked directories. Build entry points restore pinned .NET tools before compilation and accept an explicit workload refresh. macOS validation covers the application and installer, and Windows solution validation runs serially to keep shared build outputs consistent.
