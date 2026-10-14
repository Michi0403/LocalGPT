# LocalGPT 2.9.5 — Configurable team all-members preflight

- Council Teams now exposes an explicit team-level all-members readiness preflight policy instead of hard-coding one behavior across social structures.
- `Legacy compatibility` preserves prior behavior: maintained built-in orchestration keeps its historical readiness phase, while literal custom workflows do not gain a new preflight implicitly.
- `Disabled` skips all-member model readiness turns completely.
- `Role-aware probe` gives every selected provider-qualified member one compact preflight against the roles and responsibilities actually assigned to it for the run; it does not ask members to solve the original user request or perform substantive role work.
- Explicit preflight probes have a small configurable per-member output-token budget and an optional prompt override with model/team/assigned-role placeholders.
- Preflight member output is visible and persisted as run evidence, but is excluded from later workflow model context by default. Teams can explicitly opt in to carrying that potentially large transcript forward.
- The supplied Initial Hardware Calibration Benchmark explicitly disables the all-members preflight so large benchmark Councils proceed directly from inventory and Task Curator work into assigned Benchmark Subject tasks.
- Existing benchmark role-task authority, four-task execution, deterministic four-point calibration and seed-preservation behavior remain intact.
- Council seed version advances to 23 so the maintained benchmark default receives the explicit disabled-preflight setting while user-owned copies remain preserved.
- Wire protocol remains 2.1.1.
