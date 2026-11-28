# LocalGPT 7.8.9 — Unruffled Uakari

- Check Python interpreters and native libraries before using them on Apple silicon. Reject Intel-only Python and accept native or universal Python.
- Keep platform-specific PyTorch selection and the optional Windows wrapper in their respective platform paths. Give native PyTorch more time to load from external storage and surface package installer error output.
- Prevent duplicate settings registrations from multiplying Python package profiles and mail recipients on save.
- Give the process toolchain list and toolchain knowledge catalog separate API routes.
- Match exact 1-Wire request retries before assigning generated message IDs and timestamps.
- Transcribe 16 kHz mono PCM WAV with Whisper even when FFmpeg is absent. Other input formats still require FFmpeg for decoding.
