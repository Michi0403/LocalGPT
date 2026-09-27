# LocalGPT 5.1.5

LocalGPT 5.1.5 addresses the post-build startup failure reported against 5.1.4 and adds the same bounded DevExpress popup behavior now used by the PublisherStudio repair.

The optional dedicated MCP listener is preflighted before Kestrel receives it. If Windows refuses a loopback-only MCP port, LocalGPT keeps the primary loopback host alive and exposes MCP on its configured path through that endpoint instead. Remote-capable MCP configurations are not silently weakened; an unavailable dedicated socket disables MCP for that run rather than taking down the application. The primary LocalGPT app/installer endpoint remains unchanged.

DevExpress modal dialogs are also viewport-bounded, with oversized popup bodies scrolling internally rather than bleeding beyond their visible modal surface. Dropdown/listbox portals are intentionally excluded.

The version advances from 5.1.4 to 5.1.5. InteractiveServer directives and the existing DevExpress/Razor ownership model are unchanged. No .NET build/publish and no GitHub/online access were used for this source-only handoff.

See `CHANGELOG-v5.1.5-STARTUP-POPUP-VIEWPORT-RESILIENCE.md` and `VALIDATION-v5.1.5-source.md`.
