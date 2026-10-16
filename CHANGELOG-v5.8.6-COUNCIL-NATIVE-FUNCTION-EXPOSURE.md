# LocalGPT 5.8.6 — Council Game Team native DXFunction exposure

## Reproduction from the supplied run

- Game Development architect's source listing failed: `chat.upload_workspace_files` was in the registered DXFunction catalog, but the Ollama client returned `NotFound` ("The requested automatic function is not registered").
- Architect then tried `dxai.tools.discover` with `take=1000`, rejected because that *different function* has a maximum page size of 50. Subsequent discovery succeeded but did not read source.
- Deterministic curator did successfully hash/read all 1,072 original/extracted items. Source reading and game creation could not progress; the run correctly marked the missing evidence as incomplete.

## Root cause

`CouncilAutomaticFunctionPolicyService.Resolve` discarded explicitly configured automatic/native function names for any ordinary (non-`games.*` runtime) Council team, returning `null` as the allow-list. `OllamaThinkingChatClient.GetAutomaticFunctions` interprets `null` as *broker only*. The source-file reading functions were therefore discoverable by the broker but not advertised/invocable as native tools in the Game Development workflow. This mismatch did not affect broker-based general/learning rounds.

## Changes

- Ordinary Council steps with configured Team/Exact function lists now expose those specific registered names as native tools **plus** the three established DXFunction broker functions (`dxai.tools.discover`, `dxai.tools.preview`, `dxai.tools.invoke`).
- Ordinary workflows without configured names preserve their existing broker-only default. Strict `games.*` runtime teams retain the original hard allow-list semantics without the additional broker functions.
- Every proposed name must still exist in the current registry and satisfy AI/direct/automatic invocation conditions. Target DXFunction policy, validation, and human-approval boundaries are unchanged.
- Game architect's template now provides exact source-tool usage and distinguishes discovery's 50-item limit from workspace listing's 20,000-item limit.
- Existing PowerShell Council source contract check now checks direct-exposure wiring and source-tool declarations.
- LocalGPT 5.8.5 -> 5.8.6. PublisherStudio, PDFsharp, ReleasePackaging unchanged.

## Remaining requirements

- The user must build and rerun Game Development on Windows/Ollama to establish runtime success. No .NET compilation or Ollama invocation was performed here.
- Complete byte curation is not semantic source understanding; successful `chat.upload_workspace_file` tool results and project build/playtest results remain distinct requirements.
