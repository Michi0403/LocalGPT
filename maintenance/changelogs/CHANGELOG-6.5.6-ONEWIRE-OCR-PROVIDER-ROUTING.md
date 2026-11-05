# LocalGPT 6.5.6 — OCR host selection / clear failure reporting

- OCR no longer assumes that the first Chat Ollama endpoint hosts the OCR model. It checks installed model inventories (`api/tags`) on the existing operator-configured Ollama endpoints with a bounded probe, selects an endpoint which actually advertises the requested model, or reports that no reachable configured host has it.
- Caller cancellation remains propagated; OCR policy ceilings remain in effect. No automatic download, installation, network discovery of untrusted hosts or security bypass is introduced.
- Source-only review; no .NET or PowerShell runtime available in the authoring environment. **A successfully generated OCR layer has not been demonstrated.**
- No Python or Node validation scripts added.

- The standard Council final Consensus writer now publishes a live participant activity (status, provider fragments, result and completion) like the other Council phases. The saved Consensus step and existing ordered transcript handling are preserved. This is source-only; browser visibility has not been verified at runtime.
