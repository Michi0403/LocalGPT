# LocalGPT 6.3.1 — Learning/Council/Ollama/console source repairs

Source baseline: the user's `LocalGPT-6.3.0-MFA-stop-learning-source-CANDIDATE(1).zip`.
Release status: **offline source candidate; not compiled or exercised in .NET/Windows**.

## Changes actually introduced in 6.3.1

1. **Learning Round source recovery** (`MultiModelCouncilService.SourceReviewRecovery.cs`): recovery now considers all nonempty extracted archive entries instead of only ZIP entries or files already embedded in the prompt. It keeps the configured workspace reference, per-archive representation, priority terms, real bounded `ReadFileAsync` content checks, and verified-read receipts. A hash-only ZIP scan does not silently satisfy semantic reading. This addresses a genuine selection restriction consistent with the reported 0/1 file, 0/2 archive-root exception. It does not prove a multi-thousand-file semantic learning run completes.
2. **Ollama model disappearance** (`AiProviderBootstrapService.cs`): bootstrap profiles are read from all active SQLite knowledge records instead of the newest 500 entries; user-edited/approved priority, existing regex parsing, maintained aliases including `gemma2:27b` and `gemma3:27b`, and direct provider-model-ID support remain in place. This fixes the bounded-summary starvation path, not network-side Ollama catalog availability.
3. **Council/DXFunctions** (`OllamaThinkingChatClient.Tools.cs`, `.ToolExecution.cs`, `Council/CouncilPreflightService.cs`): AI participants can see and request directly invocable DXFunctions that require a human decision, not only unattended-safe functions; the resolver accepts the same capabilities. The central registry still validates arguments, asks through Human Collaboration, queues deferred calls, and does **not** execute pending/declined invocations. An optional organic skill is not an artificial prerequisite for an existing registered DXFunction. This does not grant network/database access to functions that are not installed, configured, connected, or approved.
4. **Physical Ollama host UI** (`Components/Pages/Install.razor`, `Components/Shared/InitialSetupAssistantPanel.razor`): one editable host panel per normalized host in `/Install`, and one physical-host summary per endpoint in setup with separate GPU/accelerator rows. Changing a setup host endpoint propagates to its related device rows. Keeps per-device selection/VRAM metadata.
5. **Install/operations output** (`OperationalActivityFeedService.cs`, `OperationalActivityFeed.razor`, `wwwroot/js/localgpt-operational-feed.js`): recent command progress is rendered after static status/activity, so new Ollama pull lines appear at the terminal tail. The terminal follows fresh output unless the user scrolls away to inspect earlier output. No fake progress or model-download success is asserted.
6. **ASCII terminal window** (`Chat.razor`, `Chat.razor.cs`, `Chat.razor.css`): reuses the existing interactive ASCII terminal within popup sizes Compact, Comfortable and Full window without remounting the conversation when switching. This is **not yet** a draggable/resizable, persistent bottom dock with an independent 5–10-line minimode; those remain follow-up runtime/UI work.
7. **Static regression coverage**: adds checks for the uncapped provider-profile read, approval-gated AI invocation, host grouping, console-tail behavior, and ASCII modes; updates the old source-selection test to catch the previously wrong prompt-only ZIP filter.

## Inherited from the user's 6.3.0 source; not newly fixed here

- The independent OneWire MFA-enrollment verification popup is present with a real TOTP check and is not tied to trusting a remote peer.
- The Council/Chat stop cancellation plumbing is present. The report that Stop still hangs in the installed application remains **unverified/unresolved** until a running instance and model provider can be exercised.
- The full end-to-end archive/regex ingestion across *every file* and all Learning Round phases is not proven by a source regression test.
- The ASCII Chat/Game/Operator view integration, bottom dock, free resize, and true fullscreen visual behavior have not been demonstrated in an interactive browser.

## Verification and limits

- `python -m pytest -q tests/static`: **29 passed** in the offline environment.
- JavaScript syntax check of `localgpt-operational-feed.js`: passed.
- No .NET SDK/compiler build, DevExpress integration, Ollama connection, live MFA, browser session, Windows installer, or complete Council round was run; do not present this archive as a tested executable release.
- Existing authorization, permissions, database schema, source-review truth checks, and architecture/build gates have not been intentionally removed or bypassed.

## Follow-up acceptance checks on target machine

1. Open `/Install`, detect one Ollama daemon with multiple GPUs and verify one host editor, all GPU rows, and a visible continuous pull-progress tail.
2. Verify `gemma2:27b`, `gemma3:27b`, and provider-owned model-ID entry/pull separately against your reachable Ollama instance.
3. Start a two-ZIP Learning Round; verify full-file read receipts for the requested files, source-review gates on both archive roots, and completion without the 0/2 exception.
4. Have a Council model request a registered service/database DXFunction. Check that the human approval inbox receives the exact request, that pending requests execute nothing, and that approved calls run through the authorized service.
5. Enroll MFA, verify the popup once, then start and stop standard chat and Council runs; confirm stream cancellation, provider unload, and no orphan session.
6. Test ASCII terminal sizes across Chat/Game/Operator. Bottom-line minimization, drag-to-resize, and persistent dock require additional implementation if still desired.
