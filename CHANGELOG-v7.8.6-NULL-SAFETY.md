# LocalGPT 7.8.6 – Unruffled Uakari

- Council function gateway: a step without visible text no longer throws while stripping function-call markup, which could replace the results of functions that already ran with "Function gateway unavailable".
- Ollama tool calls without a name: tool message, trace and result use an empty name instead of null; the "not registered" answer now lists the functions offered in this request so small models can correct their next call.
- CouncilRoleAssignmentsController: XML summary placed above the attributes (CS1587), so the controller appears in the documentation.

- Chat transcript: the GoalExecution "subagent" and "coordinated source route" notices and the unusable build-repair notice are now also written into the chat, not only into the overwritten status line.

Checks performed: guard token contracts of the edited files compared; build pending.
