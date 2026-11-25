# LocalGPT 7.8.4 – Understated Uakari

Restores Windows-proven 7.8.2 behavior for compiler file work; keeps only non-intrusive 7.8.3 fixes.

- Reverted: `repairPrompt` seed text is again exactly the 7.8.2 text (including restore problems as environment issues).
- Reverted: build-repair input again contains `CompletedFiles` (paths with hashes) as in 7.8.2.
- Kept from 7.8.3: provider host test uses `aiDiscoveryTimeoutSeconds`; clearer `CompilerNotSelected` message; an unparsable repair answer ends repairs with a warning instead of aborting file work (only reached where 7.8.2 already failed).
- The withdrawn 7.8.4 prompt/seed changes (commit 80ae400a) stay reverted (47a8e9d3).

Checks performed: `git diff 81778156 -- src` shows only the three kept changes and the version. Not compiled here (no network for restore).
