# LocalGPT 4.9.5 — microphone, approval reuse, Python project handoff

## Fixed

- Restored a visibly rendered Chat microphone action by using LocalGPT's already-loaded Open Iconic microphone/stop glyphs and preserving the existing DevExpress button behavior.
- Corrected approval reuse semantics: a `CurrentApplicationSession` or `PersistentUntilChanged` approval now applies to later parameter sets of the same guarded operation, while pending requests and `ExactRequestOnce` approvals remain exact fingerprint/correlation matches.
- Prevented repeated approval cards for operations such as approved web searches when the human explicitly chose a reusable approval scope.

## Enhanced

- Greenfield Python generation now binds `.py` review content to the Council run's database-backed project/revision when no project was supplied.
- The existing code-generation execution path then writes the real Python files into the resolved project workspace, registers the revision workspace, and scans tracked files so structure/content regex metadata is persisted through the existing project-maintenance flow.
- Program-compiler Council guidance now requires Python handoff to include database project/revision context, Python toolchain evidence, regex-backed project structure evidence, the generated artifact URL, and the local `WorkspacePath`/project path as a manual fallback.
- Download URLs remain origin-relative so the caller can resolve them against the same LocalGPT HTTP or HTTPS endpoint used to access Chat.

## Compatibility / safety

- No broad render-mode changes were made. Existing `InteractiveServer` boundaries were left intact to avoid introducing nested render-mode boundaries around callback-bearing child components.
- No PublisherStudio source change was required for this request.
- No .NET build, restore, publish, installer execution, or GitHub access was performed.
