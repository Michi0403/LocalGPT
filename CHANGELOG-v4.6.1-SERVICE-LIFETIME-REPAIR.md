# LocalGPT 4.6.1 — startup service-lifetime repair

## Scope

This is a deliberately narrow corrective release for the LocalGPT 4.6.0 startup regression reported from the Windows runtime. It does not begin the larger multi-host/project-curation feature work planned after this hotfix.

## Fixed

- Restored startup service-provider validity for `IHumanCollaborationService`.
- `HumanCollaborationService` remains singleton-owned because hosted 1-Wire and collaboration infrastructure consume it as a singleton.
- Removed the captured scoped `IKnowledgeFreshnessReviewService` constructor dependency from that singleton.
- Freshness decision handling now creates a short-lived dependency-injection scope only when a `knowledge.freshness.*` human decision must be applied, then resolves `IKnowledgeFreshnessReviewService` from that scope.
- Added a release guard that rejects reintroduction of the singleton-to-scoped capture while preserving the intended service registrations.

## Preserved

All 4.6.0 UI/editor and Council compile repairs are retained, including the block-bodied Council rejoin callback, responsive DevExpress provider editor, bounded number editors, starter-button layout, Architecture action grid, and the DevExpress popup stacking repair inherited from 4.5.9.

No PublisherStudio source change is part of this release.
