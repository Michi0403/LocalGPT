# LocalGPT 2.6.7 - Multi-host Council stream, tools, and evidence repair

## Why this release exists

A real 18-member Council run selected models from two Ollama PCs correctly, but the streaming execution path forced the effective model concurrency to one for the entire run. The second host therefore participated only after the first host's queued work had progressed, which looked like the remote Council half was ignored. The same run also exposed heartbeat text inside member prose, uneven DXFunction availability for Ollama models that reject native tool metadata, confusing upload-workspace provenance, and unsupported "missing feature" claims.

## Council host scheduling

- `MaxParallelModels` now means **maximum concurrent model requests per participating AI host/PC** when parallel hardware roads are enabled.
- Provider-qualified model identities are grouped by canonical execution host; `localhost` and `127.0.0.1` share one physical host group while a LAN Ollama endpoint gets its own group.
- Each host receives an independent bounded semaphore. Two Ollama PCs can therefore work during the same logical Council phase instead of one PC waiting for the other host's entire queue.
- The existing hardware-road lease still applies inside each host, so explicit CPU/GPU/accelerator lane limits remain authoritative.
- A Council phase still has a completion barrier: Discussion/Review/Consensus does not advance until every assigned member in that phase has completed, failed, or been explicitly skipped.
- Streaming presentation uses per-member channels. Model execution can overlap across hosts while the DXAiChat output renders one intact member stream at a time, avoiding invalid interleaving of thinking/tool HTML and member prose.
- Model Council UI and generated catalog wording now state `Parallel models per AI host` explicitly.

## Live status and transcript integrity

- Repeated `Council still running after ...` heartbeats are no longer appended to the Council transcript or model text.
- Heartbeats only refresh the live session timestamp.
- Current Council phase/progress is stored as live-session status and rendered as the second line below the single `Council ... is still running` indicator.
- LocalGPT-owned streaming status fragments remain visible to the user but are excluded from the accumulated model answer/thinking buffers. They can no longer split a model sentence and become part of a Missing Feature Report, saved chat, or final answer.

## Provider thinking and DXFunctions

- Provider-supplied thinking/reasoning remains visible and is explicitly allowed to contain useful self-correction.
- Final-answer recovery no longer instructs models to suppress thinking or tools; it still requires a substantive user-visible final answer.
- The Council base prompt now protects visible provider thinking/self-correction and exact registered DXFunction use as maintained behavior.
- Ollama models that return HTTP 400/501 for native tool metadata receive a bounded textual DXFunction contract instead of simply losing tools. Textual calls are recovered through the existing DXFunction registry and normal policy checks.
- The fallback lists only real registry names and tells the model never to invent a function name.

## Upload workspace evidence

Three real read-only DI DXFunctions are now available to Council members:

- `chat.upload_workspace_files`
- `chat.upload_workspace_context`
- `chat.upload_workspace_file`

The workspace prompt now distinguishes original user uploads from generated `context.md`/`manifest.json` artifacts and reports original upload count/bytes separately. A generated 150 MB context describing thousands of repository entries no longer implies that thousands of files were uploaded by the user.

## Missing-feature and exact-source evidence rules

Missing feature reports must distinguish:

- **Verified missing** - current source/runtime/database/log evidence proves absence.
- **Not verified / not found** - the Council searched available evidence but cannot prove absence.
- **Requested / desired capability** - a member wishes for or recommends the capability; creative requests are explicitly welcome.

If a user asks the Council to review, learn, test, or modify the **exact running LocalGPT/PublisherStudio source** and that exact source tree/archive/full matching dump is not available, the Council must create a `human.collaboration.request` Guidance request such as `Running source required`. It must appear in **Open Requests** instead of continuing as though the source had been inspected.

## Guard hardening

`build/audit_provider_qualified_council.py` now checks the multi-host execution gate, phase completion barrier, intact stream presentation, heartbeat separation, upload DXFunctions/provenance, textual tool fallback, preserved provider thinking/self-correction, evidence taxonomy, and running-source Open Request contract.

## Version

- LocalGPT: 2.6.7
- Installer Console: 2.6.7
- WebView Wrapper: 2.6.7
- Organic 1-Wire protocol: unchanged at 2.1.1
