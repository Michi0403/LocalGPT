# LocalGPT 6.1.9 source verification

- Bounded decoding before JSON deserialization in `GoalExecutionService.SemanticPlanning.cs` reviewed for escaped quotes and ampersands.
- Goal planning and durable resume now distinguish connected executable 1-Wire peer capabilities from organic skill keys. No 1-Wire permission gate or executable handler was changed. The ledger structure and Council execution were preserved.
- Not compiled or executed under .NET/PowerShell. Live model-provider responses need host verification.

- The supplied application log also shows a separate `ObjectDisposedException` for an Ollama HttpClient during streaming. This release does **not** claim to repair that independent lifecycle issue without a reproducible owning-call sequence.
