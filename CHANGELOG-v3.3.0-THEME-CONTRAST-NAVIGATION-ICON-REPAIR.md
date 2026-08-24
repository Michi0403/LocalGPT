# LocalGPT 3.3.0 — Theme contrast, navigation and icon repair

## Why this release exists

LocalGPT supports independently selectable shell and DevExpress component themes. That flexibility exposed a UI contract bug: a shell control could inherit text/icon colors from the selected DevExpress component theme even though its background came from the shell theme. Opposite-luminance combinations could therefore make otherwise functional buttons appear blank.

A second CSS-scope bug made the problem more visible in the navigation drawer: `MainLayout.razor.css` applied a background color to every descendant `.icon`. Navigation items use SVGs as background images, so the inherited icon background became a white or theme-colored rectangle behind the SVG. The same broad selector also allowed unrelated navigation icons to inherit shell-button sizing.

## Theme-safe application chrome

- Introduces stable LocalGPT navigation tokens in `localgpt-theme-contract.css` for drawer background, foreground, border and hover states.
- The navigation drawer now stays deliberately dark enough for its light foreground instead of assuming every shell primary color is a suitable menu background.
- Main-layout Menu and Back to Home buttons use LocalGPT shell palette tokens instead of relying on DevExpress `Dark`/`Light` text colors.
- Drawer header and footer buttons use the navigation palette, so the independently selected DevExpress component theme cannot make them disappear.
- The broad `MainLayout ::deep .icon` rule is removed. Only LocalGPT shell/navigation buttons receive masked foreground styling.

## Navigation icons

- Converts navigation item icons to CSS masks, so source SVG fill/stroke colors no longer determine visibility.
- Mask foreground comes from the same guaranteed navigation text color; black source icons and white source icons therefore render consistently.
- Adds a new `projects.svg` icon for **Projects**.
- Adds a related `project-maintenance.svg` icon for **Project Maintenance**.
- Adds both new assets to the repository's explicit static-output list.
- The Projects page heading also uses the new project icon as a theme-colored mask.

## Chat button contrast

- Adds LocalGPT-owned semantic action classes for neutral, primary, success and danger `DxButton` actions on the Chat page.
- Primary Chat actions use a darkened shell-primary mix with white text rather than assuming raw primary is dark enough. Static contrast analysis of the maintained shell palettes keeps the lowest primary-action contrast above WCAG AA normal-text contrast.
- Neutral Chat actions use the current shell body/surface pair rather than DevExpress component-theme foregrounds.
- Runtime-tagged AI Chat send/upload controls receive the same theme-safe treatment.
- Native Bootstrap secondary/primary actions inside the Chat workbench are aligned with the shell palette as well.

## Scope

- No database schema, Council, knowledge, RegEx, project persistence, 1-Wire, DXFunction, provider or deployment behavior is changed.
- InteractiveServer render-mode boundaries are unchanged.
- The 3.2.9 database/relationship/lifecycle work is retained intact.
- DevExpress remains 25.2.9.
- PublisherStudio remains 2.9.7 and is unchanged.

## Version

3.2.9 was the final single-digit patch in the 3.2 line. In accordance with the repository release convention, the next LocalGPT release is **3.3.0**, not 3.2.10.
