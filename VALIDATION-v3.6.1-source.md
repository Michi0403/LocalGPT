# LocalGPT 3.6.1 source validation

Static validation only; no .NET build was run.

- Confirmed LocalGPT project, installer-console, and WebView wrapper versions are 3.6.1.
- Confirmed generated macOS launcher contains runtime endpoint-file lookup, HTTP fallback probe for port 5000, five-minute startup allowance, browser open, and Terminal log helper.
- Confirmed the optional LocalGPT dependency helper contains Homebrew Ollama install, model pull, LM Studio download guidance, and status paths.
- Confirmed Unix release packaging removes only transient RID staging and transient `.app` working copies after native artifacts return successfully.
- Confirmed no GitHub access or .NET compilation was used for this patch.
