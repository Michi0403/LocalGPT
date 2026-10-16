# LocalGPT 5.8.2 — static validation / Windows test matrix

**Environment:** No dotnet, GitHub, PowerShell, Ollama, DevExpress UI, or real project build executed here.

1. Launch Game Project Development with a newly uploaded ZIP and no selected Project: a new `Game` project with a stable UUID and current revision should be created **once**, before model calls; the IDs should be in bootstrap and usable by `project.game.*`.
2. Launch with an existing valid Game project: do not create a second project. Launch with invalid/archived/non-Game selection: give the explicit preflight failure; preserve the original project.
3. Upload corrupted/incomplete ZIP: curator should block before development. Upload valid ZIP: curator should read all archived entries, hash them, and preserve coverage report. This is archive-integrity evidence, not per-model comprehension.
4. Have the baseline architect enumerate the upload but not read any actual file: expect one bounded retry and then an explicit failure, not fabricated Tremulous rules. Have it call `chat.upload_workspace_file` successfully via native Ollama or recovered DX gateway: expect the next workflow step.
5. Run with failing qwen3.6 or absent model: verify participant error and no fake build/playtest proof. A run without actual successful `project.game.build` and `project.game.start` results must show `Game development incomplete` and preserve resumable state.
6. Run with approved saves, successful build and runtime launch: confirm function-result receipt names, accurate Game project type, applied requirements and working human/AI control, and review the exported build ZIP. This test is not yet performed.
7. Recheck General Council, Learning Round and existing user-owned Council templates for regressions. Ensure system seed 44 preserves user-modified copies.

**Limits:** Requiring a successful source-file read does not guarantee every file or class was understood. Complete, self-contained Tremulous recreation requires further source coverage, code generation and acceptance tests. Model-timeout and user-approval limits remain intentional.
