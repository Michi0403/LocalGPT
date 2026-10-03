# LocalGPT 5.2.8

LocalGPT 5.2.8 repairs the repository-local build-storage preflight introduced in 5.2.7. Fresh clones require no cache-path configuration: heavy build state defaults to `artifacts/.build-storage`, ignored by Git. Optional environment/parameter values only override the default.

The PowerShell compatibility guard now treats source-code variable names literally when validating documentation browser-profile placement, avoiding the corresponding StrictMode failure. Application runtime behavior is unchanged.
