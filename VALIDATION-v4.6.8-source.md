# LocalGPT 4.6.8 source validation

GitHub was not used and `dotnet`, restore, build, MSBuild, NuGet or publish were not invoked in this environment.

LocalGPT 4.6.8 is a source-level repair for the Kernel Creature Tournament, Council rejoin and detachable ASCII/pixel game surface.

## Diagnostic basis

The supplied macOS diagnostics show a Kernel Creature Tournament session starting for a Council run, a different ASCII game later becoming active for that same run, and the tournament deterministic system step subsequently failing because it could not resolve its prebootstrapped tournament session. The supplied diagnostics also show a model lane reaching the configured 1800-second timeout and previous circuit-disposal failures, which make a bounded, non-destructive UI rejoin important.

## Corrective source checks

- Council game lookup supports an exact game-family key in addition to the existing newest-game lookup.
- Tournament bootstrap, deterministic workflow bridge and Chat game surface request `kernel-creature-tournament` explicitly.
- Kernel Tournament bootstrap geometry is 144x40; deterministic tournament rendering remains bounded to 96-160 columns and 32-48 rows.
- ASCII and Pixel are presentation modes over one deterministic frame/session; no second game-state owner was added.
- Operator remembers a Game-plane origin and returns to Game when closed.
- Same-run rejoin retains the existing live Council attachment identity and bounds the attach gate to three seconds.
- Conversation/operator follow-tail respects manual scrolling instead of snapping on every mutation.
- The non-fullscreen popup header is height-bounded and becomes its own small scroll surface before it can collapse the game viewport.

## Static validation completed without .NET

- application architecture audit;
- service resilience audit;
- async continuation policy audit;
- DevExpress Blazor control/template audit;
- cross-platform boundary audit;
- configurable Council behavior-policy audit;
- ConfigurationRoot qualification audit;
- expanded Kernel Creature Tournament audit;
- JavaScript diagnostics manifest and `node --check` syntax validation;
- JSON parsing;
- XML/MSBuild parsing;
- release-identity audit;
- ZIP CRC, path-safety, clean extraction and byte-for-byte source comparison after packaging.

## Runtime limitation

Compilation and interactive browser/game confirmation remain for the user's .NET 10/DevExpress environments. The source changes are intentionally limited to the diagnosed tournament/session/rendering boundaries and preserve the existing `InteractiveServer` ownership model.
