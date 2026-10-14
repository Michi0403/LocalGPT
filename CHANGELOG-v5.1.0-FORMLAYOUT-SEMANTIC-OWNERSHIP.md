# LocalGPT 5.1.0 — FormLayout semantic ownership and maintenance repair

- Replaced the mechanically generated component-level and former-section `DxStackLayout` wrappers with `DxFormLayout` ownership while preserving existing component content and DevExpress controls.
- Added one containment-only `.razor-component-boundary` div around normal component roots so CSS/size bleed can be isolated without transferring layout ownership back to native HTML.
- Kept only the intentional StackLayout paths in `LocalGptFormLayoutSurface`: the user-selectable Stack layout and legacy inner vertical/horizontal Stack compatibility.
- Strengthened the Razor maintenance architecture guard so `DxFormLayout` is the maintained default for editors, scanners, settings, form fields and ordinary component surfaces.
- The guard now rejects generic StackLayout component/section wrappers, requires FormLayout ownership around editor controls, and requires an explicit documented reason for non-Form primary layouts.
- Added a Grid-to-Stack architectural check so redundant Grid/Stack layering is flattened unless the Stack is a genuinely separate one-dimensional subgroup with a nearby reason marker.
- Hardened `Assert-DevExpressComponentRetention.ps1` so accidental removal of the FormLayout-first semantic guard, boundary rule, Stack anti-shortcut checks or Grid-to-Stack intent checks fails independently of the main Razor audit.
- Preserved the existing bans on `<section>`, `<dialog>`, native/manual modal ownership, and replacing DevExpress controls with native Razor controls.
- Repaired `ThemeSwitcher.ButtonCss` and `GetButtonCssForRender()` after the prior generated render-helper conversion created invalid C#.
- Repaired the component-safety registration guard to validate the real singleton/interface mapping in `Program.ServiceRegistration.cs`, retaining one shared bounded component-activity service rather than weakening the rule.
- Refreshed Razor XML documentation for generated render helpers and retained component-local logger/notification/activity ownership.
- Preserved existing InteractiveServer render boundaries and DevExpress 25.2.10.
