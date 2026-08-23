# LocalGPT 3.2.9 — Database, knowledge relationships and lifecycle hardening

## Database workbench

- Converts `/database` to the same full-width responsive workbench language used by newer LocalGPT configuration pages.
- Keeps **Knowledge & relationships** and **SQLite tables** as separate workbench panels so the knowledge lifecycle editor is not visually mixed with the generic database row editor.
- Keeps the existing `InteractiveServer` boundary and uses renderer-affine busy/completion refreshes for state-changing UI operations.
- Knowledge selection now uses a semantic `Topic · Scope · short-id` label instead of topic text alone. This matters for the supplied database because many imported learn-base records intentionally share a topic/scope family.
- Generic SQLite row selection now prefers human-readable columns such as `DisplayName`, `Name`, `Title`, `Topic`, `Key`, function/model/provider names, label, version, project path, scope, status and description before falling back to a stable identifier or rowid.
- The table preview adds a **Record** column based on that semantic label and moves `rowid` to a secondary diagnostic column instead of presenting it as the primary identity.
- Long semantic values and identifiers are compacted for selectors while the row editor continues to operate on the authoritative row identity.

## Structured knowledge ↔ RegEx semantics

- Adds persisted `CouncilKnowledgeRegexPatternLinks` with the composite key `(KnowledgeEntryId, RegexPatternId)`.
- Each link stores a maintained semantic purpose (`Alias`, `Classification`, `Extraction`, `Validation`, `Routing`, `Identifier`, or `Structure`), a human-readable meaning, enabled state, confirmation marker and update timestamp.
- Adds `IKnowledgeRegexLinkService` / `KnowledgeRegexLinkService` for listing, saving, removing and locally testing enabled knowledge recognition links.
- Recognition tests reuse the central bounded `IRegexCompilationService`, cap one test at 64 enabled links, use a 350 ms per-pattern timeout, and do not persist or log the caller's test text.
- The Database knowledge editor can create/update/unlink RegEx semantics and run a transient recognition test before relying on the link.
- The schema migration is additive and uses restrictive deletes so removing knowledge or a RegEx pattern cannot silently destroy a semantic relationship.

## Project/topic knowledge accessibility

- Exposes the existing `LocalGptProjectTopicKnowledgeLinks` capability directly in the Database knowledge editor.
- Adds project/topic selectors, an explicit link reason, existing-link presentation and confirmed unlink support.
- Adds `ILocalGptProjectService.GetKnowledgeLinksAsync` and `UnlinkKnowledgeAsync`; endpoint records are preserved when only the relationship is removed.
- Restores `CouncilKnowledgeEntry.ProjectTopicLinks` so the persisted relationship is navigable from the knowledge side as well as through the link table.

## EF navigation repair

The supplied SQLite database had no declared foreign-key violations. The main disconnect was therefore in the CLR navigation graph and UI reachability rather than broken persisted constraints. 3.2.9 restores reverse navigation for foreign keys that already existed:

- Council knowledge → human ratings;
- Council knowledge → project/topic links;
- Council knowledge ↔ RegEx pattern links;
- project revision → requirements;
- project revision → artifacts;
- project requirement → artifacts;
- project/revision → document imports;
- project → organic-skill assignments;
- project → embedded firmware plans;
- compiler installation → build verifications;
- chat conversation → Council game sessions.

The model snapshot and EF architecture guard are updated with entity-specific relationship checks. The previous guard assumed navigation names such as `Artifacts` could occur only once globally; that assumption became invalid once the legitimate reverse navigations were restored. The updated guard is stricter about *which entity owns each relationship* instead of rejecting valid duplicate navigation names.

## Render/reactivity audit

- Retains the 3.2.8 `/dx-functions` post-`OnAfterRenderAsync` completion render repair.
- Finds and repairs the same state-after-await pattern in `Drawer.razor`: after browser width detection changes the responsive drawer width, the component now explicitly requests the renderer-affine refresh that makes the selected width visible immediately.
- Reviews the other maintained `OnAfterRenderAsync` paths and does **not** add blanket `StateHasChanged()` calls where the async work only attaches JavaScript or already marshals visible state through an existing renderer callback. This avoids render loops and needless circuit traffic.
- Database state-changing operations now explicitly render the busy state and completion state, improving perceived responsiveness on slower database operations.

## Disposal and cancellation hardening

- Retains the user's `ThemeJsChangeDispatcher` `IJSObjectReference.IsDisposed()` guard before module disposal.
- Treats `JSDisconnectedException`, teardown cancellation and already-disposed module races as expected debug-level shutdown outcomes in that module path.
- Makes `ChatGameConsole.DisposeAsync` idempotent, unhooks service events once, tolerates expected disconnect/cancellation/already-disposed browser teardown and always releases its `DotNetObjectReference`.
- Hardens the two `CompositeChatClient` streaming enumerator cleanup paths for requested cancellation and already-disposed enumerators.
- Does **not** globally suppress asynchronous-disposal failures. Generic service diagnostics and unrelated `DisposeAsync` calls continue to surface unexpected cleanup faults, because hiding them would trade one noisy race for silent resource bugs.

## Supplied database findings

- SQLite `integrity_check`: **ok**.
- Declared foreign-key violations: **0**.
- 48 application tables were present in the supplied export.
- `CouncilKnowledgeEntries`: **381** rows.
- `RegexPatterns`: **118** rows.
- `LocalGptProjects`: **17** rows; project topics: **5** rows.
- `LocalGptProjectTopicKnowledgeLinks`: **0** rows, despite the available project/knowledge structure.
- 49 normalized topic/scope groups contain repeated knowledge records (225 rows total), dominated by learn-base architecture fingerprints. Those are not automatically deleted because repeated source-backed findings may carry different provenance or lifecycle state.
- One exact duplicate RegEx pattern/flags pair exists (`builtin.mod-id-cleaner` / `builtin.package-part-cleaner`). It is reported rather than automatically merged because the names may represent intentionally different semantic uses.
- Several `...Id` fields remain deliberately soft/correlation identifiers. 3.2.9 does not convert them into foreign keys without a dedicated migration and usage-invariant review.

See `DATABASE-RELATIONSHIP-ANALYSIS-v3.2.9.md` for the detailed conservative migration assessment.

## Historical v0.8 review

- The early stable source confirms that Chat, Database, Test Lab, Model Council/Minecraft-era workflows and LearnBase knowledge import were not simply removed; their current equivalents are broader and more structured.
- What regressed in places was **directness**: as persistence and Council services multiplied, some capabilities became reachable only through GUIDs, indirect Council actions or specialist pages.
- 3.2.9 therefore recovers early-version accessibility without discarding the richer 3.x architecture: semantic selectors, relationship editors and clearer workbench boundaries expose capabilities already owned by the current services/database.

## Version

- LocalGPT, InstallerConsole and WebView wrapper advance to **3.2.9**.
- DevExpress remains **25.2.9**.
- PublisherStudio remains **2.9.7** because its source is not changed in this release.
