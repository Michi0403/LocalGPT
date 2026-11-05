# LocalGPT 2.4.4 — project workspace read test

Focused regression repair only. This candidate restores AI-visible read-only access to source/text files in the project linked to `/chat` without changing the existing 1-Wire, documentation, Pages, release, or project-write paths.

- Adds `project.workspace.files.list` as an automatically callable read-only DXFunction. `projectId` is optional and defaults to the project selected in Chat Configuration.
- Adds `project.workspace.file.read` as an automatically callable read-only DXFunction for C#, Razor, solution/project files, Markdown, JSON, XML, scripts and the text types already accepted by LocalGPT's existing workspace/text policy.
- Reuses `CouncilRuntimeService.EnumerateWorkspaceTextFiles`, `CouncilRuntimeService.ResolveWorkspaceTextFile`, and `SafeTextDocumentService` instead of introducing a new hardcoded file policy.
- The function descriptors are synchronized by the existing DXFunction catalog and are therefore visible to normal chat and Council function selection without adding a new system-prompt rule.
- Existing chat-upload ZIP extraction remains unchanged.

Static source audits in the assistant environment passed: service resilience, application architecture, async continuations, and documentation/1-Wire contract. No .NET SDK is available in that environment, so this source candidate is not claimed compiled.
