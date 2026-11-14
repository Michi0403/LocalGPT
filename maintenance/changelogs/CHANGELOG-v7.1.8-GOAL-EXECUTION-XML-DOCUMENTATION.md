# LocalGPT 7.1.8 — Goal execution constructor XML documentation

- Fixes compiler error `CS1573` from the November 14, 2026 Windows Debug build: the `GoalExecutionService` primary constructor has a `configuration` parameter but was missing its XML `<param>` documentation.
- Documents the existing `IOptionsMonitor<LocalGPT.BusinessObjects.ConfigurationRoot> configuration` parameter without changing its type, DI registration, behavior, model preferences or 1-Wire contracts.
- Increments LocalGPT application version from 7.1.7 to 7.1.8; setup installer version remains unchanged.
- This is an XML documentation-only C# source correction. No UI, inference, configuration serialization, SignalR, MCP, installation or networking code has changed.

**Build limitation:** The .NET SDK and PowerShell are not installed in the packaging environment; a complete build and runtime checks have not been performed here.
