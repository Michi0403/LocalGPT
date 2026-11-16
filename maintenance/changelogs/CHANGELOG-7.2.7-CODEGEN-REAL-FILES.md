# LocalGPT 7.2.7 — code generation writes real files

## Problem
Coding teams returned "empty" solutions. Review `9adf744a…` (StreamScheduler) showed why: the model put every type into `codeDomTypes`. The CodeDOM emitter only supports one sealed class with one method that returns a string, so `IStreamService` became `public sealed class IStreamService { string CreateStreamEventAsync() => "Task<StreamEventDto>"; }`. It had no Program.cs, no real code, and verification could never succeed.

## Fix
- `codegen.review.create` now rejects requests that have `codeDomTypes` but no `files` with content. The error tells the model to resend complete sources in `files[]` (relativePath + content, including .csproj and Program.cs), so it can self-correct in the same round.
- The function description now says this up front.

## Test
In /chat with team program-compiler-team (gemma4:26b), ask for a small .NET 10 console app. The review folder must contain full source files. A codeDomTypes-only call must show the rejection message in the run.

## Also in 7.2.7
- gpt-oss tool calls arrived with the envelope as tool name (`localgpt-dx-call`, `assistant`) and failed as "not registered". Ollama tool calls are now unwrapped to the inner `functionName`/`parameters` (allow-list still applies).
- Build-LocalDevelopment.ps1 builds LocalGPT.PluginContracts before the app (fixed CS0006 on a cleaned obj folder).
- 1-Wire listener: a failed bind (10013 = Windows excluded port range, 10048 = port in use) now retries every OneWire:ListenRetrySeconds and logs the cause instead of leaving 1-Wire silently dead. This was the "connection refused" PublisherStudio saw on 15 Nov.
- Keyaccount reviewer: blank reviewer now uses the model the run actually used (endpoint-equivalent match, no IsConfigured requirement); the warning log names the keys when nothing matches.
- Token settings: Council answer/context token changes are saved to DefaultMaxOutputTokens/DefaultContextTokens, context changes sync per-model routes, and goal checkpoints keep ModelRoutes, so restarts and subgoals no longer fall back to 262,144.
- Single-AI chat rendering: an open model code fence is closed before LocalGPT trace panels, and raw or encoded <localgpt-dx-call>/<unusedNN> markup is stripped outside code (patterns runtime.render.dx-call-markup / placeholder-token).
- /__artifacts/council/: trailing quote/bracket characters in model links are trimmed; a missing artifact returns a clear message instead of an empty 404.
