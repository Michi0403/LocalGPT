# LocalGPT 4.2.1 - metadata-backed console identity

LocalGPT 4.2.1 adds a compact identity header to the normal LocalGPT application console and the LocalGPT installer console. The header is intentionally sourced from project/assembly metadata rather than duplicated product constants.

## Startup identity

The first application/installer lines now show:

- product display name and semantic version;
- canonical repository URL;
- project owner/author;
- SPDX project license expression.

`Directory.Build.props` remains the canonical repository-level source for `Authors`, `RepositoryUrl`, `RepositoryType`, and `PackageLicenseExpression`. Those values are emitted as assembly metadata and consumed at runtime by the shared `ConsoleProductIdentity` helper. Each executable project supplies only its own standard MSBuild `Product` display name.

The installer repository slug used for release/source operations is now derived from the same metadata-backed repository URL, removing the previous separate `Michi0403/LocalGPT` runtime constant and preventing installer behavior and the visible repository line from drifting apart.

No version, repository URL, owner, or license value is hardcoded in the console writer.

## Preserved behavior

The 4.2.0 live Ollama catalog/workbench repair, 4.1.9 macOS packaging spawn reduction, and earlier runtime/cancellation protections remain unchanged.
