# LocalGPT 7.3.4

- 7.3.3 only covered single-model chat. In AI Council runs the DX call tags are read by CouncilDxFunctionOrchestrator from the visible answer only, so gemma4:26b's codegen.review.create written in thinking was still ignored (run 450f3693). The orchestrator now uses the last complete call tag from thinking when the visible answer has none; registry policy and approvals still apply.
- build/Build-Documentation.ps1: browser stdout/stderr are drained with a bounded wait (FUTURE2_DOCUMENTATION_BROWSER_DIAGNOSTIC_DRAIN_MS, default 15 s) instead of an unconditional .Result, so a detached Edge helper holding the pipe open no longer hangs the PDF render (MacBook Neo finding by Codex). PDF validation is unchanged.
