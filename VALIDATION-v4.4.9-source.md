# LocalGPT 4.4.9 source validation

## Validation boundary

This release was prepared from the supplied source archive only. No `dotnet` command was run and no GitHub/online repository access was used. Validation therefore consists of source inspection, maintained Python audits, JavaScript syntax checking and archive/re-extraction comparison rather than compilation or runtime execution.

## 4.4.9 contracts checked

- All maintained LocalGPT application-version project files and the game-console browser cache-buster identify 4.4.9.
- Kernel Creature Tournament owns a prebootstrapped `kernel-creature-tournament` game session through its assigned `games.ascii.kernel-tournament.rules` class.
- The service-owned tournament engine exclusively owns HP, damage, guard/recovery consequences, eliminations, bracket progression and champion selection.
- Trainer/creature AI workflow steps are bounded micro-turns and cannot award their own damage or victory.
- Tournament game animations are one-shot successive frames with a separately held subtitle; ordinary conversation ASCII sequences retain their existing replay behavior.
- Tournament timing comes from the selected compatible copyable runtime class; maintained defaults are 320 ms frames and a 1500 ms subtitle hold.
- The seven maintained self-contained presets carry `localgpt.council.context.role-isolated`; broad-context presets are not blanket-isolated.
- Role-isolated orchestration suppresses unrelated memory/project/capability briefings, project briefings and external-project-context injection while preserving role/team contracts.
- Council team seed is 32 and runtime-class seed is 6.
- Routed Razor pages retain `InteractiveServer` except the intentional non-interactive error page.
- The intentional `localgpt-game-console.js` change has a refreshed LF-normalized SHA-256 diagnostics-manifest entry.

## Static audit results

- LocalGPT 4.4.9 release/version/rendermode/JavaScript-manifest audit: passed.
- Kernel Creature Tournament engine/UI audit: 43 checks passed.
- Council role-context isolation audit: 7 isolated maintained presets; 10 broad-context project/evidence presets detected and retained.
- Chat ASCII-console audit: 24 checks passed.
- ASCII DOOM campaign audit: 32 checks passed.
- Architecture policy audit: passed.
- Service resilience audit: 2,493 service methods checked; 29 yield methods and 3 direct Program/Startup methods intentionally skipped.
- Async continuation audit: 272 source files, 3,352 await tokens, 2,936 `ConfigureAwait(false)`, 193 renderer-affine `ConfigureAwait(true)`, 218 explicitly configured await-using disposals, and 5 configured async streams.
- Configurable Council behavior-policy, X-Round/heartbeat, code-generation/DXFunction, PowerShell interpolation, cross-platform boundary and Kawaii documentation-layout audits: passed.
- `localgpt-game-console.js` JavaScript syntax check plus changed JSON/MSBuild XML parse checks: passed.
- Release archive verification: clean ZIP CRC, clean re-extraction and byte-for-byte tree comparison are required and were performed for the delivered archive.

## Limitation

A real .NET compile/run remains required on a machine with the repository's supported .NET workload to prove compiler, DI activation and runtime behavior. The source audits here are designed to catch the maintained architectural/configuration regressions without pretending to be that build.
