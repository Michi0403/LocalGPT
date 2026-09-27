# LocalGPT 5.1.5 — startup and DxPopup viewport resilience

## Release gate

- [x] Existing InteractiveServer render-mode directives are unchanged from 5.1.4.
- [x] The primary desktop/installer web endpoint remains the authoritative loopback endpoint.
- [x] Optional MCP listener failure is fault-contained and no longer tears down the web host when a safe loopback fallback exists.
- [x] DevExpress popup content is bounded by the browser viewport and oversized bodies can scroll internally.
- [x] DevExpress dropdown/listbox popup portals are excluded from the modal-body overflow rule.
- [x] Required DevExpress component-retention and Razor-layout ownership contracts remain in place.
- [x] The popup viewport contract is covered by the maintained Razor source audit.
- [x] No serialized application data, database schema, public wire protocol, or user-owned configuration format was changed.

## Fixed

### Startup resilience for the optional MCP listener

The 5.1.4 runtime could compile successfully and still abort during host startup when Windows rejected the optional dedicated MCP socket. The listener is now preflighted before it is added to Kestrel:

- a loopback-only MCP configuration falls back to the already-authoritative LocalGPT loopback web endpoint and exposes the configured MCP path there;
- a configuration that permits remote MCP clients is not silently weakened to loopback semantics; when its dedicated socket cannot be admitted, MCP is disabled for that run and LocalGPT continues starting;
- runtime MCP status fields are updated with the effective listener state so diagnostics do not report a dedicated endpoint that was suppressed.

This keeps MCP optional. It does not change the LocalGPT application/installer port contract and does not mask failures of the primary LocalGPT endpoint itself.

### DevExpress popup viewport ownership

A shared CSS contract now constrains real `DxPopup` modal dialogs to the browser viewport. The DevExpress modal body is the overflow owner, with `min-width/min-height: 0`, bounded dimensions, internal scrolling, contained overscroll and a stable scrollbar gutter. This prevents large editor content from escaping the visible modal surface on short or narrow viewports.

The rule is deliberately scoped to DevExpress modal cells. Dropdown/listbox portal cells retain their existing DevExpress sizing and stacking behavior.

## Maintenance guard

`build/audit_razor_maintenance_contract.py` now requires the `DEVEXPRESS_POPUP_VIEWPORT_CONTRACT` marker and its essential viewport/overflow declarations. `Assert-DevExpressComponentRetention.ps1` also requires that audit capability to remain present.

## Version

5.1.4 -> 5.1.5.
