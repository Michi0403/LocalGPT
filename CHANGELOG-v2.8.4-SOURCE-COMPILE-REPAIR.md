# LocalGPT 2.8.4 — Source compile repair

## Build repair

- Repairs `ChatUploadWorkspaceService` where a configured `await using` statement incorrectly had a local declaration as its embedded statement, producing CS1023 and follow-on `destinationStream` scope errors.
- Repairs the same malformed statement shape in `CodeGenerationWorkflowService`, eliminating the CS1023 and follow-on `target` scope errors.
- Preserves the intended disposal boundary: copy streams are asynchronously disposed before the destination file is reopened for hashing or byte analysis.
- Keeps the strict async policy explicit with `ConfigureAwait(false)` on both asynchronous disposals and copy/flush operations.

## Compatibility

- LocalGPT: 2.8.4.
- LocalGPTWebviewWrapper: 2.8.4.
- LocalGPTInstallerConsole: 2.8.4.
- 1-Wire protocol: 2.1.1 (unchanged).
- InteractiveServer render-mode directives are unchanged.
- No database migration is required.
