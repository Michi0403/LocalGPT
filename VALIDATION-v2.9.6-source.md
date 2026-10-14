# LocalGPT 2.9.6 source validation


- Native Ollama GPU placement no longer applies the legacy qwen/gwen/gemma family-wide `num_gpu=20` heuristic when no explicit road/run override exists. Low-B variants are therefore no longer accidentally forced into partial offload; explicit `OllamaNumGpu` remains authoritative and Ollama auto-placement handles the default.
- Benchmark Task Curators are explicitly forbidden from adding `UNABLE`, opt-out, delegation, capability-exemption, or ask-the-user escape clauses to the four bounded maintained tasks.
This is a source-only validation record. No `dotnet`, MSBuild or Visual Studio build was run in the release-preparation environment.

Validation performed offline against the supplied LocalGPT 2.9.5 source baseline and the user's large-Council transcript/log evidence:

- XML parsing of all `.csproj`, `.props` and `.targets` files.
- C# lexical delimiter/string/comment scan of all changed C# source files.
- LocalGPT 2.9.6 targeted release audit plus retained 2.9.3/2.9.4/2.9.5 regression audits.
- Existing provider-qualified Council, X-round, benchmark/rejoin, codegen/DXFunction, architecture, service-resilience, async-continuation, XML-documentation and documentation/1-Wire source audits.
- Byte/line invariants against 2.9.5 for all `@rendermode` directives, Wire Protocol source, and `Directory.Build.props`.
- HWiNFO parser check against the supplied LEGENDARYSONIC report: RX 7900 XTX and 24,576 MiB VRAM are detected deterministically; the report is never sent to a model.
- Source scan confirms no active `Win32_VideoController.AdapterRAM` capacity read remains.
- Source-only ZIP integrity, one-root structure and forbidden build/cache artifact scan.

The user-provided benchmark evidence specifically showed repeated `ProviderModelBenchmarkService.ParseFirstJsonObject` failures on absent, HTML-escaped and truncated JSON, a 95-subject task-execution round followed by deterministic calibration, queue-inflated participant durations, malformed approval-gated function proposals, and loss of live result lanes after Stop. The 2.9.6 source changes target those observed failure modes without changing Wire Protocol 2.1.1.
