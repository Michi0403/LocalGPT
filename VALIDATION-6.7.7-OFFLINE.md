# LocalGPT 6.7.7 — Offline source validation

Source baseline: `LocalGPT676.zip` (LocalGPT 6.7.6).

## Checks performed

- Full ZIP CRC integrity and per-file comparison to baseline.
- All baseline files retained; no baseline files deleted.
- The 28 maintained JavaScript file hashes match the build manifest (with line-ending normalization).
- `node --check` on `localgpt-reconnect.js` succeeds.
- Both image function schema JSON values contain `negativePrompt` with `default: ""`, but not in `required`.
- Checked source-level C# syntax boundaries and PowerShell source guard contract strings.
- Cross-platform RID list unchanged.

## Verification limitations

The environment has no .NET SDK, Windows PowerShell or PowerShell 7. C# compilation, PowerShell execution, EF startup, DevExpress and full live Blazor recovery were **not** tested. The original .NET-to-JavaScript Blazor pending stream fault is **not** proven fixed.
