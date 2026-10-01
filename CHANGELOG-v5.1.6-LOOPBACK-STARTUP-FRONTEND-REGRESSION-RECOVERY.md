# LocalGPT 5.1.6 — loopback startup and frontend regression recovery

## Release gate

- [x] The reported Windows socket-access failure is handled at the authoritative LocalGPT loopback endpoint as well as the optional MCP endpoint.
- [x] `AddressAlreadyInUse` still preserves the requested endpoint so the desktop wrapper can reuse an already-running LocalGPT instance rather than launching a duplicate host.
- [x] A Windows `AccessDenied` bind on the requested application port can recover to an available loopback port instead of aborting Kestrel startup.
- [x] The installer/browser handoff reads the started process' `runtime/server.json` and follows the effective loopback URL after a recovered port selection.
- [x] The shared DevExpress popup contract keeps all six LocalGPT `DxPopup` surfaces inside the browser viewport and makes oversized modal bodies internally scrollable.
- [x] Previously repaired Chat microphone module dispatch, InteractiveServer-safe theme teardown, language selection, selected-workbench text contrast, and configurable function/message layout surfaces remain present.
- [x] All 20 explicit `@rendermode` directives are unchanged from 5.1.5.
- [x] `src/LocalGPT/Services` is unchanged from 5.1.5.
- [x] No database schema, serialized application data, public wire protocol, or user-owned configuration format was changed.

## Fixed

### Primary LocalGPT listener recovery

The supplied 5.1.4 runtime log showed two separate Windows socket-access failures: the optional 1-Wire TCP listener reported WinSock 10013, and Kestrel then aborted host startup with the same socket error. Version 5.1.5 preflighted the optional MCP listener, but it did not preflight the authoritative application listener itself. That left a real gap when Windows denied the requested application port.

5.1.6 preflights the requested loopback application port before Kestrel receives it. The behavior is deliberately narrow:

- a successful bind keeps the requested port;
- `AddressAlreadyInUse` keeps the requested port so the existing WinUI health-probe/reuse path remains authoritative;
- `AccessDenied` selects an available loopback replacement, excluding the active OneWire, discovery, remote-web and dedicated MCP ports;
- other socket failures are not silently hidden behind a new port.

The effective port is published through the existing `Program.Port`, `Program.BaseUrl`, and `runtime/server.json` runtime contract.

### Installer/browser endpoint handoff

The installer console previously slept for two seconds and always opened the originally requested URL. That is incorrect if LocalGPT has to recover from a Windows-reserved/denied port.

The installer now performs a bounded wait for `runtime/server.json`, accepts it only when the runtime snapshot belongs to the process it just started and is fresh for that launch, validates that the URL is the LocalGPT loopback HTTP endpoint, and opens that effective URL. If no matching runtime snapshot appears, the historical requested URL remains the fallback.

### LocalGPT popup viewport containment

The shared `DEVEXPRESS_POPUP_VIEWPORT_CONTRACT` was strengthened with normal-viewport fallbacks plus dynamic-viewport limits and shrinkable workbench roots. Upload review, documentation, local-path browsing, runtime-plugin editing, the ASCII console, and TestLab source editing can no longer force the DevExpress modal itself beyond the viewport merely because an inner editor/grid has a large intrinsic size. The DevExpress modal body remains the fallback overflow/scroll owner.

### Retained frontend repairs

This pass explicitly re-audited the earlier LocalGPT evidence instead of assuming those reports were already closed:

- Chat microphone calls the exported ES-module `start` function from `localgpt-microphone.js`;
- `ThemeJsChangeDispatcher` does not issue a browser-side module-disposal call during InteractiveServer teardown;
- the shell language selector remains a DevExpress `DxComboBox`;
- selected configuration/workbench buttons explicitly retain readable foreground/text-fill colors;
- `FunctionCallInfoContentTemplate` and message content remain owned by `LocalGptFormLayoutSurface`, with Content/Grid/Stack/Tabs/Splitter/Carousel/FormLayout support.

## Version

5.1.5 -> 5.1.6.
