# LocalGPT 7.2.6 - Model context fit

Not built here (no .NET SDK in this working copy); source edits only.

- Ollama requests now read the model's maximum context from `/api/show` (`model_info` key ending in `.context_length`, cached per host and model for the process). The effective `num_ctx` is the smaller of the saved value and the model maximum; `num_predict` is reduced for that request when prompt plus output would not fit. Saved Chat configuration is never changed. Unknown maximum keeps the previous behaviour. Information log: model, configured, model maximum, effective context and output.
- A member answer made predominantly of `<unusedNN>` placeholder tokens now fails the step ("produced placeholder tokens instead of text (likely context or memory overflow for this model)") through the 7.2.5 empty-answer path.
- New runtime values: `OllamaModelContextLengthKeySuffix`, `OllamaModelDetailsLookupTimeoutSeconds`, `CouncilPlaceholderTokenPrefix`, `CouncilPlaceholderOutputMinimumPercent`.
- Docs files (docs/docfx.json, docs/index.md, docs/pdf-cover.html, docs/pdf/toc.yml) need the 7.2.6 bump on the device.

Check: a gemma4:26b council with 262144 configured now logs an effective context at the model maximum and produces text; placeholder-only answers show a failure line.
