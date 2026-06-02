# Blazor Bootstrap DevExpress Design Guide

Use this guide when LocalGPT or the AI Council generates .NET 8-10 Blazor UI with
DevExpress components and Bootstrap v5 layout.

## Source Baseline

This guide is grounded in these official sources:

- DevExpress Blazor component documentation:
  https://docs.devexpress.com/Blazor/400725/blazor-components
- DevExpress Blazor get-started documentation:
  https://docs.devexpress.com/Blazor/401057/get-started
- DevExpress Blazor Template Kit documentation:
  https://docs.devexpress.com/Blazor/405308/get-started/template-kit
- DevExpress Blazor icon documentation:
  https://docs.devexpress.com/Blazor/401749/styling-and-themes/icons
- Bootstrap v5 grid documentation:
  https://getbootstrap.com/docs/5.3/layout/grid/

Treat these as source-backed guidance for generation. When package versions,
theme names, or API signatures are uncertain, call `/__diag/devexpress` and mark
unknown details as `Needs verification`.

## Mental Model

Use Bootstrap for macro layout and spacing. Use DevExpress for application
controls and business interaction.

Good generated Blazor pages usually combine:

- Bootstrap containers, rows, columns, gaps, flex utilities, spacing utilities,
  responsive breakpoints, and visibility utilities for page structure.
- DevExpress components for data grids, forms, editors, menus, toolbars,
  schedulers, charts, upload, reporting viewers, dialogs, tabs, splitters, and
  AI chat.
- ASP.NET Core backend services for data access, document/report generation,
  native commands, artifact downloads, and EF/SQLite persistence.

Do not replace DevExpress controls with plain HTML controls when the page needs
grid editing, forms, menus, tabular data, upload, report/document UI, AI chat, or
status dashboards. Do not use DevExpress controls only as decoration around an
otherwise fake page.

## Bootstrap v5 Layout Rules

Bootstrap's grid is mobile-first and based on containers, rows, columns,
responsive tiers, gutters, and a twelve-column system. For generation:

- Start with `.container-fluid` for full application workspaces and `.container`
  for narrow forms or documentation pages.
- Use `.row g-3` or `.row g-4` for responsive page sections with consistent
  gutters.
- Use columns such as `.col-12`, `.col-md-6`, `.col-xl-4`, and `.col-xxl-3` to
  create predictable mobile-to-desktop layouts.
- Use `.d-flex`, `.align-items-center`, `.justify-content-between`, `.gap-2`,
  `.flex-wrap`, `.mb-3`, `.p-3`, and `.text-muted` for simple layout polish.
- Keep dense operational pages scannable. Use compact headings, strong labels,
  table/grid surfaces, and small help text instead of oversized hero sections.
- Avoid page sections that are only decorative cards. Cards are appropriate for
  repeated items, focused tools, status summaries, and modal-like surfaces.

## DevExpress Component Selection

Use the component family that matches the workflow:

- Data tables and editable admin screens: `DxGrid`, data columns, command
  columns, toolbar actions, paging, filtering, and layout save/restore when
  useful.
- Forms and settings: `DxFormLayout`, `DxTextBox`, `DxMemo`, `DxComboBox`,
  `DxSpinEdit`, `DxCheckBox`, validation, and save/cancel actions.
- Actions: `DxButton`, `DxDropDownButton`, `DxSplitButton`, `DxToolbar`, and
  icons where a symbol is clearer than a text-only command.
- Navigation: `DxMenu`, `DxTreeView`, `DxAccordion`, `DxTabs`, `DxDrawer`, or a
  Bootstrap nav shell with DevExpress-compatible icons.
- Async or long work: `DxLoadingPanel`, progress/status text, cancellation where
  possible, and safe download links after backend work completes.
- Visual analysis: DevExpress charts, pivot/grid, dashboard, scheduler, maps, or
  reports only when the route has real data and a backend owner.
- Documents and files: keep Office, report, PDF, RichEdit, upload, and export
  generation in ASP.NET Core services, then show status and download links in
  Blazor.
- AI chat: use `DxAIChat` with explicit model/provider selection, streaming when
  available, saved memory, visible model notes, and tool routes rather than
  arbitrary self-expansion.

Generated pages should include real Razor structure: `@page` for routable pages,
appropriate render mode, dependency injection, markup, and `@code` with typed
state or service calls. Do not generate a C# class that merely returns Razor-like
strings unless the user asked for a text generator.

## Template Starting Points

Use official project templates as starting points for generated solutions:

