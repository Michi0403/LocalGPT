# LocalGPT 5.8.3 — Razor ownership and XML documentation build repair

## Windows build failures fixed

- Replace the two native `<section>` elements in the Council inspection popup in `Chat.razor` with semantic `<div role="region">` containers. Preserve existing `aria-label` values, classes, scrolling, two-column evidence layout, and the enclosing `DxPopup` / `DxFormLayout` ownership. The existing `Assert-RazorMaintenanceArchitecture.ps1` rule remains unchanged.
- Document the `runId` argument of `Chat.ToggleCompletedCouncilActivityEvidence` in its XML comments (CS1573).
- Document the `asGameProject` argument in both `IProjectArchitectureService.EnsureCouncilRunProjectAsync` and `ProjectArchitectureService.EnsureCouncilRunProjectAsync` (CS1573). Method signatures and behavior remain unchanged.
- Increment LocalGPT 5.8.2 → 5.8.3. Keep the other project/package versions unchanged because those components were not modified.

## Scope and follow-up

The source-level fixes target exactly the two failures from the supplied Windows logs. They do not change the Game Development Team's source curation, model processing, project creation, PDF merging, Python requirements, or PublisherStudio. A full Windows build and interactive Council run have not been executed here.
