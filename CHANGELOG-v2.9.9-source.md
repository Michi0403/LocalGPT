# LocalGPT 2.9.9 source changelog

## Council Teams architecture compile repair

- Fixed the three `Assert-TextServiceOwnership.ps1` violations introduced by 2.9.8 in `CouncilTeams.razor`.
- User-editable automatic-function list parsing and presentation now run through the injected `CouncilTextService` rather than direct component `.Split(...)` / `string.Join(...)` operations.
- The architecture guard is not weakened or bypassed; the UI delegates the behavior to the maintained DI-backed service boundary.
- Removed the stale `team` XML documentation parameter from `BuildConfiguredWorkflowPreviousStep`, fixing the CS1572 warning reported by the Windows build.

## Configured Council live lanes and stream synchronization

- `LeaderSingle`, `AllMembersSequential` and other configured single-participant workflow steps now publish the same rich producer-side participant activity used by parallel Council phases.
- Thinking, provider text, function calls/results and recovery/status fragments are appended to the participant lane immediately while the model is running.
- The ordered DXAIChat transcript copy is coalesced into bounded 8 KiB presentation chunks for configured single/sequential steps, matching the anti-backlog strategy already used by parallel phases.
- Model execution remains independent from browser transcript replay. A completed provider turn no longer has to look inactive merely because ordered presentation is catching up.
- Completed participant answers are stored in the live lane before normal Council-step integration. Cancellation/failure statuses also close the lane explicitly instead of leaving a stale running card.
- Existing renderer-affine Blazor awaits remain explicit `ConfigureAwait(true)` sites; Council/service orchestration continues using `ConfigureAwait(false)` so renderer synchronization is not introduced into model execution.

## Ollama native-function robustness

- Native Ollama tool construction now validates transport-safe function-name uniqueness before tool metadata is sent to the provider, not only during fallback/resolution.
- The parameterless `localgpt.time_state.now` schema/validator repair from 2.9.8 remains intact and is covered by the 2.9.9 release audit.
- Qwen/Ollama thinking parsing remains intact; configured single-participant stages now expose that same stream through the rich participant lane instead of only the ordered transcript surface.

## Compatibility

- Product version: 2.9.9.
- Under the maintained single-digit segment rule, the next release after 2.9.9 is 3.0.0 rather than 2.9.10.
- Council seed version remains 25; no user-owned team configuration is overwritten by this repair.
- LocalGPT Wire Protocol remains 2.1.1 and its source tree is byte-identical to 2.9.8.
- All 19 explicit `@rendermode` directives are byte-for-byte equivalent in location/content to 2.9.8; `CouncilTeams.razor` remains `@rendermode InteractiveServer`.
- All 137 browser JavaScript source files are byte-identical to 2.9.8.
- `Directory.Build.props` is byte-identical to 2.9.8.