- DevExpress Blazor Template Kit for a DevExpress-themed Blazor shell, Bootstrap
  stylesheet options, Open Iconic resources, ready pages, layout examples, and
  component demos.
- `dotnet new blazor` or official Microsoft sample shapes for the base Blazor
  Web App or Blazor WebAssembly structure.
- LocalGPT/TacosPortalOpen patterns for this repository: Interactive Server in a
  WebView2-friendly app, DevExpress UI, backend services, EF/SQLite persistence,
  diagnostics, and artifact downloads.

Before generating a whole project, write a short archetype contract that states
the target: LocalGPT feature app, AI host control plane, Blazor WASM client, ASP.NET
Core backend, Minecraft datapack, Fabric mod, NeoForge mod, or Paper plugin.
Different archetypes must produce visibly different navigation, pages, services,
and docs.

## DevExpress Theme And Bootstrap Use

DevExpress Blazor themes can be used with Bootstrap-based styling. For generated
apps:

- Link the chosen DevExpress theme CSS before local `app.css`.
- Keep local CSS small and semantic. Prefer Bootstrap utility classes for
  spacing and alignment before writing custom CSS.
- Use DevExpress sizing/theme settings consistently across a page.
- Check static asset loading when the UI appears unstyled. Missing
  `_content/DevExpress.Blazor.*` or app `.styles.css` assets is a packaging
  issue, not a design issue.
- Avoid one-hue visual systems. Use a restrained neutral base, one primary
  action color, clear status colors, and enough contrast for grids/forms.

## Navigation SVG Icon Contract

When the council generates navigation icons, include two SVG styles per concept:

- `line`: outline icon, no fill, `stroke="currentColor"`, round caps/joins,
  readable at 16px and 24px.
- `solid`: filled or duotone icon, `fill="currentColor"` plus optional opacity
  layers, still simple at 16px and 24px.

Each icon must:

- Use a stable square `viewBox`, usually `0 0 24 24`.
- Include a short `<title>` for standalone files.
- Avoid embedded text, gradients, shadows, or decorative blobs.
- Be aligned to the same visual weight across the navigation set.
- Use `currentColor` so DevExpress, Bootstrap, hover, active, disabled, and theme
  states can color it with CSS.
- Be wired through DevExpress `IconUrl` or `IconCssClass` when used in
  DevExpress navigation components, or through an `<img aria-hidden="true">`
  inside Bootstrap navigation.

For generated nav, default to line icons and switch to solid icons on hover,
active route, or selected state. Keep labels visible for normal users unless the
layout is an intentionally compact icon rail with tooltips.

## UX Expectations

Generated DevExpress/Bootstrap pages should feel like useful application
screens:

- Put the actual tool or dashboard in the first viewport.
- Provide concise help text or tooltips next to technical choices.
- Show empty, loading, error, and success states.
- Use DevExpress grids/forms for editable data.
- Show download links through backend routes for generated files.
- Keep advanced settings visible but grouped, not hidden behind mystery states.
- Save user-affecting defaults and presets in EF/SQLite, while appsettings keeps
  bootstrap and logging configuration.
- When a request has important unresolved architecture choices, generate a
  runtime decision poll and stop before coding. Do not treat Blazor/DevExpress
  as the default for every app; offer it only when the user picked it, the target
  repository already uses it, or the product shape clearly benefits from it.
  Poll options should cover target platform/runtime, language/framework, UI
  stack if any, solution split, data model style, security, deployment, artifact
  expectations, and reference-app fidelity.
- When the user asks to recode or clone a goal application, preserve its
  recognizable navigation, first-screen workflow, API/settings surface, model
  catalog/state, and user tasks. If Blazor/DevExpress is the chosen stack,
  DevExpress components should recreate the app's information architecture, not
  replace it with a generic dashboard.
- Make the selected AI/model/provider visibly verifiable before sending. If a
  diagnostic URL requests a model, lock the runtime chat session to that model
  and surface the lock in the UI/status text so frontend tests cannot silently
  hit a different configured model.
- For local inference, show runtime status before the first model token arrives.
  Keep this separate from model-thinking blocks so users can distinguish
  LocalGPT/Ollama transport progress from the AI's actual reasoning.
- Keep frontend smoke prompts deliberately small for large local models. Verify
  DXAiChat selection, poll injection, send behavior, streaming status, and
  answer rendering with capped context/output before asking the model to review
  a full source tree.

If a generated Blazor/DevExpress result lacks routing, navigation, an index
page, real DevExpress components, or a project-specific information
architecture, reject it and ask the council to regenerate from the archetype
contract. If the target is not Blazor/DevExpress, judge it against the selected
stack instead.
