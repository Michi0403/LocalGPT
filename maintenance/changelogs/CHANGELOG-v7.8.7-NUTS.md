# LocalGPT 7.8.7 – Unruffled Uakari

## Council delivery

- A new Chat starts with its own conversation. Follow-ups reach a running Council only after you explicitly open its live transcript.
- The .NET solution toolchain writes `.slnx` files for reviewed C# projects. An approved build can deliver the solution, source, and compiled output together in the Chat ZIP.

## NUTS benchmarks

- In `/install`, register the Ollama providers you want to compare. On **AI Council**, select the provider and model for each benchmark participant. Select one route when localhost and a LAN address point to the same Ollama instance.
- Open **NUTS role assessment** in the benchmark panel. Enable the scenario and edit its JSON. A scenario can include up to 256 cases for configured Council roles. The supplied scenario includes C# and PublisherStudio layout and capability-selection checks.
- To reproduce the four live smoke cases on another machine, paste [`NUTS-SMOKE-SCENARIO.json`](../../NUTS-SMOKE-SCENARIO.json) into the scenario editor and select the same models if installed. Use this public example to check setup. Use a separate set of held-out cases for adapter qualification.
- Saved reports show which cases passed, failed, or produced no measurement. A role suggestion requires at least two successful cases with complete coverage. Select the recommended roles and request approval in the Human Collaboration Inbox. Approval adds the provider-qualified models to the preset while retaining its existing models and role policies. For a supplied preset, select the editable copy in Chat.

## Neural Training and LoRA

- The Neural Training Council can review measured model gaps and assemble a dataset from user-verified knowledge revisions. Save and verify the source revisions you want to use before approving dataset preparation. Keep evaluation examples separate from training examples. The training tool requires a compatible original local `safetensors` checkpoint.
- To compare an existing LoRA, first make its model variant available through a provider that exposes model digests. Benchmark the base and variant in the same run with matching profiles and a dedicated `heldOut` scenario. The review records the exact revisions and accepts a role only when the variant passes every case for that role and improves on the base. Apply an accepted review explicitly. Council role resolution checks the provider digest again before using it.

## Verification

- The live Windows compiler test used Qwen 3.5 4B. Chat reported a verified build and returned a ZIP with the `.slnx` and executable. The extracted program returned `Even` for `2` and `Odd` for `3`.
- The Windows benchmark used local Ollama with `qwen2.5-coder:1.5b` and Ollama on a second machine with `qwen2.5-coder:3b`. Both models passed all four held-out cases for eight successful responses. The adapter-review test captured the tie and reported the candidate needed improvement. The PublisherStudio cases verify layout and accessibility answers. Live 1-Wire rendering and adapter training require their own verification.
- A second Windows run using the public smoke scenario again passed eight out of eight cases. The two recommended defaults were saved through the frontend in the test database.
