# LocalGPT 4.6.1

LocalGPT 4.6.1 is a narrow startup hotfix for the service-lifetime regression in 4.6.0. The user-provided Windows runtime log showed service-provider validation rejecting `IHumanCollaborationService` because the singleton `HumanCollaborationService` directly captured scoped `IKnowledgeFreshnessReviewService`.

The singleton lifetime is intentionally preserved because hosted 1-Wire and collaboration infrastructure depend on it. Instead of weakening those ownership boundaries, `HumanCollaborationService` now injects the singleton-safe `IServiceScopeFactory` and creates a short-lived scope only when a `knowledge.freshness.*` decision needs the scoped freshness-review service.

A 4.6.1 release audit now asserts the intended singleton/scoped registrations, forbids direct constructor capture of `IKnowledgeFreshnessReviewService` by `HumanCollaborationService`, and requires scoped resolution at the post-decision call site. The 4.6.0 UI/editor and Council compile corrections remain unchanged.

This release intentionally does not start the broader multi-LocalGPT pairing/orchestration, curator-gate, generic project ingestion, blob reconstruction, or semantic ASCII mouse/action work. Those changes are deferred until this repaired baseline is tested by the user.

No PublisherStudio source change is required for this LocalGPT-only hotfix.
