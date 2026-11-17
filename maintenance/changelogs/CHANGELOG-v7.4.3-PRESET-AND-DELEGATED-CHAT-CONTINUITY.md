# LocalGPT 7.4.3 — preset and delegated-chat continuity

Source candidate; compilation and PowerShell gates pending on the maintainer's environment. No online GitHub or .NET tooling used.

## Changes

- Keep the originating Chat run identity independent of the durable GoalExecution subrun ID. Forward existing X-Function child Council callbacks, correlate participant activity by execution run, and restore each retained subrun's transcript in the originating Chat. Retain separate run identities for execution, approvals, receipts, logs and artifact verification.
- Reuse the existing host-aware phase scheduler; do not launch unrelated parallel child-Council trees or bypass existing lane limits, cancellation or approval boundaries.
- Use seed-backed fallback roads (4,096 output / 32,768 context). Default adaptive/learning presets target at most 16,384 context; other maintained presets at most 32,768, retaining smaller specialized limits. One request per host by default; independent hosts retain parallel execution. Explicitly customized settings remain authoritative. The seed updates existing Council fallback values if exactly 262,144, and stock preset rows only if their substantive fields exactly match the previous shipped templates. Preserve preset IDs and the selected default.
- Apply bare Ollama preset templates to matching installed provider instances; copy the original road limits and independent capability lists. Keep explicit provider bindings, including unresolved ones; if no generic template can run, retain the selections for readiness reporting. Missing generic pool entries are not installation requirements when other entries match.
- Strengthen existing PowerShell Council X-Round source contracts for the updated integration.

## Build report from 7.4.2 installation

The reported static policy and compiler failures point to abandoned Qwen source files, absent from the supplied clean 7.4.2 archive before this edit. Replacing files in-place without removing obsolete files is not equivalent to extracting a complete source snapshot. See `maintenance/CONTEXT-DRAFT-CLEANUP.md`. No checkpoint table migration, destructive repair, or architecture exemption has been introduced.

## Validation still required

Run all existing repository PowerShell gates and Debug/Release builds. Apply a stock preset on a fresh and upgraded database, and verify a customized preset remains unchanged. Run the same model tag on two configured hosts, check independent bounded roads and provider identity, then request a delegated Council step and a multi-subgoal GoalExecution. Verify actual worker output, tool receipts, cancellation and approval boundaries, preserved parent chat history and restart/rejoin behavior. The spool retains its existing bounded retention; full persistent context compaction, capability learning, and specialist selection remain pending in TASKS.md.
