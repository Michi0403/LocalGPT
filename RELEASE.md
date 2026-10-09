# LocalGPT 5.4.1

This corrective release repairs the source compiler error in the 5.4.0 `ResilientStep` wrapper for seeded compiler/release Council workflows. `Step` gained two optional Boolean arguments for complete upload-workspace curation and deferred approval handling; the wrapper still passed following arguments positionally. The helper now passes every optional argument by **name**, preserving the wrapper's original flags and allowing the new gates to use their own defaults.

The 5.4.0 per-ZIP curator, research approval, seed role sampling, and file logging are unchanged. See `CHANGELOG-v5.4.1-RESILIENT-STEP-ARGUMENT-REPAIR.md` and `VALIDATION-v5.4.1-source.md`. The owner must compile/runtime-test locally; no .NET compilation was run for this package.
