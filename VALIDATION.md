# LocalGPT 4.3.6 source validation

Source-only validation; no .NET command was run. The 4.3.6 check is deliberately narrow: documentation pointer decorations are viewport-contained, scroll-neutral, synchronized across authored/embedded/Pages assets, and protected by the new build guard. Chromium geometry regression checks confirmed that adding many paw-trail elements no longer changes document scroll dimensions. See `VALIDATION-v4.3.6-source.md`.
