# LocalGPT 3.1.4

LocalGPT 3.1.4 is the XML documentation completeness successor to 3.1.3. It preserves Council round recovery, cancellation handling, live UI stability, durable benchmark evidence and machine-derived coverage truth while extending repository documentation enforcement to Razor code-behind, Razor `@code` members, Razor component types and enum members.

## Toolchain state

- .NET SDK policy: `10.0.400`
- Target framework: `net10.0`
- DevExpress: existing `25.2.*` package lane retained
- 1-Wire protocol: `2.1.1`

## Documentation state

- 9,865 maintained direct C# declarations pass contextual XML documentation validation;
- 45 Razor component types have documented partial class declarations;
- 752 direct Razor `@code` declarations pass XML documentation validation;
- required `<typeparam>`, `<param>`, `<returns>` and `<value>` tags are checked for explanatory text, not mere presence;
- enum members are now individually documented and audited;
- `.razor.cs` files are no longer excluded from the documentation scanner;
- the established repository XML documentation validation entry point now covers both C# and Razor.

## Compatibility

No database migration, benchmark evidence schema migration, provider runtime change, Council orchestration change, or 1-Wire protocol change is introduced by 3.1.4. The new Razor companion partial classes carry documentation only and do not add runtime behavior.

See `CHANGELOG-v3.1.4-XML-DOCUMENTATION-COMPLETENESS.md` and `VALIDATION-v3.1.4-source.md`.
