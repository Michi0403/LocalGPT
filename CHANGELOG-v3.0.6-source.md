# LocalGPT 3.0.6 source changelog

## Compile-repair release

LocalGPT 3.0.6 is a focused source repair over 3.0.5. It keeps the AI-guided hardware/provider/model initial-setup architecture intact and repairs compile blockers reported from the 3.0.5 source package.

### Fixed

- Repaired the missing closing parenthesis in `ConsoleHistoryFunction` around the bounded `take` parameter read.
- Added the missing `LocalGPT.BusinessObjects` import to `InitialSetupProviderConfigurationDxAiFunction.cs`, restoring resolution of `DxaichatFunctionInfo`, `DxAiFunctionInvocationRequest`, and `DxAiFunctionInvocationResult` and therefore the `IDxAiFunctionHandler` contract.
- Disambiguated LocalGPT's application `ConfigurationRoot` from `Microsoft.Extensions.Configuration.ConfigurationRoot` in `AiProviderBootstrapService` by using the fully qualified LocalGPT business-object type for both the options monitor and persisted configuration root.
- Updated the maintained runtime version identification used by the opt-in CanIRun.ai request to 3.0.6.

### Preserved

- The 3.0.5 AI-guided initial setup, cross-platform shared console, provider bootstrap, model installation, hardware recommendation, benchmark-team and Council wiring remain additive and unchanged except for the compile repairs above.
- Existing confirmation gates for consequential console/provider operations are retained.
- LocalGPT 1-Wire protocol remains `2.1.1`.
- No EF migration or schema change is introduced by 3.0.6.
- InteractiveServer render-mode boundaries were not intentionally changed by this release.

### Version

- LocalGPT: 3.0.6
- LocalGPTWebviewWrapper: 3.0.6
- LocalGPTInstallerConsole: 3.0.6
