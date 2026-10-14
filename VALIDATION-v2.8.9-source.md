# LocalGPT 2.8.9 source validation

This is a source-only repair release. No `dotnet`, MSBuild, Visual Studio build, or GitHub access was used during preparation. The user's Windows .NET build remains authoritative.

Static validation covers:

- LocalGPT application/wrapper/installer version `2.8.9` and single-digit minor/patch slots.
- Council rejoin uses one initial `DxAIChat.LoadMessages` bind and snapshot-driven incremental refreshes.
- Rejoin serialization and authoritative live-transcript autosave merging are present.
- SignalR timeout/keepalive settings remain bounded and loopback-oriented.
- Existing strict-async, Council, trace, architecture, resilience, 1-Wire and XML documentation audits.
- `@rendermode` directives remain byte-for-byte equal to 2.8.8.
