# LocalGPT 4.2.0 — Ollama live catalog and workbench completion

## Runtime workbench

- Replaced the compressed Ollama `DxFormLayout` caption/editor pairs with a two-column responsive field grid that collapses to one column below 900 px.
- Gave every server editor a full-width bounded cell so long labels and spin controls cannot escape the form border.
- Replaced DevExpress `Text=`-only checkbox captions with explicit visible labels for Ollama local-only mode, permanent model deletion confirmation, and LM Studio CORS.
- Kept permanent deletion behind the existing explicit acknowledgement; no destructive behavior was weakened.

## Ollama model discovery

- The explicit provider search now recognizes official `/library/<family>` and community `owner/model` Ollama results.
- Ollama search families are expanded through their provider-owned `/tags` pages instead of only the family landing page.
- Up to 40 search-result families are expanded with at most six concurrent provider-owned requests, and up to 2,048 concrete provider model identifiers are retained.
- Fixed-origin URI construction and segment validation reject alternate hosts, traversal syntax, whitespace and shell/path separators outside the supported Ollama namespace/model/tag grammar.
- Namespaced community model URLs and install IDs remain intact instead of being discarded by the old `/library/`-only parser.
- Current maintained Ollama aliases are additively merged into persisted profiles. User aliases keep precedence, while older databases no longer hide newer built-in families such as Gemma 4.
- Provider catalog rows are sorted lighter-first when a parameter size can be inferred.

## Hardware-fit evidence

- Live provider search results are enriched with exact CanIRun.ai provider mappings already approved by the user.
- The provider-result surface now shows attributed grade, score, status, quantization, memory requirement and CanIRun source link when that evidence exists.
- Added `CanIRunMaximumCatalogCompatibilityRows` with a default of 1,024 and a hard in-service ceiling of 2,048.
- The compact recommendation-result limit and full compatibility-catalog limit are independent, preventing an old five-result policy value from truncating catalog comparison after upgrade.
- Provider availability remains independent from CanIRun.ai evidence; an Ollama result with no CanIRun match is still shown without inventing hardware suitability.

## Preserved behavior

- Provider web lookup remains explicit user action and fixed to maintained provider HTTPS origins.
- CanIRun.ai still requires its separate explicit hardware-data opt-in.
- LocalGPT does not move Ollama model blobs implicitly.
- 4.1.9 batched macOS native bundle inspection/signing inventory reuse is preserved.
- 4.1.7 database-init churn, cancellation and Ollama stream-stop protections are preserved.
