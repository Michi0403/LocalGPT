## Handoff 16 Nov 2026 (Claude session, LocalGPT 7.2.7 → 7.3.5, PublisherStudio 4.8.8 → 4.8.9)

Done and pushed (each with changelog in maintenance/changelogs or repo root):
- Code generation: codeDomTypes-only reviews rejected; review proposal written to CouncilArtifacts/<name>/<time>-review-<id>-proposed; codegen.review.create raises its own codegen.review.execute approval (hash pinned, build on).
- Model calls: Ollama tool envelopes unwrapped; DX call tags recovered from thinking (chat 7.3.3, Council 7.3.4); final-answer recovery asks for the exact call (7.3.2) and gets the full answer budget (7.3.5); unclosed call tag shows "Function request incomplete".
- Defaults: no hard-coded gpt-oss:20b; reviewer order = Preferred Council models; first-run presets gemma4:12b / deepseek-r1:8b / gemma4:e4b, "Large GPU Council (24 GB)" with gemma4:26b; new installs 16k answer / 32k context; token settings and chosen chat provider persist.
- 1-Wire listener retries a blocked bind (OneWire:ListenRetrySeconds); Keyaccount reviewer uses the run's model; artifact links tolerate trailing quotes.
- Data Management → Councils: Common roles + Keyaccount reviewer picker; all DxCheckBox labels render.
- Hyperlight: Node.js, QuickJS, PHP, Java, shell added (Node/PHP/Java verified in UI).
- Ollama: oversize requests shed largest tool schemas, then fail with a clear error; HTTP errors no longer swallowed (7.3.5).
- Docs build: bounded browser diagnostic drain. PublisherStudio 4.8.9: installer version aligned, release script owns help-docs validation.

Open, in order:
1. Re-run HelloMath with Program Compiler team + gemma4:26b (answer 33k, context 64k) on 7.3.5; expect proposal folder, execute approval, build, ZIP in CouncilArtifacts. Approve as "Claude/automated test" only when the owner allows.
2. Council member selection is not persisted (reverts to gpt-oss:20b after reload); persist like LastChatProviderSession.
3. Media (Neo): missing-image-model branch → queue localai.python.* / localai.model.install approvals, set goal NeedsApproval, resume goal after approval (needs council-run → goal lookup). Verify receipt + insertion in PublisherStudio.
4. Sandbox panel <details> collapses on re-render; live Council lane shows raw <details> text.
5. Teams must keep free tool combinations (CodeDOM, workspace files, approved terminal, Hyperlight); restrict tools only to fit context.
6. Codex Neo notes: branch codex/neo-integration-handoff, maintenance/NEO-INTEGRATION-HANDOFF.md. Docs ZIPs are committed only by the Mac release build.
