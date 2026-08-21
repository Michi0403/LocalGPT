# LocalGPT 3.1.11 — Council Formatting, Topic-Neutral Learning & Session Restore

## Council formatting

- Recognized LocalGPT self-assessment envelopes are normalized before Markdown/autolink processing, preventing URLs inside JSON from corrupting the payload.
- Valid assessment JSON is shown as structured data plus an inert `language-json` code disclosure; invalid payloads remain inert code instead of activating markup.
- User-visible provider/function JSON uses relaxed JSON escaping so characters such as em dashes, quotes and angle brackets no longer leak as `\u2014`, `\u0022`, `\u003C` or `\u003E`.
- HTML entities inside JSON string values are decoded only for the controlled code-display projection and are HTML-encoded again at the final render boundary. This makes values such as `&amp;` readable without globally decoding untrusted model prose.
- Self-assessment envelope recognition is compiled through the existing database-backed regex service rather than a new ad-hoc runtime regex.

## Topic-neutral Learning Round

- Seed version advances to 27 through the existing lossless Council-team seed-evolution path. User-modified teams remain user-owned instead of being overwritten.
- Learning Round is now explicitly domain-neutral: uploads and collaboration may concern science, school, research, creative work, software, LocalGPT or any other subject.
- The maintained seed now owns a literal four-step workflow: evidence inventory, study, verification and bounded learning maintenance. The workflow is persisted/editable through the existing Council-team BusinessObjects/services rather than hardcoded in runtime orchestration.
- The Learning team inventories actual uploads first, performs bounded reads, treats flattened text exports as inspectable evidence, resolves tool-answerable ambiguity with read-only functions, separates source facts/user assertions from inference, and prevents meta-planning from replacing evidence work.
- Knowledge is the primary learning outcome. Regex creation is optional and is used only when it has a reusable retrieval/validation purpose and can be tested before persistence.
- The repetitive all-member readiness introduction is disabled for the maintained Learning seed; substantive persisted workflow steps own the run.

## Live session configuration restore

- Running Council snapshots now retain the originating Council team key, model-preset identity, performance-preset identity, critique rounds, memory flag and automatic-project flag in addition to the existing provider-qualified models/routes and hardware/token settings.
- Rejoining a running Council maps that authoritative service-owned snapshot back into the existing Chat Configuration state on the Blazor renderer. The same state automatically drives the three quick selectors.
- Performance-preset identity is retained when a profile is applied to a running Council.
- Initial hardware-preset loading can recover the saved preparation identity after the service list arrives, avoiding initialization-order loss.

## Protected surfaces

`Chat.razor`, `Chat.razor.css`, and the 3.1.10 attachment/draft browser bridge are byte-identical to 3.1.10. No new CSS, composer sizing, quick-row markup or DxAIChat structural change is introduced. No EF migration, BenchmarkEvidence schema change or 1-Wire protocol change is required.
