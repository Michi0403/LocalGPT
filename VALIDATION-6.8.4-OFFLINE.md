# LocalGPT 6.8.4 — Offline source verification

Source input: `LocalGPT_6.8.3_source.zip`.

- 24/24 focused offline assertions passed. These cover disposal-safe cancellation token ownership across Chat partials and the game console, game reattachment display, run-first game correlation, cancellation and approval boundaries, fail-closed Council request creation, exact unchanged tournament implementation sources, and source-guard retained failure behavior.
- Negative controls against original 6.8.3 passed: the old code still contained a post-disposal `CancellationTokenSource.Token` read; it lacked the console cached token and initial prebootstrapped-game presentation fix.
- Deterministic game engine service, Kernel Tournament service, Council Tournament orchestration and game bootstrap service were byte-for-byte identical to 6.8.3.
- LocalGPT project XML parsed and version increased from 6.8.3 to 6.8.4.
- Source ZIP integrity and inventory will be verified after packaging.

Not tested: native Windows PowerShell 5.1 or modern pwsh execution, PowerShell parser, Razor / DevExpress generated code, .NET compilation, Windows/macOS runtime, real model execution, browser game rendering. All are outside the available toolchain by the maintainer's constraint. No claim of successful build is made.

A reported runtime identifies itself as `6.4.1.0`, which is inconsistent with the supplied `6.8.3` source archive. The actual installed executable version must be verified independently.
