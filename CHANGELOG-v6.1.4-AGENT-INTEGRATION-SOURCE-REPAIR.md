# LocalGPT 6.1.4 — Council capability access and media model verification

## Release scope

Source-only integration changes based on the supplied LocalGPT 6.1.3 archive and the companion PublisherStudio 4.3.1 archive. This is **not** a certified .NET/DevExpress build and is **not** a complete implementation of every prior architecture-audit recommendation.

### Implemented

- **Council AI functions (audit B; partial A).** Registered `council.teams.list`, `council.teams.inspect`, `council.teams.validate`, `council.teams.match`, and `council.teams.save` via the existing DI-handler discovery. Matching reports gaps based on persisted team role/function capabilities. Validation reuses the authoritative Council configuration validator on a detached draft. Persistence delegates to the existing confirmed, copy-on-write Council team service; it does **not** write unreviewed seed changes.
- **Approval-safe AI team mutation.** Saving remains a non-automatic DXFunction requiring central exact-parameter human confirmation/deferred approval. A model's own assertion of approval is not sufficient.
- **CanIRun/provider installation handoff (partial audit D).** Added read-only `initial.setup.canirun.install.plan`: it joins the recommendation's explicit Ollama model identifier with knowledge-backed Ollama provider profiles and returns the exact existing `initial.setup.provider.model.install` arguments for a **separately approved** installation. Absent identifiers or providers yield an explicit unsupported mapping rather than an invented download address. The plan does not assert runtime verification.
- **Specialized model installation verification (audit K).** Added a model-load probe to the maintained isolated managed-Python bridge. Every advertised supported capability must load through its existing adapter before LocalGPT publishes an installed-model manifest. A failure aborts install and removes the newly promoted model directory. Unsupported capability types remain explicitly unsupported.
- **Inline video/audio result presentation (partial audit G).** Artifacts issued through the same-origin managed artifact service render playable HTML5 video/audio in Markdown-capable chat output, while retaining their open/download links and the non-media fallback. The artifact URL is HTML encoded and constrained to the owned media endpoint.
- **Source regression checks.** Added five standard-library Python tests under `tests/static` for function registration schemas, central approval boundary, model-load verification, version, and CanIRun mapping evidence.
- Version increased from **6.1.3 → 6.1.4**; second/third segments remain single-digit.

### Important outstanding work, not misrepresented as fixed

- Full goal-level autonomous planner and self-replanning execution controller across every Council, single AI and function (audit A).
- Fully general data-driven media execution adapter registry covering heterogeneous architectures, speech synthesis, arbitrary audio generation, model licenses, revision pinning and end-to-end model inference validation (audits D/E).
- Typed cross-application artifact/event bus and universal media/editor handoff (audit G).
- Recoverable persisted in-flight Council step ledger after process restart (audit F).
- CanIRun compatibility request caching/batching optimization (audit J).
- Comprehensive integration/evaluation test matrix, end-to-end real AI installation and actual .NET build (audit I).
- Generalized diagnostic error taxonomy and long-running operator-controlled task cancellation/retry (audit L).

### Verification constraints

- Python bridge compiles with `python -m py_compile` and static integration tests pass.
- Required PowerShell architecture, runtime text, XML documentation, source-format, async, EF/migration and service guards **were not executed**: `pwsh` is not installed in this environment. **No guard, build targets or policy bypass was changed**.
- `dotnet` is not installed; no attempt was made to compile, restore packages, launch LocalGPT, exercise its database or download remote models.
- Before distribution run the repository's unmodified mandatory maintenance scripts and normal .NET/DevExpress build in the supported development environment, then test approved AI team saving, configured Council continuity and at least one model-load probe on the actual host device.
