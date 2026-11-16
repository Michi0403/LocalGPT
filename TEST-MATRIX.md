# Acceptance test matrix (must run on the real host)

1. Windows 64GB RAM / 24GB VRAM: Select installed gemma4:26b, Program Compiler team, original HelloMath request. Confirm prompt reaches model, real codegen.review.create, approval, execute, .NET 10 build, artifact.verify and downloadable ZIP; only receipts count as verified.
2. Small Mac (8GB RAM, 1.5B coding model): Run a small C# generation from the same default team selection. Confirm system recommends smaller work packets, actual model tokens appear, DX tools stay callable, and no OOM/empty loop. Do not expect 128K context.
3. Force context overflow: Confirm preflight returns to GoalExecution in one phase, GoalExecution AI + Configuration Steward statuses are visible in Chat. If model AI returns validated JSON, show Configuration Steward AI · Subagent; otherwise show deterministic Steward only. Confirm a NEW compact Council request differs from the rejected one.
4. Manual mode: Set GoalExecutionResourceMode=Manual and a deliberately small context. Verify the steward doesn't alter context, load, explicit model routes, or user-approved presets. Goal recovery may still split the task.
5. Multi-step source read: project workspace and verified file reads must be retained as evidence/references even when prior chat payload is compacted; DX tool restrictions stay intact.
6. 1-Wire PublisherStudio / Excel: Confirm peer discovery, deferred approval, protected write/execute, verified artifact and download still function. No new 1-Wire handlers introduced.
7. Chat disconnect/rejoin: Refresh or reconnect after a failed automatic attach, rejoin the SAME run ID, and verify no duplicate Council starts and persisted transcript returns. Separate full observer-detach race still needs testing.
8. Defaults and learning: exercise an unmodified default team and knowledge-backed small coding subagent; ensure no forced hardcoded model family or lost tool schema.
9. Architecture: verify AssertLocalGptApplicationStaticPolicy target executes; compiled Debug+Release; no new static service helpers or DI cycles.

All above are required runtime tests, not claimed PASS results.
