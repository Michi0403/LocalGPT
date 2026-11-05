# LocalGPT 5.8.5 — Council inspection popup sizing

## Change

- Size the **Council answer / provider evidence** DevExpress `DxPopup` at **90vw x 90dvh**, with a 90dvh maximum height; the popup no longer relies on auto height or the former `min(98rem, 96vw)` width cap.
- Align the portal-rendered evidence layout with that popup's usable body height instead of capping it at 78dvh / 58rem. Reserve 5rem for popup chrome and body padding and retain a bounded interior layout.
- Preserve two separately scrollable panes for completed member answers and their provider stream; on narrow screens stack the panes in two equal, min-height-zero grid rows.
- Keep the existing glass-surface styling, DevExpress popup, Council data retrieval, project workflows, and the other ASCII game popup unchanged.

## Version

- LocalGPT: **5.8.4 -> 5.8.5** (version segments remain single-digit as requested).
- ReleasePackaging, PDFsharp, PublisherStudio: unchanged.

## Verification scope

- Source-only change; no dotnet build, PowerShell build, browser runtime, or GitHub access was used.
- See `VALIDATION-v5.8.5-source.md` for static checks and Windows acceptance steps.
