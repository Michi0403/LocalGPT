# Frontend Design Pattern Library

Use this library when LocalGPT, DXAiChat, the AI Council, or an agent generates
frontends for .NET, Blazor, DevExpress, Bootstrap, or custom component stacks.

This is a compiled pattern library. Models should use the patterns below
directly. They should not tell the user to look at external design feeds, copy
names, copy branding, copy screenshots, or use a third-party gallery as the
runtime source of truth.

The user supplied visual design references to teach LocalGPT common frontend
patterns. Those references were distilled into generic application archetypes,
component mappings, services, layout rules, and accessibility checks.

## Source Baseline

This library is grounded in:

- User-supplied design reference URLs for modern commerce and social app
  concepts, compiled into reusable patterns rather than names or brands.
- Microsoft Windows app design guidelines overview:
  https://learn.microsoft.com/en-us/windows/apps/design/guidelines-overview
- Microsoft Windows color, layout, navigation, typography, usability, and
  writing style guidance:
  https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/color
  https://learn.microsoft.com/en-us/windows/apps/design/layout/
  https://learn.microsoft.com/en-us/windows/apps/design/basics/navigation-basics
  https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/typography
  https://learn.microsoft.com/en-us/windows/apps/design/usability/
  https://learn.microsoft.com/en-us/windows/apps/design/style/writing-style
- Bootstrap v5.3 layout, components, helpers, and utilities:
  https://getbootstrap.com/docs/5.3/layout/grid/
  and https://getbootstrap.com/docs/5.3/utilities/spacing/
- DevExpress Blazor component catalog and adaptivity docs:
  https://docs.devexpress.com/Blazor/400725/blazor-components
  and https://docs.devexpress.com/Blazor/405212/common-concepts/adaptivity
- Microsoft Fluent 2 layout, color, tokens, shape, and accessibility guidance:
  https://fluent2.microsoft.design/layout
  and https://fluent2.microsoft.design/color
- W3C WAI accessibility principles:
  https://www.w3.org/WAI/fundamentals/accessibility-principles/
- Nielsen Norman Group usability heuristics and visual design principles:
  https://www.nngroup.com/articles/ten-usability-heuristics/
  and https://www.nngroup.com/articles/principles-visual-design/

Treat this document as the implementation rulebook. External visual references
are provenance only unless the user explicitly asks to inspect a new one.

## Pattern Extraction Contract

When a user asks for a frontend or goal-app recode:

- Identify the product archetype: commerce, social, SaaS, admin, AI tool,
  media editor, finance, health, education, game companion, or portfolio.
- Identify the primary user task: browse, compare, configure, generate, chat,
  edit, review, approve, buy, publish, moderate, export, or monitor.
- Build information architecture first: first screen, navigation, primary
  entity types, task flow, filters, editor panels, save/download paths, status
  states, and permissions.
- Choose visual density: immersive/visual, balanced product UI, or dense
  operational UI.
- Translate the design into concrete pages, components, CSS tokens, services,
  models, persistence, diagnostics, and artifact routes.
- If the selected stack lacks a ready component, create a project-local Razor
  component with semantic HTML, Bootstrap utilities, scoped CSS, and typed
  parameters.
- Do not invent DevExpress APIs. Use `/__diag/devexpress` when uncertain and
  mark unknown controls or signatures as `Needs verification`.

## Shared Visual System

Generated apps should define a small design system:

- CSS variables: surface, surface-muted, border, text, text-muted, accent,
  accent-contrast, success, warning, danger, info, focus-ring, radius, shadow,
  spacing, and navigation width.
- Type scale: one page title size, one section title size, one body size, one
  caption/help size, and optional compact grid size.
- Shape: 6-8px radius for work apps; larger radius only for consumer/product
  cards where it serves the design.
- Color: neutral base, one primary accent, semantic status colors, and enough
  contrast. Avoid single-hue interfaces unless the domain requires it.
- Icons: use existing icon libraries where available. For navigation SVGs,
  generate line and solid variants with `currentColor`.
