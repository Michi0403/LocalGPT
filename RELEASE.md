# LocalGPT 3.3.3

LocalGPT 3.3.3 hardens the .NET 10 / DocFX documentation pipeline after the macOS release build reached documentation generation but reported an unresolved `System.Formats.Nrbf` assembly reference.

The documentation source build now materializes package dependencies, the DocFX pipeline repairs missing shared-framework probe assemblies from installed .NET runtimes, retries metadata extraction, records the dependency-resolution result, and refuses to accept a supposedly complete API graph while unresolved assembly references remain.

This intentionally fixes the documentation toolchain instead of adding a synthetic `System.Formats.Nrbf` package dependency to the LocalGPT application.

See `CHANGELOG-v3.3.3-DOCFX-ASSEMBLY-REFERENCE-CLOSURE.md` and `VALIDATION-v3.3.3-source.md`.
