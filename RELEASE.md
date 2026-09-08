# LocalGPT 3.9.2

LocalGPT 3.9.2 repairs the installed-app runtime failure that prevented the unified Setup guide from initializing on macOS.

The supplied runtime log shows an invalid inherited process current directory causing optional hardware probes to fail in `Interop.Sys.GetCwd()`. That exception propagated through the Setup snapshot and produced the red `Initial setup refresh failed` state. The same invalid-current-directory dependency also caused the optional file logger to throw while DevExpress was rendering the form, terminating the Blazor circuit.

3.9.2 removes that dependency from the Setup path: the generated macOS launcher uses the durable per-user LocalGPT runtime directory, startup can repair an already-invalid inherited current directory, hardware probes are explicitly rooted and best-effort, optional hardware failure no longer aborts the Setup snapshot, the file logger uses per-user logs, and the bounded provider/model console uses the durable runtime directory when no explicit working directory is supplied.

The intended unified workflow is preserved: hardware review, optional CanIRun.ai recommendations, provider selection, Ollama Start/Stop/Restart/Refresh, endpoint registration, direct first-model install, one-click recommendation-driven model installs, installed-model selection, and benchmark-team creation remain in the Setup guide.

See `CHANGELOG-v3.9.2-PACKAGED-SETUP-RUNTIME-REPAIR.md` and `VALIDATION-v3.9.2-source.md`. The feature scope introduced in 3.9.0 and the build-guard repair in 3.9.1 remain preserved.
