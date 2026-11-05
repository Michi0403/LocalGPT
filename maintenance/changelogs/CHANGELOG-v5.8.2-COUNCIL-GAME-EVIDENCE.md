# LocalGPT 5.8.2 — Game Council project/source evidence recovery

- Fix the `Game Project Development Team` failure observed in Council `9c4de58d-9e79-4a5b-ada4-a45e6023806d`: the run had no project UUID and model calls to nonexistent `project.list`/`project.get` returned NotFound; later roles repeated that block and finished without a built game.
- When Game Project Development is started with no selected project, ensure an idempotent Council-owned **Game**-typed Project and revision before the first model call; preserve generic Council project defaults elsewhere. A selected missing/archived/non-Game project now fails preflight explicitly rather than silently dropping project context. Keep the newly created project/revision identifiers authoritative in the run and role prompts.
- Keep `localgpt.projects.list` as the canonical registered project listing function; update the Game team function inventory and source-reader instructions to use real registered function names.
- Enforce deterministic original/extracted upload curation for Game Project Development, including old user-edited step configurations. This reads and SHA-256 checks archive contents but **does not** prove individual models understood or read every source file.
- Demand a successful `chat.upload_workspace_file` result after the baseline role when upload evidence was curated. Retry the read-only role once before refusing to implement source-derived rules without any file-reading evidence. Preserve the uploaded workspace and earlier Council steps on failure.
- Check both registered DX gateway result steps and provider-native function-result traces. Do not rely on a model's ungrounded completion prose.
- At the end of Game development, prominently mark the result **INCOMPLETE** if successful source-read (when applicable), `project.game.build`, or `project.game.start` evidence is missing. The existing one-use human approval boundary still applies to project mutation/build operations.
- Increase maintained Council seed version 43 -> 44 so updated system templates reach existing installs; user-modified system seed definitions are preserved as custom copies by existing seed evolution logic.
- No changes to PublisherStudio, Python requirement, other Council workflows, PDFsharp, or release packaging.

## Evidence comparison

- Failing Oct 15 Game Development Council: eight selected models, timeout/recovery failure for qwen3.6:27b, no assigned ProjectId, rejected calls to `project.list`/`project.get`, and only a workspace file-list call visible rather than a successful source-file-read receipt. The final answer requested a UUID instead of delivering a build.
- Earlier Oct 10 Learning Round: `LocalGPT: workspace curator gate` reported completed full-read coverage of archive entries (the program could read and hash the archive bytes). That does not prove every model independently read every file.
- The new gate guarantees validated byte-read coverage plus at least one observed function read before source-derived design proceeds. It cannot guarantee perfect semantic learning, full implementation of a large game, successful provider responses, or unapproved mutations.
