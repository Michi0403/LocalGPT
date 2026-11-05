# LocalGPT 2.9.8 source changelog

## Council behavior policy is user configuration

- Added `CouncilAutomaticFunctionPolicyMode` and persisted team/step automatic-function policy.
- Council Teams now lets users choose no automatic functions, every registered policy-approved function, the team's allow-list, or one exact step allow-list.
- Shipped function lists remain resettable template data; `MultiModelCouncilService` no longer owns a hidden benchmark allow-list.
- Added persisted role-compliance retry and final-answer recovery controls per workflow step.
- Added the maintained architecture rule that user-observable behavior policy belongs to serializable BusinessObjects and DI-backed Services/Controllers, while technical compatibility/buffer invariants remain implementation-owned.

## Council team preset lifecycle

- Council Teams can delete a configured preset through an explicit-confirmation tombstone.
- Deleted presets cannot run but stay visible in the configuration editor for recovery.
- Any configured team can be reset from any supplied default template while preserving its configured key.
- Default templates are exposed separately from persisted user-owned rows.
- Seed version advances to 25 so maintained template defaults are refreshed without overwriting user-owned edits.

## Parameterless DXFunction validation

- Empty-object function calls such as `localgpt.time_state.now` now validate correctly.
- The schema validator enumerates proposed properties directly instead of dereferencing the default result of `FirstOrDefault()` on `{}`.
- Ollama automatic-tool construction now rejects duplicate transport-safe tool names rather than exposing ambiguous aliases.

## Rich Council lanes and live synchronization

- Preserved the existing rich participant cards, provider thinking, function calls/results, completed answers, role/host labels and structured JSON expansion.
- Chat now prefers the newest in-memory live-session participant state over an older attached snapshot.
- Added lightweight live-session reads for participant activities and transcript independently.
- Large transient stream buffers trim with hysteresis instead of shifting almost continuously at the maximum size.
- Ordered transcript replay coalesces completed provider fragments before presentation; participant-local rich lanes still receive producer updates immediately.
- Removed the redundant explicit Ollama unload request after a call that already owns `keep_alive=0s`, preventing avoidable dispatch delay between members.
- Recovery status now explicitly distinguishes additional model work from delayed UI rendering.

## Compatibility

- Product version: 2.9.8.
- LocalGPT Wire Protocol remains 2.1.1.
- Interactive Server render-mode directives are unchanged from 2.9.7.
- No JavaScript browser source changed in this release.
