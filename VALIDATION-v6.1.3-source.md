# LocalGPT 6.1.3 verification notes

Source-only checks: (1) no `localgpt-game-console.js` global script in App; (2) collocated module exports all methods invoked by `ChatGameConsole`; (3) module import is after the interactive render, not in prerender setup; (4) JS interop disposal owns the module and reports errors; (5) GameStudio shared renderer remains unchanged; (6) diagnostics hash manifest includes collocated modules; (7) Node syntax checks and ZIP integrity.

Windows acceptance gate (not performed here): run Debug build with all architecture guards, open ordinary Chat, Council game, Game Studio scene, switch ASCII/pixel modes, resize/fullscreen/toggle, validate keyboard and touch inputs, open/close repeatedly, and verify browser module requests and ILogger diagnostics. Runtime behavior cannot be certified in this container.

Release routing source checks: Windows and Unix version-scoped staging, explicit command-line PublishDir/PublishUrl overrides, early output-root version-directory rejection, assembly version verification, payload version stamps, ZIP root/version-marker verification and stable outer asset names. These checks can be inspected statically here; an actual packaging run requires Windows/PowerShell and the installed .NET 10 toolchain. A LocalGPT 6.1.3 release should create staging/6.1.3 and bundle/6.1.3 (never staging/6.0.12) when 6.1.3 is declared in the application csproj.

Versioning note: 6.1.2 was already released by the user. This 6.1.3 source is a version-only reissue of the previously prepared 6.1.1 source; independent edits from the user’s 6.1.2 checkout are not represented as merged.
