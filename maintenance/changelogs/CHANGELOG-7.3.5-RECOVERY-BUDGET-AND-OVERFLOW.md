# LocalGPT 7.3.5

- Final-answer recovery was capped at the step recovery ceiling (default 8,192 tokens) even when the run allows 33k, so gemma4:26b's codegen.review.create call (both files inline) was cut off before its closing tag and nothing ran (run 0a5e37cf). The recovery now gets at least the run's answer budget.
- An opened but unclosed <localgpt-dx-call> now shows a visible "Function request incomplete" step instead of being ignored.
- Ollama requests that exceed the allowed context first drop the largest tool schemas; if still too large, LocalGPT stops with a clear error naming the setting, instead of sending a payload Ollama truncates or rejects (Neo finding: 44k tokens against 32k).
- Ollama HTTP errors were logged but swallowed in OllamaThinkingChatClientEnsureSuccessOrThrowAsync, so rejected requests looked successful and runs ended empty. They are now rethrown.
- Status: compiled without errors (all build checks passed); the documentation PDF step was interrupted at 25/27 when the build window closed, so a full rebuild is still pending. Not yet UI-tested.
