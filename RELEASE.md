# LocalGPT 3.2.4

LocalGPT 3.2.4 is the **Build Guard Ownership, Iterator & User AI Function Editor Repair** release. It is a focused correction to 3.2.3 after the user's Windows .NET 10 build exposed repository-policy and Razor compile failures.

## What changed

- User JSON/OData generated-source key classification/key creation is now service-owned, satisfying the repository's Text Service Ownership boundary without adding an exemption.
- Learning-project repository enumeration is materialized and logged instead of using a catch-bearing iterator, satisfying the Iterator Exception Policy without weakening it.
- `DxFunctionCatalog.razor` again owns the `_userEditorInitialMode` backing field required by its Source/Pipeline editor controls.

## Retained behavior

The 3.2.3 JSON/OData AI Function frontend, advanced pipelines, X Functions & automation frontend, source-backed Learning Round project/version/workspace/full-file persistence, .NET 10 requirement grounding, and nullable X-Round recovery fix remain. Existing render-mode boundaries, Chat UI/CSS, EF migrations and 1-Wire protocol are unchanged.

## Build status

This package was not compiled in the preparation environment. Source/static validation only was performed; the user's own Windows .NET 10 build is authoritative. See `CHANGELOG-v3.2.4-BUILD-GUARD-OWNERSHIP-ITERATOR-EDITOR-FIX.md` and `VALIDATION-v3.2.4-source.md`.
