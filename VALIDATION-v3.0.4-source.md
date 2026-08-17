# LocalGPT 3.0.4 source validation

This package was validated source-only. No `dotnet`, MSBuild, Visual Studio build, GitHub, or online repository access was used. The user's build/runtime remains authoritative. The stable 3.0.3 baseline had already been successfully built and started by the user, including a first-run benchmark Council live-session rejoin.

## Targeted 3.0.4 validation

- `build/audit_release_3_0_4.py`: 150 integration-fabric/toolchain checks.
- The audit covers offline defaults, outbound host/HTTPS checks, webhook-secret handling, no reflection action execution, pipeline execution through `IDxAiFunctionRegistry`, public-service bridge usage, user `user.*` DXFunction CRUD and dynamic registry wiring, Windows/Linux/macOS discovery, PATH-first behavior, Knowledge/regex/Human Collaboration wiring, structured toolchain environment rows, toolchain kind/platform persistence, controller/DXFunction/UI wiring, and version/Wire/seed invariants.
- Application architecture audit passes: maintained application statics, operational diagnostics, and C# structure comply with the source policies.
- Service resilience audit: 2,023 service methods own logged try/catch boundaries; 29 yield methods and 3 Program/Startup methods remain governed by dedicated policies.
- Async continuation audit: 239 source files, 2,685 await tokens, 2,414 `ConfigureAwait(false)`, 63 explicitly allowlisted renderer-affine `ConfigureAwait(true)`, 204 configured async disposals (all false), and 4 configured async streams.
- Provider-qualified Council audit: 282 checks.
- X-Round/heartbeat audit passes.
- Code-generation/DXFunction wiring audit passes.
- Configurable Council behavior-policy audit passes.
- Chat ASCII-console audit: 17 checks.
- Human-visible entity formatting audit passes.
- Documentation/1-Wire contract audit passes.
- Strict async/Council Teams responsiveness regression audit passes.
- XML documentation coverage/quality: 8,043 direct C# declarations across 553 maintained source files.
- Localization: 1,973 keys with identical key sets and no case-insensitive duplicate keys across `en-US`, `de-DE`, `es-ES`, `fr-FR`, `ja-JP`, and `uk-UA`.

## Historical regression chain

All maintained source release audits from `build/audit_release_2_8_5.py` through `build/audit_release_3_0_4.py` pass on the final 3.0.4 source tree, including the Council rejoin, async, provider routing, code-generation, namespace/structure, Windows-build guard, 3.0.3 compile-shadowing, and 3.0.0 EF model/snapshot regressions.

The generic EF model/snapshot audit covers 45 DbSet entity types and 644 persisted scalar properties.

## Migration preservation test

A synthetic SQLite upgrade was performed against an old-style `ProjectCompilerInstallations` table containing an existing Java compiler row.

- The existing row remained present with the same identifier, display name, and executable path.
- The new `KnowledgeProfileKey`, `ToolchainKind`, and `DetectedPlatform` columns received empty-string backward-compatible defaults.
- `KnowledgeEntryId` and `VersionKnowledgeEntryId` remained nullable.
- `RemoteControlConnectorDefinitions`, `RemoteControlPipelineDefinitions`, `RemoteControlExecutionRecords`, and `UserDxAiFunctionDefinitions` were created empty.
- No database reset, online connector, or user DXFunction seed is required by the migration.

## Stable 3.0.3 invariants

- Original explicit `@rendermode` directives: 19/19 unchanged; `/remote-control` adds the twentieth guarded `InteractiveServer` page.
- Browser JavaScript: 137/137 files byte-identical to the stable 3.0.3 baseline.
- Wire Protocol tree: 3/3 files byte-identical; protocol remains 2.1.1.
- Pre-existing migration files: unchanged. The 3.0.4 tree contains exactly one new migration plus the required updated EF snapshot.
- Legacy product namespace scan: no productive `TacosPortal.*` or `LocalGPT.Endpoints` references.
- Council team seed remains 25.

## Cross-platform toolchain behavior

- PATH is read and split using `Path.PathSeparator`, so Windows `;` and Unix `:` semantics follow the current platform.
- Discovery then evaluates profile environment roots, offline Knowledge-defined Windows/Linux/macOS roots, and explicit user roots.
- List-valued environment-root variables are split into individual discovery roots; values are omitted from diagnostics.
- Environment variables that point directly to an executable are accepted.
- The full PATH is not persisted as a toolchain environment-variable value.
- Missing exact-version Knowledge creates a Human Collaboration request with Markdown / Knowledge Database / text-blob / skip choices and does not trigger online search.

## PowerShell guard note

PowerShell is not installed in this inspection environment, so the actual Windows `.ps1` wrappers were not executed here. Their source-equivalent system-variable, iterator, text-ownership, architecture, render-mode, and async policies were exercised by the maintained Python/source audits. The user's Windows build remains the authoritative PowerShell/build confirmation.
