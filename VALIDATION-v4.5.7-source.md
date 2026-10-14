# LocalGPT 4.5.7 source validation

## Scope

This release is prepared without GitHub/online repository access and without invoking `dotnet`, NuGet restore, MSBuild, compilation or publishing. Validation is therefore source/static only. The user's Windows environment must still compile and runtime-test this source, especially the DevExpress browser behavior and InteractiveServer reconnect/cancellation paths.

## Maintained topology and release identity

- Version slot policy checked for `4.5.7` (second and third slots remain below 10).
- LocalGPT, installer-console and WebView wrapper versions are aligned.
- Browser asset cache keys, LocalGPT user-agent strings and generated-documentation release identity are aligned.
- InteractiveServer baseline is checked exactly: 15 direct `@rendermode InteractiveServer` declarations plus 5 `InteractiveServerRenderMode(prerender: false)` declarations; no new/removed islands.
- Runtime-class seed version remains 7; supplied Council team seed version intentionally advances to 35 for tournament failover and the maintained work-team templates.

## UI, Chat and renderer checks

- Chat provider/model/team/performance selection surfaces use typed selection-option values where this release changed the selector wiring.
- Active DevExpress adaptive dropdown portals retain pointer input, bounded scrolling and a z-index above the fixed Chat/configuration surfaces.
- No `DxRangeSelector` or `RangeSelectorValueChangedEventArgs` remains in maintained LocalGPT component/service source; compact bounded numbers use `DxSpinEdit`.
- Inline session AI requests use the existing collaboration request service and support approval, decline, suggested-response buttons and optional text in `/Chat`.
- ASCII conversation/game surfaces project the same request state rather than requiring the separate approvals panel; with no deterministic game, the Game plane presents a bounded Council Work Control for the pending request.
- Chat lifecycle catches cancellation/disconnect during JS interop as transient navigation/reconnect state and caches composer/reconnect publication.
- ASCII console browser attachment/input/scale/frame/sequence/layout state is signature-cached; Game/operator mode avoids rebuilding the full live transcript on every streamed update; process-output rendering is coalesced.
- Renderer-affine callbacks/interop are explicitly configured to continue on the renderer where needed; the maintained async-continuation audit remains the source of truth for background/service continuations.

## Tournament and role-recovery checks

- Fighter state carries bounded creature species/style/form/trait/voice metadata.
- Trainer and creature ASCII sprites are generated deterministically from identity/descriptor data and used in multi-frame exchange presentation.
- Every exchange has command, move, clash, impact, recovery and transition presentation frames while the deterministic engine retains authoritative HP/damage/bracket state.
- Duplicate creature display names are qualified locally without re-entering AI naming deliberation.
- `RecoveryTargetModelName` preserves the failed role slot through member-pool recovery.
- Tournament trainer, creature, ASCII-artist and fight phases use bounded `RetrySameThenEligibleRolePool` recovery and preserve failed evidence.
- Existing repetition-watchdog and single-name protections remain in place.

## Compiler/publisher orchestration checks

- The default seed contains `Program Compiler & Repository Curator` and `Project Maintenance & Cross-platform Publisher` teams.
- Compiler-team steps cover ZIP/text intake, regex analysis, reconstruction/planning, curation, toolchain selection, verification and human review with bounded member failover.
- Publisher-team steps cover release identity, trusted 1-Wire host/capability planning, platform execution, strongest-host documentation/PDF assignment, artifact curation and final review.
- Protected remote actions continue through existing public-service/DXAIFunction review paths; team prompts do not grant themselves direct filesystem/network execution authority.
- Project-maintenance inspection exposes current-version, version-history and artifact metadata used for versioned result organization.

## Automated source checks

The final source tree was checked with maintained Python audits for the 4.5.7 release contract, DevExpress controls, provider-qualified Council (282 checks), configurable behavior, Kernel Creature Tournament (49 checks), Chat ASCII (24 checks), ASCII color (58 checks), ASCII DOOM (32 checks), Council role isolation, architecture, async continuations, service resilience, Council SQL seeds, X-Round wiring, cross-platform boundaries, DXFunction/codegen wiring, provider repetition policy, configuration-root qualification and PowerShell interpolation. The async continuation audit covered 272 source files with 3,368 await tokens, 2,943 `ConfigureAwait(false)` continuations, 202 renderer-affine `ConfigureAwait(true)` continuations, 218 explicitly configured async disposals and 5 configured async streams. Service-resilience auditing covered 2,515 service methods.

All 24 maintained browser JavaScript files passed `node --check` and still match the JavaScript diagnostics hash manifest. JSON parsing passed for 38 maintained JSON files and direct XML/MSBuild parsing passed for 10 maintained XML/project files selected by the source validator. The repository XML-documentation coverage checker still reports historical baseline debt already present in 4.5.6; after documenting the new 4.5.7 members, the normalized finding count remains 559 and no new finding remains in a changed/new 4.5.7 member. These checks do not substitute for a .NET compile.

## Packaging

The source ZIP is CRC-tested, checked for absolute/traversal paths, cleanly extracted, and compared byte-for-byte against the packaged source tree after excluding transient interpreter/build output. This validates packaging integrity, not .NET compilation or runtime behavior.
