# LocalGPT 4.3.1 source validation

This is a source-only validation record. A .NET build was not run in this environment.

Validated statically:

- LocalGPT, installer-console, and WebView wrapper project identities use 4.3.1.
- Current documentation identity, PDF filename metadata, setup/runtime HTTP user-agent strings, and application JavaScript cache-busting identity use 4.3.1.
- `docs/templates/localgpt/public/main.js` passes `node --check`.
- The maintained Kawaii theme CSS and JavaScript exactly match the shipped `wwwroot/help-docs/styles` copies.
- The tracked Pages snapshot contains the same Kawaii CSS and JavaScript as the maintained theme source.
- Shipped documentation HTML and tracked Pages HTML use the SHA-256-derived 12-character cache keys for the current Kawaii CSS and JavaScript.
- The deep-space layer contains independent star timing/position variables, colored-star classes, CSS satellite/planet markup and styles, reduced-motion handling, translucent panel variables, and the desktop outer-gutter override.
- Existing historical Kawaii layout markers are retained so earlier layout audits remain able to recognize the equal-rail grid contract.

No claim is made that the C# projects were compiler-tested in this environment.
