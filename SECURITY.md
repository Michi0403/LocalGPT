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


## Company and institutional deployment

The LocalGPT team strongly recommends a DMZ-style isolation procedure for company use. Run LocalGPT, Ollama or other local model runtimes, and optional import/build workers inside a segmented network zone rather than on a broadly trusted workstation or server.

- Configure operating-system and perimeter firewalls to deny traffic by default and allow only the loopback ports, approved model endpoints, update sources, and explicit administrative paths that are required.
- Use a dedicated least-privilege operating-system account. It should not be an administrator and should not have write or delete permission for unrelated user profiles, shared drives, repositories, production data, secrets, or operating-system locations.
- Separate import/build workspaces from business data, back up important files, and test restore procedures.
- Disable or tightly restrict public exposure. LocalGPT's loopback defaults are not a substitute for network segmentation, authentication, reverse-proxy hardening, or an organizational threat model.
- Review GitHub/web imports, model-suggested DXFunctions, generated artifacts, and runtime-class actions before promoting them into trusted company workflows.

## Vulnerability handling

Report affected versions, impact, reproduction conditions, and a proposed fix without including live credentials or private data. Verify the fix with the checks available in the development environment and state any validation that could not be performed.

## Dependencies

Keep dependency auditing enabled where practical. Proprietary packages and licensed feeds must not be redistributed; preserve `THIRD-PARTY-NOTICES.md` and existing license metadata.
