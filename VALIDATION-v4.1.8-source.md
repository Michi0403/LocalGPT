# LocalGPT 4.1.8 source validation

This source archive was prepared without invoking the .NET SDK, `dotnet build`, publish, signing, notarization, or GitHub access.

Static validation for this release must verify:

- all maintained semantic-version surfaces report `4.1.8` and keep one-digit minor/patch components;
- the 4.1.7 runtime-churn/cancellation/database protections remain present;
- the Ollama runtime form uses usable responsive spans and the destructive acknowledgement stays service- and UI-gated;
- maintained Ollama aliases contain the new Gemma 4, Qwen3-VL and Llama 4 families;
- opted-in CanIRun comparison loads the public catalog and compatibility endpoint instead of retaining only `/api/recommend` best picks;
- official-provider catalog lookup remains explicit/user-triggered and bounded while allowing the larger result set;
- English/German localization parity, application architecture, cross-platform boundaries, provider-qualified Council, strict async continuation policy, service resilience, XML documentation and routed render-mode contracts remain intact.

Compilation and runtime execution remain the responsibility of the normal development/release environment because no .NET toolchain is used for this source-only handoff.
