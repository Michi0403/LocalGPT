# Capability map

| Area | Maintained capability | Important boundary |
|---|---|---|
| Chat | Provider-qualified single-model sessions | Protocol/formatter isolation |
| AI Council | Teams, roles, runtime classes, workflows, human participation | Deterministic step state and bounded recovery |
| Providers | Ollama, OpenAI-compatible, OpenAI, Azure OpenAI routes | Endpoint-qualified identity and scoped credentials |
| Benchmarking | Bounded tasks, peer/self review, presets | Recommendations require user application |
| Projects | Versions, revisions, requirements, artifacts, topics | Database identity is separate from paths |
| Workspaces | Root resolution, policies, toolchains, build evidence | Fresh assessment before execution |
| Knowledge | Reviewed persistent entries and source refresh | Raw uploads/model output are not trusted automatically |
| DX functions | Typed callable application operations | Handler/service owns policy and execution |
| 1-Wire | Peer discovery, identity, capabilities, work spooler | Discovery is not trust; remote flags are not approval |
| Embedded | Board/pin/transport plans and artifacts | Plan, compile, flash, and actuate are separate |
| GameDirector | Deterministic sessions with generative proposals | Models do not directly mutate state |
| Minecraft | Reviewable mod/datapack generation | Toolchain evidence required for build claims |
| Documentation | Conceptual docs, XML API, PDF, Pages | Publishes shipped static tree |

## Capability-gap contract

When LocalGPT cannot complete a requested task, it should report:

- the requested outcome;
- the missing dependency, source, API, framework knowledge, permission, or runtime capability;
- what evidence was inspected;
- what can still be produced safely;
- the next reviewable step;
- whether owner-side tooling, licensed dependencies, or human approval is required.

A gap report is not permission to self-expand or execute arbitrary installers.
