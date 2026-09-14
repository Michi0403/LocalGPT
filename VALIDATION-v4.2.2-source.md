# LocalGPT 4.2.2 source validation

This handoff was validated from source only. The environment did **not** invoke `dotnet`, compile/publish the solution, sign/notarize artifacts, or use GitHub.

The 4.2.2 release gate verifies that:

- LocalGPT, LocalGPT Setup, and the webview wrapper are versioned 4.2.2 while the one-digit minor/patch release rule remains satisfied;
- the old explicit policy that treated native Ollama and its `/v1` facade as separate provider identities is absent;
- provider discovery retains the concurrently started native Ollama and OpenAI-compatible probes but records native Ollama discovery before merging compatible candidates;
- compatible candidates are collapsed only when the OpenAI-compatible endpoint is exactly the `/v1` facade of the same native Ollama authority and the concrete model identifier matches;
- unmatched OpenAI-compatible models and genuinely independent compatible providers remain visible;
- configured `/v1` aliases that are proven to be Ollama carry configuration state onto the canonical native candidate;
- persisted OpenAI-compatible Ollama-facade Council bindings can reconcile to one uniquely matching native Ollama candidate;
- the 4.2.1 metadata-backed console identity, 4.2.0 catalog/workbench behavior, CanIRun expansion, and prior runtime/signing protections remain guarded.

Maintained Python source gates, architecture/resilience policies, XML documentation coverage, PowerShell interpolation checks, provider-qualified Council audits, configurable-behavior policy, async-continuation policy, and cross-platform boundaries are re-run against the delivered tree.

No compiler/runtime claim is made here. The user's build/release environments remain the authoritative compiler and packaging tests.
