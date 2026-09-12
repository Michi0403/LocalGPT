# LocalGPT 4.2.0

LocalGPT 4.2.0 completes the provider-model discovery and setup-workbench repair requested after the 4.1.8/4.1.9 runtime screenshots.

The Ollama runtime settings no longer rely on DevExpress caption-column compression for seven server controls. They use a stable responsive field grid, and checkbox captions are rendered as explicit visible HTML beside the control so the local-only and destructive-delete acknowledgements cannot degrade into unlabeled squares.

The explicit Ollama catalog search is now provider-live rather than alias-limited: official and community search-result families are parsed from Ollama-owned pages, each family is expanded through its full tags page with bounded concurrency, and concrete provider pull identifiers are retained under a fixed-origin/safe-path policy. Maintained offline aliases remain an additive fallback and are merged into older persisted profiles so upgraded databases immediately regain current built-in families such as Gemma 4.

CanIRun.ai hardware comparison now has its own compatibility-catalog limit. A historical small `CanIRunMaximumRecommendations` value can no longer collapse the full compatibility expansion back to a handful of rows. Exact live-provider model matches are annotated with attributed CanIRun grade, score, status, quantization and memory evidence when available.

The 4.1.9 macOS packaging process-spawn repair, 4.1.8 storage/model work, and 4.1.7 runtime churn/cancellation protections remain intact.

See `CHANGELOG-v4.2.0-OLLAMA-LIVE-CATALOG-WORKBENCH.md` and `VALIDATION-v4.2.0-source.md`.
