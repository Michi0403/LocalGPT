# LocalGPT 7.1.6 — preferred-model editor build-contract repair

- Corrected three architecture/source-guard regressions introduced with the 7.1.4 preferred-model UI: calculated `DxCheckBox` inputs now explicitly opt out of field-expression validation, the model display helper is an instance method instead of a prohibited application static method, and Razor arrow buttons delegate priority-bound checks to their code-behind rather than performing `IndexOf` string/list operations in attributes.
- Qualified the application configuration BusinessObject in `GoalExecutionService` to remove ambiguity with `Microsoft.Extensions.Configuration.ConfigurationRoot` (`CS0104`).
- Moved the family JSON convention type from the prohibited static class in `Services/OneWire` to a stateless non-static `BusinessObjects` type, updating its 1-Wire, MVC and MCP call sites and its source compatibility assertion. No serializer settings or wire format changed.
- Kept the saved preferred-model pool, Council/goal-selection rules, logs, 1-Wire/MCP/SignalR contracts, cross-platform installer scripts and application data layout unchanged.
- Advanced the LocalGPT application version from 7.1.5 to 7.1.6; installer version remains 5.0.3.

Windows Debug build evidence: user-supplied build logs from November 13, 2026. The four failures were observed in successive builds. This correction has not been compiled or executed on Windows, macOS or Linux in the preparation environment; all subsequent build and runtime verification remains necessary.
