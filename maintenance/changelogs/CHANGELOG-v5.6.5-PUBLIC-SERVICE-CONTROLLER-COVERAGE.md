# LocalGPT 5.6.5 — Public Service Controller Coverage and Compiler Namespace Repair

Source baseline: **LocalGPT 5.6.4**, not an older release. No PublisherStudio changes.

## Build repair
- Qualify `LocalGPT.BusinessObjects.ConfigurationRoot` in the Python task orchestrator constructor to eliminate CS0104 against `Microsoft.Extensions.Configuration.ConfigurationRoot`.

## Complete the existing public-service catalog API path
- Register `IPublicServiceMethodDirectoryService` and its service-owned, purpose-grouped inventory. Return stable keys, JSON schemas, policy and non-executed public property *metadata*.
- Implement `GET /api/dxai/public-service`, `GET .../groups`, `GET .../{catalogKey}`, `POST .../invoke`, and approved `PUT .../exposure`.
- Existing discovered, catalogued methods are invoked via the original DI-bound `PublicServiceMethodInvoker`; no unrestricted user-selected CLR type/method reflection route was created.
- User-approved local service invocation does **not** require AI/1-Wire exposure. AI/peer access still has its separately persisted catalog policy. The new grouped exposure action requires approval and matches an **exact service class**; individual existing policy fields are preserved.
- Additional discovery-only `localgpt.public_service.discover` DXAiFunction lets Loc query the catalog without performing actions; the existing `localgpt.public_service.invoke` function remains centralized and approval-gated.

## Purpose-specific controllers
- Runtime plugins: persisted definition list, maintained language templates, save, build/load, delete; changes synchronize the existing function catalog.
- Council teams: list, templates, lookup, save, delete, reset by maintained template.
- Knowledge and reusable regex: list links, bounded recognition preview, save and delete curated links.
- Existing DX function policy updates and regex curator reviews now require the repository's `HumanApprovalRequired` filter on these HTTP routes.

## Known scope and behavior
- The generic API exposes methods which the maintained function-catalog introspection can accurately represent and DI can resolve, not literally every CLR member in the program. Unsupported contracts still need a purpose-specific adapter. Property getters/setters are not executed by the discovery endpoint.
- A service-family policy update persists individual method policies; cancellation may produce partial completion, not an atomic database transaction. Resubmitting the identical request is idempotent for already-updated entries.
- An installed user's explicit approval is distinct from allowing AI or remote clients to invoke an operation. Approvals and validation are retained; no broad default activation of dangerous AI tools is made.
- The NuGet restore warning concerning `LocalGPTWebviewWrapper` in the user's Visual Studio log is independent of CS0104 and is not claimed fixed.

## Validation
- No `dotnet` build, RID publish, app execution or GitHub use. Static checks include the new controller guard, qualified ConfigurationRoot, Python/project orchestration, maintained cross-platform check, DevExpress validation and previously passed architecture/async/service checks. A later all-in-one re-run reached its time limit and is not counted as passing.
