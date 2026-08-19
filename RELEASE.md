# LocalGPT 3.1.6

LocalGPT 3.1.6 is the Chat quick-preset and live configuration-refresh successor to 3.1.5. It keeps the complete 3.1.5 repetition-watchdog behavior and all earlier Council/benchmark recovery and evidence work while making the most common Council preparation choices directly available beside the `/chat` prompt actions.

## Toolchain state

- .NET SDK policy: `10.0.400`
- Target framework: `net10.0`
- DevExpress: existing `25.2.*` package lane retained
- 1-Wire protocol: `2.1.1`

## Chat quick configuration

Three compact DevExpress selectors are available on the prompt action line:

- Council team;
- Council model preset;
- hardware performance preset.

Each selector uses the same service-backed collections and application handlers as the detailed Chat configuration surface. Advanced editing remains in Chat configuration.

## Live configuration refresh

Opening Chat configuration now reloads its database/service-backed lists instead of depending on the values captured when `/chat` first initialized. Council teams, model presets, hardware performance presets, persistent prompt starters, projects and chat memory are refreshed independently from provider discovery. Current manual values are not reset merely because the lists were refreshed.

## Compatibility

No database migration, benchmark evidence schema migration, 1-Wire protocol change, repetition-watchdog removal, Council recovery removal, or XML documentation regression is introduced by 3.1.6.

See `CHANGELOG-v3.1.6-CHAT-QUICK-PRESETS-LIVE-CONFIG-REFRESH.md` and `VALIDATION-v3.1.6-source.md`.
