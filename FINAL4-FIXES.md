# final4 localization/build fix

- Replaced non-ASCII U+2420 literals in the Windows PowerShell build gate with ASCII templates reconstructed at runtime.
- Reads localization JSON through strict UTF-8 decoding and rejects replacement/common mojibake markers.
- Added Git source visibility guard, .gitignore protection, MSBuild/build/package wiring and regression tests.
- The German key `Text.Start␠new␠chat` remains present with value `Neuen Chat starten`.

Validation in this environment covers JSON/catalog equality, Python source contracts, Node contracts, archive re-extraction and Git ignore semantics. A real .NET/Windows PowerShell build cannot be executed here because those runtimes are unavailable.
