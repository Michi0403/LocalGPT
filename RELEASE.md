# LocalGPT 4.5.1

LocalGPT 4.5.1 is the compile/build-guard repair release for the 4.5.0 ASCII color system. It preserves the full ANSI-16/indexed-256 presentation contract, the terminal-default black/green compatibility path, existing DOOM/Green Dragon/Kernel Tournament/hot-seat integration, custom Game-project palette authoring, and the small-model Project/knowledge/regex guidance introduced in 4.5.0.

The repair moves styled-frame and animation presentation-signature string projection out of `ChatGameConsole.razor` and into the already injected `AsciiChatTextService`, restoring the repository's text-service ownership boundary. The component now remains presentation/orchestration-only while the service owns the bounded `string.Join` work and diagnostics.

The Kernel Creature Tournament compile regression is also corrected. Tournament animation and current-frame semantic style generation now call the maintained five-argument `BuildSemanticAsciiStyleRuns(gameKey, runtimeProfile, frame, frameWidth, frameHeight)` contract explicitly. This keeps tournament color semantics unchanged while matching the actual service signature.

No seed-data bump was needed for this repair: Council team seed version remains 33 and runtime-class seed version remains 7. The wire protocol remains unchanged. Browser cache keys, package/application versions, user-agent strings and documentation release identity are advanced to 4.5.1.

Validation remains source/static only in this environment: no `dotnet` restore, compile, build or publish and no GitHub/online repository access were used. The supplied Visual Studio build diagnostics were used as the concrete regression targets, and the repository's maintained static audits are rerun against the repaired source and again after clean ZIP extraction where supported.
