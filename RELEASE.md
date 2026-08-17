# LocalGPT 3.0.4 user-configurable integration fabric, DXFunctions and Knowledge-backed toolchains

LocalGPT 3.0.4 builds on the user-validated 3.0.3 stability baseline and adds a local-first, user-owned integration/action layer rather than hardcoding online services into the application.

Key changes:

- Persistent REST/OData-style pull and tokenized webhook connectors with explicit per-connector enablement, network enablement, HTTPS/host policy, bounded redirects/timeouts/payloads, and no enabled online connector by default.
- Persistent Remote Control action pipelines that compose existing DXFunctions/public service methods through the normal registry/approval/catalog path.
- First-class user-owned `user.*` DXFunctions with Create/Edit/Delete UI/API and pipeline-backed runtime execution; source/system functions remain source-owned.
- Cross-platform Windows/Linux/macOS toolchain discovery using PATH as a list, Knowledge-defined roots/environment hints, database-backed regex extraction, and existing Project Maintenance persistence.
- Structured compiler/runtime/build-tool environment rows plus persisted toolchain kind, detected platform, Knowledge profile, and exact-version Knowledge linkage.
- Existing Human Collaboration is used when exact compiler/runtime version Knowledge is missing; the user may provide Markdown, a Knowledge Database article, a text blob, or skip. No automatic Internet lookup is performed.
- Controller, Service, DXFunction and frontend wiring is included for Remote Control, user DXFunctions, and toolchain discovery/knowledge/installation workflows.
- One additive EF migration creates the Remote Control/user-function tables and extends existing compiler-installation records without resetting existing data.

Versions:

- LocalGPT: 3.0.4
- LocalGPTWebviewWrapper: 3.0.4
- LocalGPTInstallerConsole: 3.0.4
- LocalGPT Wire Protocol: 2.1.1
- Council team seed: 25

See `CHANGELOG-v3.0.4-source.md` and `VALIDATION-v3.0.4-source.md` for the detailed source-only validation record.

This source package was not compiled in the inspection environment. No GitHub, online repository, `dotnet`, MSBuild, or Visual Studio build was used.
