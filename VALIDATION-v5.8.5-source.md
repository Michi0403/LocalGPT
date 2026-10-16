# LocalGPT 5.8.5 — static validation and acceptance

## Performed (without dotnet or GitHub)

- Verified all 2,111 original version-5.8.4 repository files remain present; only the Council popup Razor, its portal-scoped CSS, and the LocalGPT `.csproj` version were modified.
- Verified the Council evidence popup defines both `Width="90vw"` and `Height="90dvh"`, and retains `MaxHeight="90dvh"`.
- Verified the ASCII console popup remains untouched.
- Verified the evidence layout uses `calc(90dvh - 5rem)` (instead of its old 78dvh/58rem clamp) and narrow layouts retain two independently scrollable panes.
- Verified project XML and complete source ZIP integrity.

## Windows visual acceptance (not run here)

1. Open `/chat`, complete a Council run, and click **Inspect answer, thinking and provider evidence**.
2. Check the DevExpress popup occupies roughly 90% of the current viewport's width **and** height, including on a shorter window; check the header and Close control remain visible.
3. Scroll large completed answers and provider streams separately. Resize to <=800px: confirm panes stack, both remain scrollable, and the popup can still be dismissed.
4. Check the ASCII game popup and DevExpress component maintenance scripts remain unchanged.

Static checks cannot establish actual DevExpress sizing in the browser; compilation and UI validation are left to the Windows environment.
