# LocalGPT 4.8.1

## Maintenance-policy compile repair

- Repairs the 4.8.0 toolchain/runtime additions so they comply with LocalGPT's existing service-resilience policy instead of weakening or bypassing the audit.
- Adds logged `try/catch` boundaries to the new database-backed toolchain execution-profile methods, environment persistence/helpers, and local-AI runtime-policy methods identified by the owner's Visual Studio build.
- Keeps cancellation behavior explicit: cancelled async operations log at debug level and rethrow, while operational failures log without exposing command arguments, paths, environment values, prompt text, or serialized configuration contents.
- Preserves semaphore ownership and releases gates only after successful acquisition, preventing cancellation-before-lock from releasing an unowned semaphore.

## Application-static policy repair

- Removes the new application-level `static` serializer state and static helper methods from the 4.8.0 toolchain services.
- Toolchain JSON serialization and helper behavior now remain scoped to the service instance, matching the maintained LocalGPT architecture policy.
- No application-static policy allow-list, baseline, or audit exception was added.

## Text-service ownership repair

- Moves Toolchains environment search/filter behavior out of the Razor component and into `IToolchainEnvironmentService` / `ToolchainEnvironmentService`.
- The Install UI now asks the injected service for bounded filtered environment rows instead of owning direct `Contains(..., StringComparison)` text behavior.
- No text-service ownership baseline entry was added for the new UI code.

## Documentation warning repair

- Adds the missing XML documentation entry for the `profiles` dependency on `ToolchainRuntimeController`, resolving the reported CS1573 warning without suppressing XML documentation diagnostics.

## Preservation

- The 4.8.0 approval, generic toolchain, Python/Whisper, ASCII console, Council continuity, benchmark repetition, and ZIP-promotion behavior remains intact.
- Existing maintenance rules are unchanged: service-resilience, application-static, text-service ownership, XML documentation, InteractiveServer, async-continuation, localization, architecture, and other build audits remain authoritative.
- Version advances from 4.8.0 to 4.8.1. PublisherStudio is unchanged.
- No GitHub access, `dotnet`, MSBuild, NuGet restore, build, publish, or installer execution was used for this source repair.
