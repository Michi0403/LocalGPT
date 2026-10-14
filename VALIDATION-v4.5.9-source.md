# LocalGPT 4.5.9 source validation

This package was validated from the supplied LocalGPT 4.5.8 source archive without using GitHub and without running `dotnet`, restore, build or publish.

## Static checks repeated for 4.5.9

- release/version-slot identity and cache-busted browser asset version checks;
- InteractiveServer render-mode topology audit (20 maintained declarations; `Error.razor` remains the intentional routed exception);
- DevExpress control audit plus typed Chat provider `DxComboBox`, `DxSpinEdit` preservation and body-level dropdown popup stacking guard;
- Kernel Creature Tournament model/service/prompt audit for joint rigs, roster state, trainer actions, switch limits, bench rest, timeline and deterministic engine ownership;
- Learning Round/DXAIFunction wiring audit for stale-knowledge reporting and human-approved exact-source refresh;
- bounded Council rejoin retry audit while server-owned work remains active;
- provider-qualified Council, role-context isolation, configurable behavior, Chat/ASCII console, ASCII color/game authoring, ASCII DOOM, code-generation/DXAIFunction, architecture and cross-platform boundary audits;
- maintained JavaScript diagnostics hashes plus `node --check` for browser JavaScript;
- JSON and XML/MSBuild parse checks;
- ZIP CRC/path-safety validation and clean-extract byte comparison against the packaged source tree.

## Limitation

No .NET compiler/runtime was available or invoked, so this validation does not claim a successful C# or Razor compilation. The package is intended for the user's normal Windows/.NET build environment for that final runtime check.
