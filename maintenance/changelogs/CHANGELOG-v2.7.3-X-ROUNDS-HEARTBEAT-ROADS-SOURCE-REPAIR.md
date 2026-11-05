# LocalGPT 2.7.3 changelog

## X-Rounds and X-Functions

- Added first-class revisable Council control flow managed per workflow step in **Council Teams** rather than as a hard-coded gatekeeper team type.
- Added five DI-backed Council functions: `council.x.status`, `council.x.revisit`, `council.x.return_text`, `council.x.start_single_model` and `council.x.start_council`.
- Revisited steps are stored as new workflow revisions with causal metadata; earlier outputs remain immutable instead of being overwritten.
- `reconsider` revisits a current/earlier step with DX/organic side effects suppressed, while `reexecute` deliberately uses that step's normal function policy again.
- X-Round revisits cannot jump forward across configured workflow gates. Normal workflow progression remains responsible for forward movement.
- Added configurable per-step transition budgets, child-Council nesting limits and optional explicit human approval for accepted X control requests.
- Added derived single-model subtasks and derived child-Council runs that feed returned text back into the parent workflow without merging run identities.
- Added explicit text-return control so an authorized workflow role can complete the parent Council with a bounded text result.
- Added Council Teams convenience presets for **Gatekeeper**, **Reactive revisit**, **Derived single model**, **Derived Council**, and clearing X policy; every underlying switch remains directly editable.

## Live Council heartbeat behavior

- Fixed one live user/heartbeat contribution causing multiple already-running Council members to restart independently.
- A direct live message may now be claimed for immediate restart by only the ordered foreground participant for that Council run.
- The same contribution remains available to the normal Council heartbeat path so later participants/rounds receive the correction as shared context without duplicate immediate restarts.
- Participant live lanes continue to pre-register every provider-qualified member across local and remote AI hosts so hidden parallel work is visible before ordered transcript integration.

## Fine-grained scheduling and long-running models

- `/Chat` now exposes **Parallel models per AI host** alongside the existing load-balancing selector and hardware-load control.
- Running Councils keep that per-host ceiling in the mutable run snapshot, so not-yet-started phase work uses the newest setting without changing other runs.
- Added **Model response timeout (seconds)** to the same Chat Council controls. It updates the active run for provider calls that have not started yet and prevents the timeout from being an invisible fixed 30-minute behavior.
- Existing advanced **Hardware spooler and per-model CPU/GPU roads** remains available for hardware kind/device, token road, Ollama `num_gpu`, lane concurrency and per-model load overrides.
- Model presets no longer silently clamp saved `MaxParallelModels` to eight; the runtime still applies its normal participant/road safety boundaries.

## Documentation and project links

- Added the visible GitHub Pages URLs for LocalGPT and PublisherStudio near the top of the repository README so mobile GitHub users can see/copy them directly.
- Expanded the Council guide with X-Round semantics, single-consumer heartbeat behavior, live-host scheduling controls and the user-editable model timeout.
- Added a source audit covering X-Round DI wiring, revisable-history rules, human/loop/depth gates, heartbeat claims, remote live-lane registration and fine-grained run settings.

## Version

- LocalGPT application/wrapper/installer: **2.7.3**.
- `LocalGPT.WireProtocolVersion` remains **2.1.1** because X-Rounds are local Council orchestration and require no new 1-Wire message contract.