- States: loading, empty, error, success, disabled, offline, validation,
  permission-denied, and long-running job status.
- Motion: subtle transitions for drawers, tabs, hover, progress, and upload;
  no animation required for admin/data-heavy screens.

## Windows And Fluent App Design Foundations

Use Microsoft Windows app design guidelines as the baseline for Windows-hosted
Blazor/WebView2 apps and as useful discipline for web apps:

- Color: use color to establish hierarchy and communicate meaning. Keep accent
  color sparse, reserve it for important interactive elements, and ensure light,
  dark, and high-contrast modes can remain readable.
- Commanding: present actions in predictable locations. Put primary actions near
  the task, secondary actions in toolbars/menus, and destructive actions behind
  confirmation or clear affordances.
- Elevation: use depth to clarify layering, not decoration. Apply it to drawers,
  dialogs, floating command bars, popups, and focused panels.
- Geometry: use consistent shape, spacing, and sizing. Keep corners, gutters,
  icon boxes, and control heights stable across a feature area.
- Iconography: use familiar, purposeful icons that communicate concepts quickly.
  Pair unfamiliar icons with text labels or tooltips.
- Layout: organize content with grids, spacing, alignment, and responsive
  breakpoints. Design for multiple window sizes, orientations, and resolutions.
- Materials: in WinUI shells, Mica/Acrylic can add depth and warmth. In Blazor
  web surfaces, translate this into subtle surface tokens and avoid fake glass
  effects that reduce readability.
- Motion: use motion for feedback, focus, and responsive transitions. Avoid
  motion that delays repeated work or hides application state.
- Navigation: prefer consistency, simplicity, and clarity. Use flat top-level
  navigation for fewer peer pages, left navigation/drawer for many top-level
  areas, tabs for related panels/documents, breadcrumbs for deeper hierarchy,
  and list/details for frequent item-detail switching.
- Typography: use consistent type hierarchy to improve readability. Do not scale
  fonts with viewport width. Keep dense panels compact and reserve display type
  for true hero or product surfaces.
- Usability: make controls discoverable, reduce cognitive load, expose status,
  prevent errors, provide recovery paths, and keep interactions accessible.
- Widgets/glanceable surfaces: only add small summary widgets when they expose
  real state or a useful shortcut, such as running jobs, model health, or
  pending approvals.
- Writing: use clear, concise, helpful language. Labels should name actions and
  states directly. Help text should reduce confusion without becoming a manual.

## Bootstrap Role

Use Bootstrap for responsive macro layout and low-level alignment:

- Containers: `.container-fluid` for apps, `.container` for narrow docs/forms.
- Grids: `.row`, `.col-12`, `.col-md-6`, `.col-xl-4`, `.row-cols-*`, and
  `.g-*` gutters for cards, products, dashboards, and galleries.
- Flex: `.d-flex`, `.align-items-center`, `.justify-content-between`,
  `.gap-*`, `.flex-wrap`, `.ms-auto`, and `.position-sticky` for toolbars,
  headers, side panels, and sticky actions.
- Spacing: `.p-*`, `.px-*`, `.py-*`, `.m-*`, `.mb-*`, `.mt-*` for rhythm.
- Helpers: `.ratio`, `.overflow-auto`, `.text-truncate`, `.visually-hidden`,
  `.shadow-sm`, `.rounded-*`, and responsive display utilities.
- Bootstrap components are fine for simple cards, badges, breadcrumbs, navs,
  progress, toasts, modals, placeholders, and spinners when DevExpress is not
  needed.

Bootstrap should frame DevExpress controls rather than fight them.

## DevExpress Blazor Role

Use DevExpress for application-grade interaction:

- AI/chat: `DxAIChat` for conversational surfaces and tool output.
- Data: `DxGrid`, `DxTreeList`, `DxPivotGrid`, filters, summaries, paging,
  selection, editing, detail rows, and export where available.
- Forms: `DxFormLayout`, editors, validation, tab pages, and grouped settings.
- Navigation: `DxDrawer`, `DxMenu`, `DxTreeView`, `DxAccordion`, `DxTabs`,
  `DxToolbar`, and split/drop-down buttons.
