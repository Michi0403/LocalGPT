# LocalGPT 4.7.2

## Council startup and provider-binding repair

- Fixes Council runs that could reject a model as unavailable even though the same provider/model was visibly selected for the run.
- Reconciles persisted role bindings against the current provider-qualified participant identity using the same safe endpoint/model normalization already used by provider discovery, including `localhost`/`127.0.0.1`, explicit `:latest`, and the same-runtime Ollama `/v1` compatibility facade without crossing hosts.
- Preserves legacy bare model bindings only when they resolve unambiguously; ambiguous same-name models still require an exact provider-qualified selection.
- Prevents an approved one-run model-health exclusion from silently removing a model that the selected team explicitly requires for a role or model-bound workflow step. The one-run decision is consumed, but the explicit team binding remains authoritative for that run.
- Applies the same safe identity reconciliation to `AssignedModelSingle` workflow steps and assigned role-result synthesizers.

## ASCII / Pixel game popup repair

- Restores DevExpress combo/dropdown interaction inside the open ASCII game popup by raising the body-level dropdown portal from its first visible interaction instead of waiting for the listbox subtree to materialize.
- Expands the game popup to a responsive 97vw x 94dvh workspace and removes the nested header scrollbar that was consuming useful arena height.
- Lets toolbar buttons shrink and reduces combo minimum width so display, fullscreen scale and control selectors remain usable at narrower desktop widths.
- Preserves the existing ASCII and Pixel display paths; no renderer mode was removed.

## Kernel Creature Tournament responsiveness and presentation

- Limits live trainer/creature battle inference to the two kernels in the current legal match instead of asking every tournament participant to emit `WAIT` on every exchange.
- Runs the active pair through the benchmark-bounded `AllMembersParallel` path, so calibrated host concurrency can actually use faster hardware instead of forcing one request at a time per host.
- Bounds recurring battle transcript context to opening identities plus the latest battle evidence; the deterministic engine scoreboard remains authoritative and the prompt no longer grows through the whole tournament.
- Raises the tournament arena from 144x40 to 168x44, expands renderer bounds, and gives roster labels more room so names, team state and statements are less likely to be clipped.
- Adds richer joint-based trainer and creature ASCII rigs while keeping deterministic state and hit-point ownership in the LocalGPT engine.
- Adds generated trainer `HUD` motifs and creature `EFFECT` motifs to the existing model turns, giving every exchange fresh visual language without adding extra inference calls.
- Adds trainer greeting and creature ready-stance phases at every new legal match, plus attack wind-up/burst/contact, finisher and champion-reveal animation phases with trainer/creature voice lines.
- Keeps Pixel mode on the same deterministic frame/state stream, so future top-view ASCII/Pixel games can reuse the tournament battle/session foundation rather than forking combat state.

## Compatibility and scope

- `@rendermode InteractiveServer` coverage is preserved; no existing interactive component directive was removed.
- PublisherStudio source is unchanged in this release.
- No GitHub/network repository access or .NET build tooling was used while preparing this source package.
