# LocalGPT 3.1.7

LocalGPT 3.1.7 is the compile-repair successor to 3.1.6. It retains the three service-backed `/chat` quick selectors and live Chat Configuration refresh while correcting the DevExpress callback form identified by the user's real .NET build.

## Toolchain state

- .NET SDK policy: `10.0.400`
- Target framework: `net10.0`
- DevExpress: existing `25.2.*` package lane retained
- 1-Wire protocol: `2.1.1`

## DxComboBox callback repair

The Council team, model preset and performance preset `DxComboBox` controls now use explicit typed `ValueChanged` lambdas. This gives Razor/DevExpress the concrete callback argument type needed to construct the component `EventCallback` and removes the 3.1.6 `CS1503` method-group conversion failure.

The handlers themselves are unchanged and still delegate to the existing service-backed detailed Chat Configuration application paths.

## Preserved behavior

The 3.1.6 live service refresh, 3.1.5 repetition watchdog, Council recovery/failover, cancellation handling, benchmark evidence, coverage truth guard and XML documentation completeness work all remain present. No database migration or evidence-schema migration is introduced.

See `CHANGELOG-v3.1.7-DXCOMBOBOX-TYPED-CALLBACK-REPAIR.md` and `VALIDATION-v3.1.7-source.md`.