- Feedback: `DxLoadingPanel`, progress, popup/dialog/window/message box/toast
  components when supported by the installed package.
- Media/files: `DxUpload` or `DxFileInput`, plus backend services for storage,
  validation, artifact generation, thumbnails, and downloads.
- Visualization: charts, gauges, maps, scheduler, dashboard, reports, PDF,
  RichEdit, and document viewers only when data/service ownership exists.

If DevExpress does not provide the visual shell directly, compose:

- semantic `.razor` components for the shell,
- Bootstrap for layout and responsive behavior,
- DevExpress controls inside the shell for real interaction,
- backend services for data, persistence, generation, downloads, and logs.

## Commerce Pattern

Use for fashion, food, marketplace, subscription, booking, and object-focused
commerce.

Pages:

- Home/catalog
- Product detail
- Cart or saved items
- Checkout
- Profile/orders
- Admin catalog/order management

Components:

- Product hero with image/media, title, price, status, and primary CTA
- Responsive product cards with image ratio, price, tags, favorite action
- Filter drawer/sidebar with category, sort, price, availability, and tags
- Search bar with suggestions
- Product detail tabs for description, specs, reviews, shipping, and history
- Cart rows with quantity stepper and price summary
- Checkout form with validation and order review

DevExpress/Bootstrap mapping:

- `DxGrid` for admin catalog/orders
- `DxFormLayout`, `DxTextBox`, `DxSpinEdit`, `DxComboBox`, `DxCheckBox` for
  product/edit/checkout forms
- `DxUpload` for product images or attachments
- `DxDrawer` or `DxPopup` for filters, cart, quick view, and mobile panels
- `DxTabs` for detail sections
- Bootstrap responsive card grid, image ratio helpers, sticky CTA/actions

Services:

- catalog service, cart service, checkout/order service, profile service,
  media service, inventory/status service, payment adapter placeholder,
  EF/SQLite repository or API client.

## Social And Community Pattern

Use for communities, messaging, activity feeds, profiles, teams, and moderation.

Pages:

- Feed
- Explore/search
- Profile
- Messages
- Notifications
- Create post
- Moderation/admin

Components:

- Feed card with author, media/text, reactions, comments, share/save
- Story/activity rail
- Profile header with avatar, stats, actions, tabs
- Composer with text, upload, preview, visibility, and validation
- Comment drawer or detail panel
- Message list plus conversation pane
- Notification grouping by time/type
- Moderation queue with status and action reasons

DevExpress/Bootstrap mapping:

- `DxTabs` for profile/feed filters
- `DxGrid` for moderation/admin queues
- `DxMemo`, `DxUpload`, `DxButton`, `DxPopup`, `DxDrawer` for composer and
  detail panels
- `DxListBox` or custom Razor list components for conversations and activity
- Optional `DxAIChat` for AI suggestions, moderation drafts, or help

Services:

- feed service, profile service, relationship/follow service, message service,
  notification service, media upload service, moderation service, recommendation
  placeholder, EF/SQLite or API persistence.

## AI Host And Developer Tool Pattern

Use for LocalGPT, model hosts, code generators, API consoles, diagnostics,
artifact browsers, and technical workbenches.

Pages:

- Dashboard
- Chat/API console
- Model catalog
- Downloads/install plans
- Running models/jobs
- Settings
- Logs/diagnostics
- Artifact browser

Components:

- Left navigation drawer/rail with section icons
- Top command toolbar with selected provider/model/project
- Model cards or grid with size, format, status, actions, and warnings
- Chat/API console split view
- Log grid with filters and severity badges
- Job/progress timeline
- Settings form grouped by provider/runtime/safety
- Artifact list with safe HTTP download links

DevExpress/Bootstrap mapping:

- `DxAIChat`, `DxGrid`, `DxFormLayout`, `DxTabs`, `DxDrawer`, `DxToolbar`,
  `DxUpload`, `DxProgressBar`, `DxLoadingPanel`, `DxPopup`
