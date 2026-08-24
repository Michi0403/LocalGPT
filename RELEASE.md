# LocalGPT 3.3.0

LocalGPT 3.3.0 is a theme-contrast and navigation-chrome repair release built on the 3.2.9 database, relationship and lifecycle work.

The main correction is architectural rather than a theme-by-theme patch: LocalGPT's shell and DevExpress component themes are intentionally selectable independently, so application chrome can no longer depend on DevExpress button foreground variables. Menu, Back to Home, drawer header/footer controls and the important Chat actions now consume LocalGPT-owned contrast tokens instead. Navigation icons are rendered as masks, eliminating the white-square/background artifact caused by the old broad `MainLayout ::deep .icon` rule and making black/white source SVGs equally visible.

Projects now has its own `projects.svg` navigation/heading icon and Project Maintenance has a related maintenance variant. Chat primary, neutral, success and danger actions plus runtime send/upload controls also use explicit shell-aware contrast styling.

No persistence, Council, RegEx, DXFunction, provider, 1-Wire or deployment behavior changes in this release. InteractiveServer boundaries remain unchanged. DevExpress remains **25.2.9** and PublisherStudio remains **2.9.7**.

See `CHANGELOG-v3.3.0-THEME-CONTRAST-NAVIGATION-ICON-REPAIR.md` and `VALIDATION-v3.3.0-source.md`.
