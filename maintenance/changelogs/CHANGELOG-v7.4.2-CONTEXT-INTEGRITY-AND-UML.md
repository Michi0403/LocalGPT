# LocalGPT 7.4.2 — context integrity and documentation UML

Source candidate; release and runtime verification pending.

- Native Ollama request sizing respects the selected hardware-road ceiling and model-advertised limit, including transport compatibility retries; the previous automatic increase of goal context and load to 100% was removed.
- Preserve structured historical function calls/results and image payload clones; measure serialized messages and native schema text. Capture Ollama-reported token counts as diagnostics when present.
- Retain conversation turns and strict tool scope instead of silently removing old turns; reduce only completed assistant reasoning and eligible unscoped tool directory through the existing broker before returning a preflight failure.
- GoalExecution retains the durable plan and observed receipts, refuses automatic replays after prior tool activity, and asks the configured semantic planner for smaller remaining work without escalating resource settings. Reject unchanged singular context replans and silent step truncation.
- Extend the existing PowerShell Council wiring guard with context-integrity source contracts.
- Resolve the generated DocFX Mermaid module by current artifact inventory rather than a hardcoded chunk hash; validate declared local module targets in the existing Pages artifact gate.
- No schema change, Qwen migration adoption, DI replacement, saved team overwrite or training operation. See `TASKS.md` for pending full compaction/checkpoints, Task Steward/Nurse, specialist subround/host routing, training and evaluation.

## Verification

JavaScript syntax checked for the changed documentation theme. ZIP integrity checked at packaging. PowerShell, Roslyn/compile, DocFX/browser/PDF, Ollama and database runtime gates are not run in the editing environment. No compiler or online repository access attempted. The full repository validation and owner-side smoke tests remain mandatory before deployment; no validation stamp is issued.
