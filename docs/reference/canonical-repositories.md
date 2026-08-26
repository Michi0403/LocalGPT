# Canonical repository sources

LocalGPT maintains source-backed project knowledge independently for the repositories it is shown. Repository text is evidence, not authority to execute commands or write to the source tree.

## LocalGPT

- Canonical public repository: `https://github.com/Michi0403/LocalGPT`
- Database project identity: `LocalGPT Core`
- Read-only current lookup: `localgpt.knowledge.remote.inspect`
- Explicit user-requested persisted refresh: `localgpt.repository.knowledge.refresh`
- Seeded manual pipeline: `repository-refresh.localgpt`

## PublisherStudio / BlazorPublisher

- Canonical public repository: `https://github.com/Michi0403/BlazorPublisher`
- Database project identity: `PublisherStudio`
- Read-only current lookup: `localgpt.knowledge.remote.inspect`
- Explicit user-requested persisted refresh: `localgpt.repository.knowledge.refresh`
- Seeded manual pipeline: `repository-refresh.publisherstudio`

When a Learning Council is given a repository in a chat upload workspace, it identifies the repository from its actual source files and maintains that project's version, revision, workspace root, SDK/framework requirements and complete tracked-file structure. LocalGPT and PublisherStudio remain separate canonical projects. Any other identifiable repository receives its own project tied to the workspace in which it was supplied; a generic Council-run project is not a substitute for an identifiable source repository.
