# LocalGPT 6.1.9 — semantic planning JSON transport repair

Baseline: 6.1.8 complete source ZIP, no GitHub or .NET build.

- `GoalExecutionService.SemanticPlanning.cs` now decodes HTML-encoded JSON only when the model provider supplies evidence of entity encoding (`&quot;`, `&#34;`, or `&amp;quot;`). This repairs the observed `JsonException: '&' is an invalid start of a property name` while leaving ordinary literal ampersands in valid JSON alone and preserving the existing schema validation and planner fallback.
- The 1-Wire text-proposal DXFunction description clarifies that a proposal remains approval-gated, and PublisherStudio may apply it only against a matching, explicitly user-initiated local insertion intent. All transport permissions and function flags remain unchanged.
- `GoalExecutionService.cs`, `.DurableExecution.cs`, and `.SemanticPlanning.cs` now resolve `publisher.*` requirements from *connected, enabled, online 1-Wire peer capability descriptors*, not solely from LocalGPT organic skill keys. This repairs the observed `Goal execution blocked; missing runtime requirements: publisher.business-context, publisher.text.insert.propose, publisher.spreadsheet.inspect` when the peer advertises those capabilities. A missing/disconnected peer remains a blocker, and the existing per-peer approval/execution policy is still enforced at the tool boundary.
- Version 6.1.8 → 6.1.9. All repository architecture gates left unchanged.

Verification: source review and targeted string/contract checks only. No .NET compilation, PowerShell gate execution, or live model-provider invocation was performed.
