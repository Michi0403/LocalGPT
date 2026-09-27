# LocalGPT 5.1.1 — Razor template-context repair

- Explicitly names every maintained `DxFormLayoutItem` template context so nested DevExpress child-content templates do not collide on Razor's implicit `context` parameter (`RZ9999`).
- Keeps the FormLayout-first UI architecture intact; no DevExpress control or workflow was removed to make the compiler quiet.
- Fixes `ProjectMaintenance.PermissionBoolean` and `PermissionIcon` as instance helpers because the component diagnostics contract requires their failures to use the injected typed logger.
- Extends the Razor maintenance audit with build-breaking `RAZORUI0010/RAZORUI0011` checks for missing/reused FormLayout template context names.
- Retains the containment-only root div, FormLayout-first semantic ownership, `<section>`/`<dialog>` bans, typed component logging, render-mode rules, and DevExpress component-retention contract.
