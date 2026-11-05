# LocalGPT 2.7.9 changelog

## Hardware performance presets are now a first-class application feature

- Added the `HardwarePerformancePreset` business object and SQLite persistence for reusable Hardware spooler profiles. A profile stores provider-qualified per-model roads, minimum/maximum output tokens, minimum/maximum context tokens, Ollama GPU-layer settings, load overrides, lane concurrency, session load, provenance, and default/archive/approval state.
- Added EF Core mapping, model snapshot coverage, and migration `20260813190000_AddHardwarePerformancePresets`. Existing databases are upgraded through LocalGPT's maintained automatic migration startup path.
- Added `IHardwarePerformancePresetService` / `HardwarePerformancePresetService` as the owner of persistence, normalization, benchmark-to-profile synthesis, delete, and application to prepared or running Council configurations.
- Added `HardwarePerformancePresetController` for list/get/save/delete/apply API access. Mutating endpoints remain human-approval gated.

## Chat Configuration / Hardware spooler frontend

- Added a user-visible **Performance preset** selector directly above the per-model CPU/GPU road editor.
- `Custom hardware settings` keeps the existing manual road workflow. Selecting a stored profile copies only exact provider-qualified matching roads and does not change Council membership.
- Added **Save current**, **Delete preset**, and **Refresh** controls. New benchmark profiles appear in the selector after a benchmark completes.
- Applying a profile also raises the session output/context ceilings to the maxima stored in that profile, preventing a valid per-model profile from being silently clipped by older Council-wide token limits.
- The same selector path can update the saved next-run preparation or, while editing a running Council, the live run configuration revision.

## Benchmark workflow

- Provider benchmark UI runs now persist every successful measured recommendation as a Hardware spooler performance profile instead of requiring the user to translate benchmark prose into token settings manually.
- Single-model and Benchmark Council runs both refresh the profile selector after persistence.
- The provider-qualified benchmark DXFunction accepts `performancePresetName`; every successful approved DXFunction run stores its measured profile automatically.
- The old Ollama-only `localgpt.models.benchmark.autotune` handler remains available for direct compatibility but is no longer advertised to AI Councils. The provider-qualified benchmark is the maintained Council path.
- The legacy model-membership preset apply methods remain compatibility-only and are no longer exposed as benchmark actions in the maintained benchmark UI.

## Council DXFunctions

The following DI-backed handlers are preseeded into the maintained DXFunction catalog at startup and are included in the Adaptive Model Benchmark Council capability set:

- `localgpt.hardware.performance.presets.list` — read-only profile discovery.
- `localgpt.hardware.performance.presets.get` — read-only exact profile inspection.
- `localgpt.hardware.performance.presets.save` — approval-gated creation/update of reviewed profile variants.
- `localgpt.hardware.performance.presets.delete` — approval-gated deletion.
- `localgpt.hardware.performance.presets.apply` — approval-gated application to saved preparation or one running Council without changing participants.

The benchmark Council seed was advanced to revision 20 and now instructs the Benchmark Director/Preset Synthesizer to use these functions rather than inventing unsaved token recommendations in prose.

## Version contract

- LocalGPT application, WebView wrapper, and installer: **2.7.9**.
- 1-Wire protocol: **2.1.1** (unchanged; this release adds application/database behavior, not a wire-protocol shape change).
- No GitHub/network repository access, `dotnet`, MSBuild, restore, compile, publish, PowerShell build, or DocFX invocation was used for this source package. The owner-side Windows build remains authoritative.
