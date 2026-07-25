# Component safety and bounded UI awareness

LocalGPT components keep their existing features and may use different visual compositions, but they share one operational safety contract.

## Required top-of-file dependencies

Every maintained `.razor` component except `_Imports.razor` declares these directives in the top directive/using section:

```razor
@inject ILogger<ComponentName> Logger
@inject INotificationService Notifier
@inject IComponentActivityService ComponentActivity
```

Do not replace these directives with component parameters or `[Inject]` properties. Other component-specific services remain normal top-level `@inject` directives.

## Failure boundaries

`Routes.razor` wraps routed UI in `SafeErrorBoundary`. `MainLayout.razor` adds a second boundary around page content. `App.razor` owns the shared `ComponentSafetyToasts` provider. An unhandled render, lifecycle, or event failure is logged with technical details, shown to the human as a sanitized notification, and summarized in bounded operational memory.

The routing boundary is recovered after navigation and the layout boundary is keyed to the active URI. A failure in one page therefore cannot leave unrelated pages permanently hidden behind a stale error boundary.

A boundary is the final safeguard, not a replacement for local recovery. A component that can recover from an expected failure should keep its recovery behavior and use its normal logger and notifier.

## Handled operations

Reusable UI-operation wrappers such as `RunUiActionAsync` follow this sequence:

1. Record a concise operation-start event.
2. Execute the existing feature without changing its visual contract.
3. Record completion, cancellation, or failure.
4. Log technical failure details without prompt, response, uploaded-file, generated-source, credential, or secret content.
5. Notify the human with a useful but sanitized message.
6. Restore busy state in `finally`.

Core workflow methods must not swallow a failure and allow callers to announce success. They either return a meaningful explicit result or log and rethrow so the component-level recovery path runs. Cancellation is preserved as cancellation rather than converted into a generic error.

## Notification-to-memory bridge

Components call `INotificationService`; they do not call the DevExpress toast service directly. `NotificationService` normalizes notification text, omits message bodies from logs, and records only that a sanitized notification of a given severity was presented. This gives LocalGPT short-term awareness of user-visible state without copying notification content into model context.

## Bounded short-term context

`ComponentActivityService` stores at most 128 process-local entries. Entries may contain:

- route paths without query strings or fragments;
- component and operation names;
- success, warning, cancellation, or failure state;
- short fixed summaries that do not contain user or model content.

Entries never contain prompts, assistant responses, uploaded data, generated source, database row values, credentials, tokens, secrets, full file contents, or full exception details. `AiContextBootstrapService` can include a small recent briefing as operational context only. It is never authority and is not durable memory.


The read-only `/__diag/component-activity` route exposes the same sanitized bounded entries and briefing for owner-side verification. It does not expose the omitted content categories.

## Validation

`build/Assert-ComponentSafety.ps1` verifies:

- all maintained Razor components retain the three top-level directives;
- safety services are not moved to `[Inject]` properties;
- the shared error boundary and toast provider remain wired;
- notification events remain connected to bounded activity memory;
- reusable UI-operation wrappers retain logging, notification, and activity reporting;
- the activity service remains registered and included in AI bootstrap context.

This guard runs before Roslyn parsing and full Debug and Release builds. A source package is not a verified release until those compiler builds pass for the exact packaged fingerprint.
