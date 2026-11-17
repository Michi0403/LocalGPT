# LocalGPT 7.4.9 — client-first WASIX compiler (source candidate)

- Adapt the supplied MeowReal browser compiler into a circuit-independent LocalGPT workbench, reachable from the existing Chat sandbox panel. Existing DevExpress controls and backend execution providers are retained.
- Pin the Wasmer SDK/Clang package identity in a serializable frontend runtime descriptor; offer C89–C17 and C++98–C++20 compiler flags.
- Explicit offline provisioning of administrator-reviewed local SDK assets and Clang WebC with digest checks and an install receipt. No browser CDN imports or runtime registry downloads. No host interpreter fallback.
- Scope COOP, COEP, content-security policy and permissions restrictions to the browser-workbench route; require cross-origin isolation and a secure context.
- Compile and execute through a dedicated worker with separate explicit user actions, virtual project directory, validated WASM, local downloads, bounded presented receipts, cancellation and timeout. Source never travels through the server circuit.
- Add an explicit browser export of small WASI Preview 1 command modules as SHA-256-bound Node guest source, with no preopened directories or environment. Reject unsupported imports; requires independent review and the existing Hyperlight Node run path. Guest compatibility remains untested.
- Extend the existing session sandbox PowerShell source contract; document actual client/guest boundaries and smoke acceptance.
- No schema changes, no modifications to MeowReal or PublisherStudio, no modifications to Hyperlight guest images or host plugin executor. The browser adapter is not yet an AI-callable or backend plugin provider.
- Pending: real SDK/WebC provisioning, validation of old SDK WebC loading ABI and compiler package/standard compatibility, browser and PowerShell/.NET tests, verified guest WASM execution, project/session integration and runtime-plugin dispatch. No claims of guest or browser runtime success.
