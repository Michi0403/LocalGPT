# LocalGPT v0.1.4 — Organic Learning Hotfix 3

## Build and bootstrap repairs

- Fixed the C# interpolated raw-string compilation error in `CouncilTextService` by escaping the JSON braces in the `<localgpt-self-assessment>` example.
- Kept the installer/WebView bootstrap contract intact: default application port `5000`, positional installer port argument, public read-only `Program.Port`, and `Program.BaseUrl`.
- Explicitly qualifies every atomic port access as `System.Threading.Volatile` to avoid the `DevExpress.CodeParser.Volatile` ambiguity.
- Preserves the existing one-wire service and discovery ports as optional wiring that cannot replace or break the application/installer port.

## Runtime reliability

- Serializes chat-memory saves through a scoped `SemaphoreSlim` and replaces message snapshots transactionally with explicit foreign keys. This avoids EF conceptual-null failures during overlapping autosave/manual-save activity.
- Theme-switcher callbacks return to the Blazor dispatcher before changing component state.
- Keeps migration compatibility backup/adoption behavior and losslessly evolves only exact old built-in defaults; user-edited database values remain authoritative.

## Offline local limits and media uploads

- Removes the active chat upload MIME filter so any browser-selectable file/media format can be attached as local evidence.
- Uses the largest supported `int` values for DX chat upload count, per-file size, prompt length, and visible prompt length.
- Removes Kestrel request-body and SignalR receive-message ceilings and raises ASP.NET multipart/form limits to framework maximums for the loopback-only offline host.
- Unknown media remains binary evidence; model/provider support still determines whether its content can be interpreted.

## Council teams, live participation and Learning Round

- Makes SQLite-backed Council teams selectable directly in Chat and links to the existing editable team/workflow page.
- Adds a refreshed selector for all currently running Council sessions so the human can join a specific heartbeat without stopping the active model.
- Adds one-click enablement of the local Human Council Participant profile and queues general corrections/information into the next heartbeat.
- Adds the seeded, editable `learning-round` team with history, regex/architecture, evidence-verification and learning-leader roles.
- Adds `localgpt.learning.snapshot` and `localgpt.learning.maintain` DI-backed DX functions.
- Learning maintenance stores facts as `ModelSuggested` / `NeedsUserReview`; it never turns model output into approved authority.

## Database-backed RegEx and function wiring

- Seeds all legacy generated regex definitions supplied for LocalGPT plus generic project-reference, namespace, DI registration, ASP.NET route, solution-project, installer-port, one-wire capability and file-path patterns.
- Adds `localgpt.regex.list`, `localgpt.regex.get`, `localgpt.regex.test` and `localgpt.regex.upsert` through the DI/DX-function architecture.
- Regex updates are knowledge self-maintenance, compile with a timeout before persistence, and cannot authorize commands or project writes.
- Advertises the learning and regex functions through the same one-wire capability/skill/UI discovery path used for external organic systems.

## PublisherStudio collaboration

- Adds `publisher.text.proposal.request`, a LocalGPT-side DX wrapper that sends text to a uniquely connected PublisherStudio as a reviewable `publisher.text.insert.propose` request.
- The request is sequentially spooled, requires LocalGPT confirmation, remains subject to PublisherStudio permission rules, and never inserts text automatically.
- Ordinary Chat can request `publisher.spreadsheet.inspect`; a Council run is not required for read-only spreadsheet evidence.

## Validation added

- Expanded the LocalGPT one-wire/bootstrap source-contract test for explicit `System.Threading.Volatile`, unrestricted chat uploads, Learning Round, regex seeding, live Council joining and Publisher text proposals.
- Added a project-reference/source-closure contract that verifies all referenced projects and critical compilation sources exist.
- See `MISSING_FEATURES-v0.1.4-organic-learning-hotfix-3.md` for deliberately unfinished work and validation limits.
