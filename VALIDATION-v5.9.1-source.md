# LocalGPT 5.9.1 — Static validation

- Baseline: user-supplied LocalGPT 5.9.0 source ZIP.
- Compare against user-supplied LocalGPT 5.5.0 source ZIP and available 5.6.1 / 5.7.1 source ZIP snapshots.
- Root cause: `null` automatic function policy changed from all eligible handlers to `DxFunctionBroker`-only after 5.6.1 and no later than 5.7.1.
- In 5.9.1, the default again exposes all eligible functions; preferred lists include broker access and keep direct-call compatibility; strict/disabled lists remain exclusive.
- Source contracts include the exact `chat.upload_workspace_files` and `chat.upload_workspace_file` handler descriptors, automatic-call eligibility, native-name resolver, and the existing policy/registry invocation path.
- Changelog and DocFX UML reference updated. Project version advanced to 5.9.1.
- Entire original ZIP must be retained; ZIP CRC and source preservation checks are performed at packaging time.
- Windows PowerShell, MSBuild, .NET and Ollama were NOT executed. Native-client model behavior and end-to-end Council function results remain unverified until tested on Windows.
