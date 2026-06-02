# Blazor DevExpress AI Generation Guide

Use this compact guide when LocalGPT or the AI Council generates .NET 10 Blazor UI for this repository.

For layout, Bootstrap v5 utility usage, DevExpress template starting points, and
navigation SVG icon style contracts, also read
`docs/BLAZOR_BOOTSTRAP_DEVEXPRESS_DESIGN.md`.

## Core Rule

For Blazor UI requests, generate a real `.razor` component or page. Do not answer with a C# class that only builds markup or Markdown strings unless the user explicitly asks for a helper class.

Generated UI artifacts should normally include:

- `@page` when the artifact is a page.
- `@rendermode InteractiveServer` or `@rendermode @(new InteractiveServerRenderMode(prerender: true/false))` when the page needs server-side interactivity.
- `@inject` services for backend work.
- `@code` with small state, async event handlers, and model records/classes.
- Existing LocalGPT layout classes such as `main-container`, `top-container`, and existing notification/toast patterns when relevant.

## Known LocalGPT DevExpress Components

Prefer components already present in this codebase unless `/__diag/devexpress` proves another component exists and fits:

- `DxButton`
- `DxCheckBox`
- `DxComboBox`
- `DxTextBox`
- `DxMemo`
- `DxSpinEdit`
- `DxGrid`
- `DxGridDataColumn`
- `DxFormLayout`
- `DxFormLayoutGroup`
- `DxFormLayoutItem`
- `DxLoadingPanel`
- `DxMenu`
- `DxGridLayout`
- `DxAIChat`

For editable data surfaces, prefer `DxGrid` with clear columns and backend services. For forms, prefer `DxFormLayout`. For long generated text, logs, or prompts, prefer `DxMemo`.

## TacosPortalOpen Pattern Notes

The user-provided TacosPortalOpen sample is useful architecture guidance for Michi0403-style Blazor work.

Relevant patterns observed in the local zip:

- `Routes.razor` uses `AuthorizeRouteView` and redirects unauthorized users.
- Pages use `@rendermode InteractiveServer` or `new InteractiveServerRenderMode(prerender: true/false)`.
- Protected pages wrap content in `AuthorizeView`.
- Admin pages use `DxGrid`, `DxGridCommandColumn`, `DxGridDataColumn`, `EditFormTemplate`, and `DxFormLayout`.
- `DxLoadingPanel` wraps long-running data fetches.
- `ToastWrapper` plus notification services report user-visible errors.

Treat TacosPortalOpen as a pattern source, not as code to copy blindly into LocalGPT.

## Backend Boundary

Keep privileged or expensive operations in ASP.NET Core backend services:

- Native commands.
- File generation.
- DevExpress Office/report/PDF export.
- SQLite writes.
- Minecraft workspace generation.

The Blazor frontend should trigger backend services, show progress/status, and render safe download links.

## Generated Artifact Expectations

For implementation-request chats, LocalGPT should produce sandbox artifacts first:

- A `.razor` file for UI requests.
- A compileable `.cs` support file when useful.
- A `.dll` only for support code that can compile in isolation.

Generated features must not integrate themselves into the real project. Ask the user before adding routes, nav menu entries, services, migrations, or project references.

## Missing Source Behavior

If the council needs current .NET, DevExpress, GitHub, or package information and LocalGPT diagnostics do not provide it:

- Ask for or request the specific source.
- Use `/__diag/devexpress`, `/__diag/dxaichat-functions`, `/__diag/build-debug-files`, and SQLite knowledge before large context dumps.
- Put unknown claims under `Needs verification`.
- Do not invent DevExpress APIs or blame the user for missing context.
