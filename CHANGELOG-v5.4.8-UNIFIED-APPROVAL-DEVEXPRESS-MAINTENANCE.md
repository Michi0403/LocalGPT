# LocalGPT 5.4.8 — Shared approval DevExpress maintenance repair

## Corrections

- Restore the required top-level `INotificationService` and `IComponentActivityService` injections on `HumanCollaborationRequestCard`. Its typed logger and method-local diagnostics remain in place.
- Keep the shared, database-backed approval/feedback form introduced in 5.4.6, including the exact request identity, explicit approve/decline, human notes, reuse scope, single-use option, optimistic decision version and pending-status check. Chat and Human Collaboration still use the same canonical resolver and refresh on saved decisions.
- Rebuild the shared card's text, reason, reuse and single-use editors under the maintained DevExpress `DxFormLayout` and `DxFormLayoutItem` hierarchy. Restore visible Council question scope, gate, round/phase and exact-request fingerprint prefix that were present in the former Inbox presentation.
- Replace the newly added native Chat decision-history `<details>/<summary>` disclosure with a DevExpress `DxAccordion`/`DxAccordionItem` while keeping the saved-decision history.
- Teach the component-retention guard to count *explicitly declared* DevExpress components moved into a shared Razor child. Keep the Inbox minimum at **41**, the Chat minimum at **99** and their original native-control ceilings. Require the referenced child at each call site, and protect the shared request card independently with a **15-tag** minimum. This preserves the old maintenance requirements without blocking genuine component reuse.
- Remove obsolete XML documentation for the former hardware tier route builder. The current `SelectMeasuredTierProfile(profiles, tierName)` documentation has one parameter entry per actual argument; the reported CS1571/CS1572 warnings are addressed without changing the tier algorithm.
- Make the 5.4.6 shared-approval source audit accept subsequent release versions while continuing to require 5.4.6 or later.

## Release and compatibility

- Increase the LocalGPT source version from 5.4.7 to **5.4.8**, including script resource query versions. No schema, service registration, security policy, dependency or wire-protocol version was changed.
- Preserve the ASCII fullscreen/Operator timeout repair from 5.4.7 and PublisherStudio 4.1.8 unchanged.
- Source-only audit and manual Windows build requirements are documented in `VALIDATION-v5.4.8-source.md`. This environment does not perform .NET build, restore, publish or GitHub operations.
