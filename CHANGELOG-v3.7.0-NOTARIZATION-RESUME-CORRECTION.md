# LocalGPT 3.7.0 — notarization resume correction

- Version advanced from 3.6.9 to 3.7.0 because the release script changed.
- Corrected interrupted Apple notarization resume to use the supported `notarytool info` status command instead of a nonexistent `notarytool wait` subcommand.
- Added bounded polling with `MACOS_NOTARY_WAIT_TIMEOUT` (default `2h`) and `MACOS_NOTARY_POLL_INTERVAL_SECONDS` (default `20`).
- Preserved SHA-256-bound submission state, so an unchanged DMG/PKG that was already uploaded is resumed rather than resubmitted.
- Keeps the 3.6.9 release-size work: self-contained Full-only macOS/Unix packaging, HTML-only embedded help, standalone compressed PDF, Ghostscript/Homebrew compression support, configurable documentation/release roots, RPM provisioning on macOS, disk cleanup, and same-version artifact reuse.
