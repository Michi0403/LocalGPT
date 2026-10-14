# LocalGPT 5.0.6 — Blazor render-state, Razor parser, DevExpress and localization hardening

## Fixed

- Preserved the DevExpress-first Razor UI contract; no maintained DevExpress control was replaced with native HTML.
- Added a pre-compile Razor component-attribute guard for the recurring `RZ9986` class of failures. Generic render expressions and mixed literal/C# component attributes now fail with exact file/line diagnostics before Razor compilation.
- The new diagnostic explains the accepted architectural choices: bind a typed instance property/field, prepare async/stateful values at the correct lifecycle boundary, or use a named/simple async event callback such as `async () => await MethodAsync(context)` when the callback contract is asynchronous.
- Added the Blazor multi-render-state mental model to `AGENTS.md`: prerender/static SSR, InteractiveServer, InteractiveWebAssembly and inherited/nested component execution must be treated as distinct lifecycle/instance paths.
- Localization validation now reports exact source/catalog lines and architectural remediation instead of only a terminal build failure.
- Literal `LT("...")` / `GetText("...")` calls are now checked against `en-US.json`, so newly localized UI text cannot be committed without catalog ownership.
- The reviewed German language-neutral baseline retains `Common.Name`, which is legitimately spelled `Name` in both English and German rather than receiving a fake translation.
- Architecture-audit failures now emit MSBuild-style file/line diagnostics plus the intended architectural choices for static-state, method-resilience, runtime-value and structure findings.

## Maintained contracts

- Existing page/island InteractiveServer render boundaries and prerender choices are unchanged.
- DevExpress Blazor remains the ordinary interactive-control owner.
- Existing service, diagnostics, persistence and localization ownership remains intact.
- No build guard is bypassed or disabled by this release.

## Version

- LocalGPT: `5.0.5` → `5.0.6`.
