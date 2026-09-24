# LocalGPT 4.8.6 — dynamic plugins and generic toolchains

## Toolchains and compilers

- Made the maintained toolchain knowledge blocks actually machine-readable by `ToolchainKnowledgeService`; the common .NET, MSBuild, Java/JDK, Maven/Gradle, Python, Node.js, PowerShell, GCC/G++, Clang, CMake, Rust/Cargo, Go, PlatformIO and Arduino CLI profiles now participate in knowledge-driven discovery instead of being documentation-only examples.
- Retained and surfaced additional common profiles for MSVC, Ninja, Make, TypeScript/npm/pnpm/yarn, Deno, Bun, PHP, Ruby, Zig, AVR-GCC and ESP-IDF.
- Added a manual/custom toolchain editor in Setup so any user-configured compiler/runtime executable can be persisted with its language/family, home path, kind, validation arguments and optional knowledge-profile key.
- Reused `ProjectCompilerInstallation` and the existing generic project build-verification path rather than introducing compiler-specific build services.
- Surfaced the existing ESP32/Arduino capability in Setup, including board/GPIO planning, Arduino sketch generation, telemetry contracts, embedded Council support, PlatformIO and Arduino CLI integration.

## Runtime extensions

- Added `RuntimePluginDefinition` persistence plus an EF migration and snapshot update. The BusinessObject is the source of truth for identity, source/package payload, compiler selection, AI visibility, safety policy, build/load status and timestamps.
- Added a dedicated runtime-plugin service and non-executing startup descriptor refresh. Persisted executable content is never silently loaded merely because it exists in the database.
- Added a stable `LocalGPT.PluginContracts` project containing `ILocalGptRuntimePlugin`.
- Added collectible per-plugin `AssemblyLoadContext` loading with `AssemblyDependencyResolver`; the shared contract remains in the host load context to preserve type identity.
- Added C# script-body materialization through a user-selected/configured .NET toolchain, JavaScript execution through a configured Node-compatible runtime, and persisted trusted compiled-plugin ZIP payload loading.
- Added a DevExpress Setup editor modal with source editing, JSON schema/safety settings, compiler/runtime selection and an inline IDE helper. Save and Build/load are separate explicit actions.
- Extended `DxAiFunctionRegistry` with runtime-extension descriptors and invocation as a third dynamic source alongside built-in handlers and persisted user pipeline functions. Runtime-extension invocation still passes through the same schema validation, catalog policy, human confirmation and deferred approval flow.

## Guard and build-policy preservation

- Kept the service resilience and application architecture rules intact; the new service/runtime implementation satisfies them without new exclusions.
- Preserved the successful small-chunk documentation PDF strategy unchanged.
- No .NET build/restore/publish was performed by the assistant; the user build remains the compiler/runtime authority.
