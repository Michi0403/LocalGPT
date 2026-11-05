# LocalGPT 4.6.7 - Chat render repair

## Chat runtime repair

- Repaired the `UploadProcessingAdvisor` DevExpress grid markup that caused `/chat` to disappear at runtime.
- Wrapped every `DxGridLayoutItem` body in the component's required `<Template>` child instead of relying on unsupported implicit `ChildContent`.
- Preserved the existing Chat page, upload quarantine flow, recommendation popup, Council handoff, explicit promotion gate and `InteractiveServer` ownership.
- No alternate Chat layout, nested render circuit or native file-input fallback was introduced.

## Regression prevention

- Extended `build/Assert-DevExpressBlazorControls.ps1` with `LGDX0002` validation for `DxGridLayoutItem` template ownership.
- Future source builds now fail before Razor runtime startup if a grid item is written with unsupported implicit child content.

## Reference review

- Reviewed PublisherStudio 3.7.4 upload/file-selection handling as an architectural reference: upload handling remains localized to its owning component/workflow instead of restructuring the surrounding page.
- PublisherStudio source is unchanged.

## Release identity

- Bumped active LocalGPT identities from 4.6.6 to 4.6.7.
- Version-slot policy remains satisfied: no second or third slot reaches two digits.
