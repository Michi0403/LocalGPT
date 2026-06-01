# AI Council Whole-Code Review - 20260601-224350

Generated through LocalGPT's own `POST /__diag/council` endpoint with a compact whole-repository source map and targeted excerpts.

## Run Metadata

- Run id: `e2c76164-2f69-47ad-8466-cbd47155c151`
- Models requested: `gpt-oss:20b`, `deepseek-r1:8b`
- Useful visible reviewer: `gpt-oss:20b`
- DeepSeek status: returned thinking-only output with no final visible answer
- Max rounds: `0`
- Max output tokens: `1536`
- Max context tokens: `8192`
- Parallel models: `1`
- Ollama keep-alive: `0s`
- Memory conversation id: `1a2f1fb3-a2d0-4fcc-bacd-286a485d9f0d`
- Knowledge entry id: `c6181871-aed7-42fc-9131-a0d27b6316d0`
- Raw council log: `C:\Users\micha\AppData\Local\LocalGPT\CouncilLogs\council-20260601-224350-e2c761642f6947ad8466cbd47155c151.md`

## Supervisor Fact Check

The council was shown the repository map and targeted excerpts, not every source line verbatim. The prompt explicitly told it not to overclaim. It still produced several inaccurate findings. Treat this report as feedback to triage, not as authoritative truth.

Confirmed false or misleading claims:

- `net10.0` is not automatically invalid in this workspace. `dotnet build LocalGPTWebviewWrapper\LocalGPT\LocalGPT.csproj -c Debug -p:Platform=x64 -p:UseSharedCompilation=false` and the wrapper build both succeeded on the installed .NET SDK/runtime.
- `GeneratedRegex` does not become stale merely because a regex source generator is used. That claim needs a concrete failing pattern or generated-code issue before action.
- `OllamaKeepAlive = "0s"` is intentional for council runs when several models are selected. It unloads one participant before the next one loads to reduce VRAM pressure.
- WebView2 smoke flags are diagnostic triggers, not expected in normal user runs. The real issue is that recent smoke runs loaded `gpt-oss:20b` correctly but did not write fresh diagnostic JSON snapshots.
- The Minecraft builder has existing workspace smoke and Living Cities datapack benchmark evidence in local diagnostics; the council did not see every log in this compact pass.

Claims worth deeper inspection:

- `SqliteTableEditorService.QuoteIdentifier` and generic SQLite table editing should be reviewed carefully, even though identifier quoting may already be constrained by table metadata.
- DevExpress static asset packaging should remain part of package smoke tests. Static web assets currently build, but packaged AppX/WebView2 behavior must keep being verified.
- The WebView2 diagnostic snapshot path still needs focused debugging because the latest wrapper smoke automation did not write new JSON despite correct model loading.
- UX/default presets/tooltips still need product attention so non-technical users can operate Chat, Council, Database, Install, and Minecraft Builder pages without losing advanced controls.

## Useful Council Feedback

Architecture strengths the council agreed with:

- The WinUI wrapper staying thin while Blazor/ASP.NET Core owns the application is the right direction.
- Local SQLite memory and council knowledge fit the local-first design.
- Native operations belong behind backend services rather than directly inside frontend JavaScript.
- The AI Council log/memory/knowledge flow is valuable, especially when model output is stored as unverified until user-approved.

Main risks to prioritize:

- Make WebView2 diagnostics reliable enough to test the real wrapper frontend, not only backend endpoints.
- Keep model-context and GPU-load policies explicit in the UI and scripts.
- Treat qwen/gwen/gemma-class 27B/30B models as heavy GPU-risk participants on this AMD 7900 XTX unless a limited-layer profile is active.
- Add focused tests around SQLite table editing and identifier validation.
- Keep improving user-facing explanations, presets, and tooltips without hiding advanced settings.

## Heavy Model Incident Note

After this council run, an attempted `qwen3-coder:30b` pass was aborted because full GPU load triggered AMD driver instability/black screen behavior. That confirmed the need for hard guardrails:

- No parallel large-model loads.
- No long keep-alive for council participants.
- No full-auto GPU for qwen/gwen/gemma-class 27B/30B models by default.
- Prefer `OllamaNumGpu = 20`, short context, and short output budgets for those models.

## Actions Taken After Feedback

- Added a reusable council code-review helper: `LocalGPTWebviewWrapper/build/Invoke-AiCouncilCodeReview.ps1`.
- Added backend council guardrails so qwen/gwen/gemma-class models default to `num_gpu=20` when the caller did not explicitly set `OllamaNumGpu`.
- Updated AI-facing docs with the AMD 7900 XTX heavy-model stability rule.
- Verified `LocalGPT` and wrapper builds after the guardrail changes.

## Suggested Next Follow-Up Passes

Run these as separate, small, low-risk council/reviewer prompts:

- Inspect `LocalGPTWebviewWrapper/LocalGPT/Services/SqliteTableEditorService.cs` for identifier validation and update safety.
- Inspect `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/MainWindow.xaml.cs` and `App.xaml.cs` for why WebView2 smoke snapshots stopped being written.
- Inspect `LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Chat.razor` for whether model/GPU mode warnings are visible enough for normal users.
- Run `qwen3-coder:30b` only with `OllamaNumGpu = 20`, `MaxContextTokens <= 4096`, `MaxOutputTokens <= 1024`, `OllamaKeepAlive = "0s"`, and no other model loaded.
