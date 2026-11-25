# LocalGPT 7.8.4 – Understated Uakari

Small-model (3–4B) friendliness of the Program Compiler path, without changing Windows behavior for larger models.

- compiler-intake prompt: states that LocalGPT already created project and revision; small models no longer call code-generation functions or ask a human for a project ID during intake.
- File-work `planPrompt`: adds a complete example `ReviewedSourceDraft` JSON for a new net10.0 console program.
- New runtime value `CouncilFileWorkRetiredPromptHashesJson` (SHA-256 of retired shipped prompts). At startup, `planPrompt`/`repairPrompt` in `CouncilFileWorkRuntimeParametersJson` are replaced only when the stored text matches a retired built-in text; user-edited prompts and all other fields stay.
- Compiler team seed version 56 → 57 so the intake change reaches untouched existing teams.

Checks performed: brace balance of edited C# file, JSON validity of seed documents, hashes compared with a 7.8.2 database. Not compiled here (no network for restore); build and Mac test pending.
