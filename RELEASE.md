# LocalGPT 4.2.1

LocalGPT 4.2.1 adds metadata-backed product identity to the normal application console and the installer console. Their first lines now expose the running product/version, canonical repository URL, owner, and project license without duplicating those values in startup code.

Repository-level `Authors`, `RepositoryUrl`, and `PackageLicenseExpression` remain canonical in `Directory.Build.props`; generated assembly metadata carries them into the executable, and a shared console identity helper renders them. The LocalGPT installer now derives its GitHub owner/name slug from that same repository metadata instead of maintaining a separate repository constant.

Runtime/model behavior is otherwise unchanged from 4.2.0, including the live Ollama catalog/workbench repair and existing CanIRun evidence handling. The 4.1.9 macOS packaging spawn-flood repair also remains intact.

See `CHANGELOG-v4.2.1-METADATA-CONSOLE-IDENTITY.md` and `VALIDATION-v4.2.1-source.md`.
