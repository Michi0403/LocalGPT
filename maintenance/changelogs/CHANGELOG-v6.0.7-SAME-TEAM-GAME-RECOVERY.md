# LocalGPT 6.0.7 — same-team source review and game completion recovery

Source-only release based on the complete LocalGPT 6.0.6 repository archive.

## Corrected / implemented

- Target the observed `game-project-development` Council failure: successful archive hashing and file listing did not create any `chat.upload_workspace_file` read receipt, so source review failed repeatedly before game authoring began.
- On a source-review gate failure, select actual text-like source files from the user's already-curated upload workspace and read bounded file excerpts. Supply real content and exact paths to the *same team* before a maximum of three self-repair rounds. No file is executed by this step, and file inventory alone still cannot pass the semantic review gate.
- Accept a subsequent verified workspace-file function receipt or a model step that cites an actually read, injected source path; otherwise leave the gate failed/resumable. Do not claim source inspection from hashes or metadata alone.
- For the Game Project Development team, check the configured `project.game.build` and `project.game.start` completion receipts after the normal workflow. Where an enabled authorized team step permits the function, repeat that same step up to two times to recover and verify, with the existing function permission and human-approval boundaries unchanged. Never launch a playtest without a verified build. Re-run the configured final handoff after such recovery.
- Increase the LocalGPT application project version to 6.0.7.

## Limitations and verification

- This is **bounded recovery**, not a guarantee that arbitrary source can be ported into the engine: successful game implementation still depends on the input sources, model/tool authorizations, project-game functions, and actual build/playtest receipts.
- If the configured team lacks an authorized game build/playtest tool, the run warns instead of claiming completion. Launching unrelated teams or polling the user for extra sessions is **not implemented** in this release; the existing same-team path was prioritized for an isolated repair.
- Static source/API inspection performed; no .NET build, native Windows execution, model inference, or Council run was performed here. The user's attached Council transcript served as the failure reproduction evidence, not as runtime proof of the repair.
