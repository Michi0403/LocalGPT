# LocalGPT 5.2.9 — DevExpress UI retention hardening

## Summary

LocalGPT 5.2.9 applies the DevExpress-first regression lessons from the PublisherStudio review to LocalGPT's maintenance contract. No ordinary LocalGPT editor was found using native input/select/textarea/datalist controls in place of DevExpress components; the only maintained native buttons remain the two pre-circuit reconnect/reload actions in `App.razor`, which must work when InteractiveServer event dispatch is unavailable.

## Maintenance changes

- Strengthened `Assert-DevExpressComponentRetention.ps1` to inspect Razor markup only and track DevExpress component minimums, native interactive-control maximums and native `details`/`summary` disclosure debt per component.
- Native `datalist` and `input type="color"` controls are rejected with diagnostics that describe the DevExpress repair pattern rather than permitting a new raw-HTML substitute.
- Existing `details`/`summary` disclosures are explicitly classified as migration debt: their count may decrease, but new disclosure UI should use `DxAccordion` or an appropriate DevExpress menu/flyout.
- Expanded the retention manifest to every maintained Razor file so new components enter the same regression contract instead of escaping the baseline.
- Updated `AGENTS.md` with the same DevExpress-first disclosure policy while preserving the pre-circuit reconnect exception.

## Runtime behavior

This release intentionally avoids invasive replacement of established LocalGPT disclosures. Application/runtime behavior is otherwise unchanged from 5.2.8; the change is a stronger build-maintenance boundary that prevents future native-control regression.
