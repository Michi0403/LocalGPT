# LocalGPT 4.0.3

LocalGPT 4.0.3 fixes the macOS `Build-Release.ps1` parser failure introduced by the 4.0.2 release-identity diagnostic. The literal colon after the release mode is now PowerShell-safe (`${mode}:`), and the release audit guards against the same un-delimited `$name:` interpolation pattern returning.

This release also corrects the 4.0.2 launcher ownership rule so `server.json` remains the useful cross-host runtime rendezvous contract it was designed to be. Stale processes belonging to the installed `/Applications/LocalGPT.app` are still replaced during an update, but debug, alternate-host and other legitimate local runtime endpoints are no longer killed or deleted simply because their executable/version differs from the packaged app. PKG endpoint cleanup is likewise limited to endpoints it can attribute to the installed application (plus dead stale owners).

The 4.0.2 runtime/source identity stamps, updater handoff, writable macOS runtime/log paths and setup fail-soft behavior remain intact. Ollama update/install, compatible-model discovery/scoring, progress handling, Council recovery and InteractiveServer ownership are unchanged.

See `CHANGELOG-v4.0.3-RELEASE-PARSER-RENDEZVOUS-REPAIR.md` and `VALIDATION-v4.0.3-source.md`.
