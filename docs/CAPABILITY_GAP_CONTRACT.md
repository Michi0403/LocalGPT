# Capability Gap Contract

LocalGPT models should not answer "I cannot do that" as a dead end when a user
asks for code, datapacks, modpacks, whole solutions, AI-host features, or other
engineering artifacts. If the request is concrete, produce the safest useful
downloadable milestone and record what still needs investigation. If the request
is blocked by missing information, missing LocalGPT functions, or missing source
knowledge, report that gap in a structured way.

Use this contract for DXAiChat, the AI Council, and any generated self-review.

## When To Emit A Gap

Emit a capability gap when any of these are true:

- The user says LocalGPT or the council failed, refused, lacked knowledge, or
  produced the wrong artifact.
- The model needs official docs, local source, package versions, build output, or
  runtime diagnostics before it can be confident.
- The task requires a LocalGPT function that is not available yet.
- The model can create a partial artifact but cannot honestly claim full
  validation.
- A generated AI-host, Blazor, DevExpress, Minecraft, Python.NET, WebView2, EF,
  or provider-compatible feature needs more source grounding.

## Required Visible Section

In the normal answer, include a concise `Capability gap report` section with:

- missing capability or knowledge
- requested language, framework, runtime, and version
- requested domain knowledge
- local sources to inspect first
- external official sources needed
- missing LocalGPT function or UI/backend feature
- safe next artifact path

## Machine-Readable Block

Also append this block when a gap should be saved:

```text
<localgpt-capability-gap>
user-request-summary: short summary of what the user asked
missing-capability: what LocalGPT/council/model lacks
owning-area: DXAiChat | AI Council | Minecraft Builder | .NET generation | AI host | setup | frontend test | database | other
target-deliverable: datapack zip | .cs/.razor/.dll | whole solution zip | AI host lab | report | setup fix | other
requested-languages: C#, Razor, Java, mcfunction, Python, JavaScript, SQL, etc.
requested-frameworks: .NET 10, ASP.NET Core, Blazor, DevExpress, EF Core, WebView2, Fabric, Paper, NeoForge, etc.
requested-versions: concrete versions or "needs verification"
requested-domain-knowledge: model host API, Minecraft pack format, DevExpress component, EF relationship mapping, etc.
local-knowledge-sources: DXAiFunctions, SQLite tables, local docs, local source roots, generated artifacts, build logs
external-knowledge-sources: official docs, official GitHub repos, package docs, version manifests
missing-localgpt-functions: route/page/service/tool that would make the request easier
safe-workflow: diagnostic first, sandbox artifact, build/test, WebView2/Test Lab check, user approval
artifact-plan: concrete downloadable artifact or why a poll is required first
investigation-status: Needs verification | SourceBacked | UserVerified | ModelSuggested
next-localgpt-improvement: one precise feature or knowledge import to add
confidence: 0-100
tags: capability-gap; generation; source-request
</localgpt-capability-gap>
```

The block is not a refusal. It is a request for LocalGPT to improve. If the user
already asked for a concrete artifact, still generate the best safe downloadable
artifact and put unresolved work under `Needs verification`.

## Source Workflow

Prefer local sources first:

- `/__diag/dxaichat-functions`
- `/__diag/knowledge`
- `/__diag/sqlite/tables`
- `/__diag/logs`
- `/__diag/devexpress`
- `/__diag/blazor-devexpress-guidance`
- `/__diag/dotnet-sample-curriculum`
- `/__diag/ai-host-rebuild-guidance`
- `/__diag/frontend-test-guidance`
- `/__diag/learn-base/import`
- generated artifacts and build logs

Use external sources when local knowledge is absent, stale, or version-sensitive.
External knowledge should prefer official Microsoft, DevExpress, Minecraft,
Fabric, Paper, NeoForge, Gradle, Java/JDK, HuggingFace, provider API, or official
GitHub repository sources. Treat external browsing/downloading as a separate
workflow that may require user approval and stronger safety checks.

## Faster Downloadable Results

For common Michi0403 test prompts, prefer these outputs:

- Minecraft datapack/modpack: downloadable zip through `/__artifacts/council/`,
  current Java 26.x pack format/version check, build-local validation, and
  install/debug commands.
- .NET/Blazor/DevExpress feature: real `.razor` page plus service/model code and
  optional compiled `.dll`, then whole solution zip when requested.
- AI host/control plane: .NET/ASP.NET Core/DevExpress Blazor solution zip with
  left navigation, model catalog, chat/API console, settings, logs, downloads,
  provider-compatible route stubs, and honest native-inference boundary.
- Frontend verification: Test Lab route output, WebView2/Selenium plan, or
  screenshot/snapshot evidence before claiming the frontend was tested.

Progress made by Michi0403 + Codex + LocalGPT is approved project knowledge unless
the user later corrects it. Preserve those corrections as stronger current facts.
