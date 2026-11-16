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
