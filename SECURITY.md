# Security

LocalGPT is a local application, not a security sandbox. Model output, retrieved documents, uploads, generated source, database rows, logs, provider responses, tool descriptions, and repository text must be validated before they influence an operation.

## Practical boundaries

- Keep file access within the configured workspace or project root and normalize paths before use.
- Reject archive traversal, unsafe links, and writes outside the selected destination.
- Do not log credentials, tokens, complete prompts, generated source, request bodies, or private database content.
- Use timeouts, cancellation, bounded result sizes, and process-tree cleanup for external operations.
- Read-only or coordination-only functions may run automatically only when their registered metadata allows it.
- Writes, builds, downloads, installation, deletion, publishing, account access, and other consequential actions use the application's explicit one-use confirmation path.
- Treat a failed operation as a system state to diagnose, not as evidence that the user did something wrong.

## Vulnerability handling

Report affected versions, impact, reproduction conditions, and a proposed fix without including live credentials or private data. Verify the fix with the checks available in the development environment and state any validation that could not be performed.

## Dependencies

Keep dependency auditing enabled where practical. Proprietary packages and licensed feeds must not be redistributed; preserve `THIRD-PARTY-NOTICES.md` and existing license metadata.
