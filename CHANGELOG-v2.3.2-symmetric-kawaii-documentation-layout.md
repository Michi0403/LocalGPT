# LocalGPT 2.3.2

## Symmetric Kawaii documentation shell

- The left table-of-contents rail and right in-article rail now use the same responsive width.
- Both rail-to-article gaps use one shared spacing variable.
- The centered shell can grow to 112rem, and the article consumes the complete remaining desktop width instead of stopping at a fixed character width.
- Short pages fill one viewport; normal document scrolling begins only when real content exceeds it. Side rails do not create nested scroll areas.
- Generated DocFX HTML, in-app help, PDF metadata, and the pinned Pages snapshot remain synchronized.
- The layout contract is intentionally shared with PublisherStudio to keep both product documentation sites visually predictable.
- Mobile and tablet DocFX behavior remains unchanged.

## Windows PowerShell maintenance compatibility

- Comparison-aware substring checks in repository validation scripts now use `String.IndexOf`, preserving ordinal and ordinal-ignore-case behavior while remaining executable under Windows PowerShell 5.1.
- Release, repository-validation, and verified-source-package lanes now run a shared compatibility guard before expensive work.
- The guard rejects the PowerShell 7-only `String.Contains(value, StringComparison)` overload without weakening any architecture, safety, migration, workflow, or source-formatting rule.

