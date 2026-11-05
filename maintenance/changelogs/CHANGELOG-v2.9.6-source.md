# LocalGPT 2.9.6 source changes


- Native Ollama GPU placement no longer applies the legacy qwen/gwen/gemma family-wide `num_gpu=20` heuristic when no explicit road/run override exists. Low-B variants are therefore no longer accidentally forced into partial offload; explicit `OllamaNumGpu` remains authoritative and Ollama auto-placement handles the default.
- Benchmark Task Curators are explicitly forbidden from adding `UNABLE`, opt-out, delegation, capability-exemption, or ask-the-user escape clauses to the four bounded maintained tasks.
Source-only release. This repository was not compiled in the release-preparation environment.

## Large-Council benchmark stability

- Removed the duplicate 95-member social Benchmark Subject round from the maintained initial hardware calibration seed. Task Curators now prepare the authoritative four-task pack and the deterministic measurement service executes that pack directly.
- The four curator tasks are bundled into one provider turn per token profile. Four profile points therefore require at most four measurement calls per benchmark-capable target instead of sixteen.
- Physical/provider host queues run in parallel while targets on the same host stay sequential to reduce VRAM contention.
- Two consecutive failed profile points stop further escalation for that target. Failures remain explicit coverage evidence; LocalGPT does not invent a higher route.
- Benchmark progress identifies target/profile/task instead of presenting the same four task labels as an apparently endless cycle.
- Malformed or truncated model JSON is scored as benchmark evidence and no longer produces application Error logs during ordinary task-quality scoring.
- The deterministic calibration summary retains provider-qualified coverage plus bounded strong and weak independent-answer exemplars for later review roles.

## Role execution, tools and knowledge

- Configured workflow role task is explicitly authoritative over the original user request and prior Council evidence.
- Generic AI-identity/capability refusal on an otherwise executable assigned role receives one bounded corrective retry on the same model, role and workflow step. A repeated refusal is recorded as failed role work.
- Workflow steps can carry an exact automatic-function allow-list. `CanUseOrganicFunctions=false` now reaches the provider-native Ollama client, so disabled steps receive no automatic tool metadata.
- The benchmark seed advertises only bounded read-only capabilities appropriate to each role; deterministic measurement exposes no automatic tools.
- Provider streams now state whether automatic tools are disabled, unrestricted by the step, or restricted to an exact list, and distinguish passive supplied knowledge from visible active function calls.
- `localgpt.knowledge.list` accepts an optional topic/content/source/tag query so roles can retrieve relevant local knowledge rather than only listing unrelated recent entries.
- General Council first-pass work is explicitly blind independent work, followed by frozen-candidate review and synthesis, preserving strong minority/individual answers instead of contaminating every candidate with peer context.

## Timing and result preservation

- Council participant `DurationSeconds` now starts after host/lane lease acquisition. Large sequential-per-host runs no longer count queue waiting as model generation time, which makes small-model timing evidence materially more useful.
- Stopping or completing a Council no longer hides already-rendered participant lanes. Completed answers remain inspectable after the run stops.

## Human collaboration and approvals

- Registered DXFunction JSON schemas are validated before a human-approval request can be queued, including UUID/type/required/enum/length/range/array constraints. A malformed `codegen.review.execute` review identifier is rejected as `InvalidParameters` before approval.
- Human Collaboration UI labels approved deferred actions as execution results and distinguishes failed execution from successful execution.
- Human guidance briefings carry requester, requested role, target members, scope and gate information and explicitly instruct the matching later role/member to consume the answer as high-priority role input. The existing heartbeat/boundary continuation model is retained.

## Configured-host hardware

- Added database-backed hardware profiles owned by the configured physical host rather than by one model or by the LocalGPT process globally.
- `/install` host cards can save CPU/system RAM/GPU/VRAM, detect local hardware, paste/import HWiNFO text reports, and retain provenance/confidence.
- User-confirmed/imported hardware is not silently overwritten by weaker automatic detection.
- HWiNFO text import deterministically parses GPU memory, GPU identity, CPU, host, OS and common system-memory forms, including `Total Memory Size [MB]`.
- Linux discovery uses DRM/sysfs and reads AMDGPU `mem_info_vram_total` when available. NVIDIA discovery continues to use `nvidia-smi` on supported Windows/Linux hosts.
- Windows `Win32_VideoController.AdapterRAM` is no longer used as authoritative VRAM; the Windows CIM fallback supplies GPU identity only.
- Configured-host hardware is exposed to LocalGPT time/state evidence and used by adaptive Ollama benchmark wiring for local host capacity decisions.

## Compatibility

- LocalGPT application / installer / WebView wrapper version: **2.9.6**.
- LocalGPT Wire Protocol remains **2.1.1**.
- Existing configurable all-members readiness preflight remains available; the supplied initial benchmark keeps it disabled.
