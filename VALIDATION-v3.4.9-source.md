# LocalGPT 3.4.9 source validation

This release is validated as source because the requested environment intentionally does not use GitHub or a .NET SDK/build.

The delivery gate is run against a fresh extraction of the exact source ZIP and requires all of the following to pass:

- `build/audit_release_3_4_9.py`
- `build/audit_application_architecture.py --root <root> --product localgpt --mode all`
- `build/audit_async_continuations.py --source-root <root>/src/LocalGPT`
- `build/audit_service_resilience.py --root <root> --product localgpt`
- `build/audit_cross_platform_boundaries.py`
- `build/audit_configurable_behavior_policy.py`

The release audit verifies version identity and one-digit slot policy, the database-backed OCR/operator limits, removal of the old Remote Control and Council/structured-text hidden caps, opt-in repetition termination, repository-owned packaging tool wiring, all seven application RIDs, Windows-only setup publishing, native package-format paths, checksums, and the reviewed Interactive Server page boundaries.

No compiler-clean or native-package-build claim is made without a real .NET/macOS/Linux toolchain run.
