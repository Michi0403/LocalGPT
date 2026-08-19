# LocalGPT 3.1.7 — DxComboBox typed callback repair

## Fixed

- Corrected the three `/chat` quick-configuration `DxComboBox` `ValueChanged` bindings introduced in 3.1.6.
- The untyped method-group form produced `CS1503` because Razor/DevExpress could not convert the method group to the required `EventCallback` during component code generation.
- Each selector now uses an explicit typed lambda matching the established DevExpress pattern already used elsewhere in LocalGPT:
  - `OrganicCouncilTeamDefinition` for Council teams;
  - `CouncilModelPreset` for model/team presets;
  - `HardwarePerformancePreset` for performance presets.
- The existing nullable handler methods are retained, so the shared application paths and defensive null handling are unchanged.

## Preserved

- all three quick selectors and their placement beside the `/chat` composer actions;
- service-backed Council team, model preset and performance preset data;
- live Chat Configuration refresh added in 3.1.6;
- provider/model refresh behavior;
- provider-stream repetition watchdog and benchmark continuation behavior from 3.1.5;
- Council round/member recovery and cancellation behavior;
- benchmark evidence and coverage-truth safeguards;
- XML documentation completeness enforcement;
- EF migration state, BenchmarkEvidence schema 1 and 1-Wire protocol 2.1.1.

## Regression guard

`build/audit_chat_quick_configuration_3_1_7.py` now requires the explicit typed `ValueChanged` lambdas and rejects the untyped method-group forms that caused the compile failure.

## Validation scope

This archive was repaired from the 3.1.6 source based on the concrete compiler diagnostics supplied from the user's .NET/DevExpress build. The preparation environment still has no `dotnet` SDK, so the repaired archive is source-only and is not represented as locally compiled.
