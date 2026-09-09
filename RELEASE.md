# LocalGPT 4.0.1

LocalGPT 4.0.1 is a focused follow-up to 4.0.0. It repairs the text-service ownership build failure reported by the first local compile and replaces the slow Windows Ollama guided updater path with a direct streaming download of the official Ollama installer while keeping clean percentage progress in the shared console.

The 4.0.0 provider-model discovery, update confirmation, console cleanup, provider-candidate reuse, InteractiveServer retention, Council recovery, and render-mode ownership remain unchanged. Mutating provider commands now also have a one-hour requested timeout so large model/runtime downloads are not killed by the previous ten-minute request limit.

See `CHANGELOG-v4.0.1-TEXT-OWNERSHIP-FAST-OLLAMA-DOWNLOAD.md` and `VALIDATION-v4.0.1-source.md`.
