# LocalGPT 4.6.6 — 4.6.5 build and architecture repair

## Razor / DevExpress compile repair

- Gave the upload recommendation `DxPopup.BodyContentTemplate` an explicit context name, preventing the nested `DxFormLayoutItem` and `DxAccordionItem` templates from colliding with Razor's implicit `context` parameter.
- Kept the existing DevExpress popup, form-layout, accordion, file-input, button and wait-indicator design; no native-control fallback was introduced.
- Preserved the current render-mode architecture: routed application pages remain explicit `InteractiveServer` boundaries and shared children inherit those circuits.

## Numeric editor compile repair

- Changed `InvariantDoubleEditor` to use an explicitly typed `double?` `ValueChanged` lambda for `DxSpinEdit`.
- Kept the invariant-culture DevExpress numeric mask, min/max values and spin increment behavior.

## Chat upload handoff repair

- Added the missing `Microsoft.JSInterop` import to `Chat.UploadAdvisor.razor.cs`.
- This restores the intended `IJSRuntime.InvokeAsync<T>` and `InvokeVoidAsync` extension overload resolution used by the existing Chat composer bridge.

## Text-service ownership repair

- Removed direct `string.Join` work from `UploadProcessingAdvisor.razor`.
- Added a resilient `CouncilTextService.FormatDistinctJoinedListOrFallback` operation and delegated the component's distinct/fallback display formatting to it.
- No text-ownership baseline exception was added.

## Release identity

- Bumped active LocalGPT identities from 4.6.5 to 4.6.6; the version-slot rule remains enforced and no second/third version slot reaches two digits.
- PublisherStudio was not changed.
