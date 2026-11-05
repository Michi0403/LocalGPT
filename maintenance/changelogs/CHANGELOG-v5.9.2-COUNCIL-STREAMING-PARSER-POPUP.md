# LocalGPT 5.9.2 — Council streaming formatter and inspection height repair

## Reproduced from supplied debugger evidence

- Visual Studio showed more than 650 first-chance `System.Text.Json.JsonReaderException` events. The supplied call stack enters `StructuredTextTranslationService.TranslateSelfAssessmentBlocksToMarkdown` at `JsonDocument.Parse`. A representative invalid payload starts with `i`.
- The formatter's database-backed multi-line self-assessment regex (`[\s\S]*?` between tags) also produced `RegexMatchTimeoutException` while repeatedly scanning growing Council text.
- The DevExpress evidence popup allowed the right provider-stream content to stop above the dialog's bottom edge because height did not propagate reliably through the portal/body/FormLayout chain.

## Repairs

1. A forward-only deterministic scanner recognizes both literal and HTML-encoded `<localgpt-self-assessment>` and `<localgpt-self-annotated-assessment>` delimiters, including mixed recognized closing spellings. Incomplete streamed envelopes remain unchanged until closed. Markdown-heading prefixes retain their previous display behavior. The persisted regex-catalog entry is retained for data compatibility, while fixed protocol delimiter scanning is no longer driven by that unbounded wildcard pattern.
2. Only complete, plausible JSON objects/arrays reach `JsonDocument.Parse`. Invalid or oversized material is still displayed as HTML-encoded inert code rather than discarded. Complete balanced but invalid JSON is rejected and fingerprinted so repeated snapshots avoid repeatedly throwing the same parser exception. The negative cache holds up to 512 fingerprints, not payload content, and is synchronized on the existing formatter service.
3. Rejection diagnostics are sampled (first occurrence and each 256th subsequent observation), record reason, counts, payload/source character lengths and JSON line/byte positions when applicable, and omit all model text. Unexpected faults still log normally. This reduces logging and first-chance debugger overhead without suppressing all errors.
4. Council inspection styling gives the DevExpress modal body and intermediate FormLayout wrappers an explicit flexible height chain. Both columns, including an empty provider-stream placeholder, stretch to the bottom and scroll independently, while retaining 90vw × 90dvh popup sizing and narrow-screen stacking.
5. The existing PowerShell source and static-asset guards assert that deterministic scanning, rejection caching and the popup height rules remain in place. The architecture chapter gains a Mermaid state diagram distinguishing streamed/incomplete/valid/invalid display reactions.

## Constraints

- No PDFsharp, PublisherStudio, Council tool policy, database schema, DI lifetime, or authorization changes. The original self-assessment pattern stays in the persisted catalog for migration compatibility; the fixed tag protocol is intentionally parsed without regex.
- Static source/CSS/XML checks only. No .NET, Windows PowerShell, Blazor runtime or Ollama test was run in this environment. The new popup layout and exact exception-rate improvement require Windows testing.
