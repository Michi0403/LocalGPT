# LocalGPT 7.3.6 — bounded conversation context and knowledge-assisted coding subagents

- The Chat→Council prompt now retains whole recent normalized turns within the operator-owned `CouncilChatHistoryBudgetCharacters` budget (default 48,000 characters). It removes older conversational repetition **from the provider request only**; the durable transcript and original attachments are unchanged. This does not circumvent an Ollama model's advertised maximum context length.
- Adds the `knowledge-assisted-coding-subagents` Council blueprint: evidence retrieval and regex preparation, bounded multi-model code candidates, approval-gated materialization through `codegen.review.create`, independent `council.development.artifact.verify` check, and truthful download handoff. Role members remain provider-qualified and user-configurable (no hard-coded Gemma or DeepSeek preference).
- Existing `program-compiler-team` is retained. Existing user-edited Council teams remain database-owned and are not replaced by this seed.
- Ollama request preflight amendment (in the upgrade patch) removes optional older conversation turns before dropping provider-native tool schemas and reports which effective context boundary caused a failure.

## Validation limits

This is a source overlay for the latest GitHub main (7.3.5), **not a compiled 7.3.6 release**. The offline assistant container lacks .NET 10, PowerShell and the DevExpress NuGet feed. Run `Build-LocalDevelopment.ps1` and a full UI/Ollama approval/build/ZIP test on your machine before promotion.

## Acceptance

1. Council started from a long DXAiChat history with gemma4:26b and 65,536 fitted-context ceiling: history is compacted; no unbounded identical context-overflow retries.
2. Verify the new team uses knowledge evidence and all role participants can be assigned without a hard-coded model family.
3. Request HelloMath; codegen creates both real files, approval enters Human Collaboration inbox, owner approves, .NET 10 build and test run, artifact verify returns success and a nonempty downloadable ZIP.
4. On pending approval/failed build, the final handoff must not claim a ZIP was delivered.