- Bootstrap split panes, sticky headers, compact status cards, responsive
  sidebars, and console/layout utilities

Services:

- provider adapter, model catalog, download plan, settings service, log reader,
  artifact service, health check, job runner, memory service, EF/SQLite
  persistence.

## SaaS Admin And Enterprise Pattern

Use for dashboards, back offices, CRUD apps, DevExpress Web API/XAF-adjacent
frontends, reporting, and operational tools.

Pages:

- Dashboard
- List/grid
- Detail form
- Audit log
- Settings
- Reports/export

Components:

- Compact KPI row
- Filter/search toolbar
- Editable grid with command column
- Detail drawer or side panel
- Master/detail tabs
- Validation summary and save/cancel actions
- Audit timeline/log table
- Export/report action area

DevExpress/Bootstrap mapping:

- `DxGrid`, `DxFormLayout`, `DxToolbar`, `DxPopup`, `DxTabs`, charts, pivot,
  scheduler, reports, and export where installed
- Bootstrap responsive grid, compact cards, utility spacing, sticky command bar

Services:

- CRUD service, validation service, authorization/security service, audit log,
  report/export backend, EF DbContext factory or API client.

## Media Editor And Workbench Pattern

Use for video tools, asset libraries, document tools, report designers, code
workbenches, and generation/export workflows.

Pages:

- Asset library
- Editor
- Preview
- Jobs/queue
- Export/downloads

Components:

- Split pane with list/editor/preview
- Inspector panel
- Timeline or step list
- Upload/drop zone
- Preview surface
- Export format selector
- Job status and logs

DevExpress/Bootstrap mapping:

- `DxSplitter`, `DxUpload`, `DxGrid`, `DxFormLayout`, `DxTabs`,
  `DxProgressBar`, `DxLoadingPanel`, `DxPopup`
- Bootstrap ratio helpers, overflow panels, sticky toolbars, compact inspectors

Services:

- asset store, job runner, native command runner if approved, preview/thumbnail
  service, export artifact service, logs, settings.

## Mobile-To-Desktop Translation

Many modern references are mobile-first. For Blazor desktop/WebView2:

- Preserve task rhythm, not the phone frame.
- Translate bottom tabs into `DxTabs`, a top toolbar, or a left rail/drawer.
- Translate floating mobile CTAs into sticky toolbar actions or inline primary
  buttons near the relevant grid/form/card.
- Translate cards into responsive Bootstrap grids.
- Translate mobile filter sheets into `DxDrawer`, sidebars, or popups.
- Use `DxLayoutBreakpoint` or CSS media queries to switch from stacked mobile
  surfaces to split/grid desktop layouts.
- Keep touch targets comfortable on mobile and keyboard/focus correct on
  desktop.

## Accessibility And Usability Checklist

Every generated frontend should include:

- Clear system status for loading, saving, generating, downloading, and errors.
- User control: cancel, close, safe back paths, and undo/redo where meaningful.
- Consistent labels, platform conventions, route names, and button placement.
- Error prevention: validation, disabled states, confirmation for destructive
  actions, and defaults that avoid data loss.
- Recognition over recall: visible filters, labels, selected model/provider,
  active route, breadcrumbs or page title where needed.
- Minimalism: remove irrelevant controls from the first screen but keep advanced
  options grouped and discoverable.
- Accessibility: text alternatives for icons/images, keyboard access, focus
  order, contrast, semantic headings/lists/forms, and no color-only status.
- Responsiveness: mobile, tablet, desktop, and wide desktop layouts must keep
  text inside containers and avoid overlapping controls.

## Generation Rule

When generating a frontend:

1. Name the archetype and primary user task.
2. Choose pattern sections from this library.
3. Choose Bootstrap, DevExpress, and custom Razor component responsibilities.
4. Name required services, models, persistence, and safe download routes.
5. Generate real files and a buildable artifact when requested.
6. If a material decision is missing, ask a poll and stop for the next turn.
7. If LocalGPT lacks a needed capability, emit a capability gap report.
