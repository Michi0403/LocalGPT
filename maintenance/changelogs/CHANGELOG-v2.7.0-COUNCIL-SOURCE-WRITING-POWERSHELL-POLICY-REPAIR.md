# LocalGPT 2.7.0 — Council source writing, PowerShell and scale-policy repair

## Changed

- Rolled the application, installer console and WebView wrapper from 2.6.9 to **2.7.0** according to the one-digit minor/patch rollover rule. The independent wire-protocol package remains 2.1.1.
- Restored `@rendermode InteractiveServer` as the first directive of `Help.razor`, matching the maintained render-mode contract while preserving the three intentionally inherited ThemeSwitcher children.
- Preserved the three-dollar raw interpolated Ollama textual-function fallback so literal JSON braces compile correctly while `marker` and the exact function directory remain interpolated.
- Added `PowerShellScript` as an explicit code-generation output kind. Reviewed `.ps1` source can be generated as files without being executed.
- Added a CodeDOM failure fallback: explicit reviewed source wins; otherwise LocalGPT emits a plain reviewed C# fallback instead of making CodeDOM a single point of failure.
- Added the read-only `codegen.capabilities` DXFunction and `/api/code-generation/capabilities` controller route so an AI Council member can discover the exact generation contract, output kinds, policies and workspace continuation functions.
- Added DI-backed generated-workspace functions for listing workspaces/files, reading a source file, approval-gated plain source-file writing, and ZIP refresh. `council.artifact_workspace_file.write` supports PowerShell, C#, JavaScript, Razor, SQL and every other text extension provisioned through `ArtifactTextExtensions`; it never executes the file.
- Removed the old fixed remote-import archive ceiling of 60,000 ZIP entries and the 50-linked-page crawl ceiling. Remote import now derives file, ZIP-entry, extracted-byte and per-file boundaries from database-backed runtime policy values.
- Removed remaining source-coded repository enumeration ceilings from project workspace permission assessment and chat-upload workspace listings; these now use the database-backed `MaxFiles` policy, while callers may deliberately request a smaller positive bound.
- Kept project file scans and generated-workspace rescans policy-backed (`MaxFiles`, `MaxSingleFileBytes`) rather than assuming small repositories.
- Expanded the code-generation guide with PowerShell, CodeDOM fallback, generated-workspace write functions, controller discovery and enterprise-scale policy behavior.

## Council report handling

The supplied Council reports were treated as diagnostic suggestions, not source truth. Fabricated `OrganicCapabilities`/`OrganicWiring` snippets, fake PR references and unverified database-table proposals were not imported. Changes in this release are tied to source/log evidence: the inaccessible registry-backed workspace-write path, the recorded 60,000-entry import failure, the render-mode assertion, and the code-generation/file-scale paths in the supplied repository.

## Source-only validation

No .NET compiler, restore, build, publish or GitHub access was used to prepare this source package. Source audits passed for architecture policy, service resilience, XML documentation coverage, code-generation/DXFunction wiring, the exact InteractiveServer assertion contract, and the removed fixed limits described above.
