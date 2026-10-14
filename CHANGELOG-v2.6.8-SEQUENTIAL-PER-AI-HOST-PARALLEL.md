# LocalGPT 2.6.8 — Sequential per AI Host, Hosts Parallel

## Council execution strategy

- Added `AllMembersSequentialOnEachAIHostParallel`.
- The mode creates one deterministic execution queue per provider-qualified AI host/PC.
- Exactly one Council member runs at a time inside each host queue, while different AI hosts run concurrently.
- The logical Council phase barrier is unchanged: the next workflow phase does not start until every assigned member across every host has completed, failed, or been explicitly skipped.
- `AllMembersSequential` remains available as the strict global single-member chain.
- `AllMembersParallel` remains available for users who deliberately want multiple concurrent model requests on each AI host.

## Default behavior

- Newly created custom workflow steps now default to `AllMembersSequentialOnEachAIHostParallel`.
- Untouched source-controlled workflows that previously used `AllMembersSequential` now use the new host-parallel sequential strategy.
- Council team seed version advanced to 18 so untouched system teams receive the safer multi-host default. User-modified team definitions remain untouched.
- The General Council peer-review step now uses the new per-host sequential strategy by default.

## Host scheduling

- AI hosts are now independent runtime scheduling boundaries even when additional hardware-road parallelism is disabled.
- `AllowParallelHardwareRoads=false` means one active model road per AI host, not one active model across the entire Council.
- `AllowParallelHardwareRoads=true` may additionally use configured CPU/GPU roads inside each host.
- Provider-qualified routes on the same host share that host boundary, so native Ollama and OpenAI-compatible surfaces on one PC do not masquerade as separate physical machines.

## Determinism and streaming

- The new strategy starts one worker per AI host and preserves deterministic member order inside each host queue.
- Concurrent host output is still buffered/presented as intact member streams so thinking, tool calls, and visible answer text are not interleaved between models.
- Host-parallel members receive the transcript that existed when the phase began. Use strict `AllMembersSequential` when each member must consume the immediately preceding member's new output.

## UI and documentation

- The Council Team editor exposes the new execution mode.
- Added EN/DE localization for the new mode label.
- Updated Council runtime and Chat/Council documentation with the three multi-member execution semantics.

## Version

- LocalGPT application: 2.6.8
- Installer console: 2.6.8
- WebView wrapper: 2.6.8
- 1-Wire protocol version unchanged.
