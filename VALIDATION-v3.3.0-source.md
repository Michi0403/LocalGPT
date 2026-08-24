# LocalGPT 3.3.0 source validation

This archive is **source-only and not compiled** in the preparation environment. No `dotnet`, MSBuild, restore, publish, EF command, application launch, or GitHub/online repository access was used. The user's normal Windows build remains the authoritative compilation/runtime gate.

## Theme and UI review

The defect was traced to two interacting theme boundaries rather than to individual broken themes:

- LocalGPT intentionally lets the shell theme and DevExpress component theme be selected independently. Shell buttons such as Menu and Back to Home were still inheriting DevExpress foreground variables, so opposite-luminance combinations could produce nearly invisible text/icons.
- `MainLayout.razor.css` used a broad `::deep .icon` selector with a DevExpress-derived `background-color`. That selector reached navigation-menu SVG icons too. Since those icons were background images, the inherited background became the white/theme-colored rectangle visible behind some icons.

3.3.0 scopes shell icon foreground rules to shell-owned buttons, moves navigation icons to masks, and gives application chrome stable LocalGPT-owned foreground/background tokens.

## Contrast checks

A source-side WCAG contrast calculation was run against all **21 maintained shell palettes** declared in `localgpt-theme-contract.css`:

- neutral body text on raised shell surfaces: minimum **4.86:1**;
- white text on the darkened primary action mix used by Chat: minimum **4.60:1**;
- navigation foreground `#f8fafc` on the stable navigation background `#151d29`: **16.19:1**.

This does not pretend to render DevExpress itself, but the controls fixed in this release no longer obtain their foreground from the independently selected DevExpress component theme.

## Final source-only validation gate

The final 3.3.0 worktree completed these checks successfully before packaging:

- Application architecture policy: passed.
- Async continuation policy: **258 source files**, **2,973 await tokens**, **2,618 `ConfigureAwait(false)`**, **135 renderer-affine `ConfigureAwait(true)`**, **215 explicitly configured async disposals**, and **5 configured async streams**.
- Service resilience: **2,155 service methods** own diagnostics boundaries; 29 iterator/yield methods and 3 direct Program/Startup methods remain under their separate policies.
- Code-generation / DXFunction wiring audit: passed.
- Provider-qualified Council feature audit: **282 checks passed**.
- Provider stream-repetition policy audit: passed.
- Chat ASCII-console lifecycle audit: **17 checks passed**.
- C# XML documentation coverage/quality: **9,627 declarations across 631 maintained source files** passed.
- Razor XML documentation coverage/quality: **45 components and 776 direct `@code` declarations** passed.
- Localization key parity: **6 catalogs with 2,012 keys each**.
- Changed CSS parse validation: MainLayout, NavMenu, Drawer, Chat, Projects, and the LocalGPT theme contract parsed with **0 CSS syntax errors**.
- Project files (`LocalGPT`, InstallerConsole, WebView wrapper) parsed as valid XML.
- New `menu.svg`, `projects.svg`, and `project-maintenance.svg` parsed as valid SVG XML.
- Release-specific LocalGPT 3.3.0 audit: **52 checks passed**, including version rollover, DevExpress retention, shell/component theme separation, removal of the generic icon leak, navigation masking, project icon assets, Chat semantic action styling, 21-palette contrast analysis, and retained InteractiveServer boundaries.

## Version and scope

- LocalGPT, InstallerConsole, and WebView wrapper: **3.3.0**.
- DevExpress: **25.2.9** unchanged.
- PublisherStudio: **2.9.7** unchanged.
- 3.2.9 database/knowledge/RegEx/lifecycle behavior was not modified by this theme-focused release.

## Environment limitation

PowerShell/Roslyn and .NET-backed repository build gates were not executed because this handoff intentionally does not use a .NET SDK. No claim of a compile/build is made. The archive is prepared for the user's normal build and runtime verification.
