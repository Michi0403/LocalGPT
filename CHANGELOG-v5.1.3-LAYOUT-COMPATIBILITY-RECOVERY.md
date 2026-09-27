# LocalGPT 5.1.3 — DevExpress layout compatibility recovery

- Restored the established pre-wrapper component geometry without rolling back DevExpress Blazor ownership. The maintenance-only `razor-component-boundary`, `razor-component-layout-owner`, generated FormLayout row/item/control shells, and migrated `razor-section-layout-owner` shells are now box-neutral in the loaded `wwwroot/css/site.css` stylesheet.
- Kept genuine DevExpress layouts and controls intact. The compatibility rule affects only the mechanical maintenance wrappers introduced around existing component roots and former section regions, so existing grid/flex placement, height propagation, overflow, and sizing remain owned by the original component surfaces.
- Extended the Razor maintenance architecture audit with `RAZORUI0012`. It now fails when the compatibility marker/rules disappear or when the application shell stops loading `css/site.css`, preventing the same visual regression from being reintroduced while all DevExpress-count checks still appear green.
- Extended the DevExpress retention guard so weakening or removing the new compatibility contract is itself build-breaking.
- Preserved the existing InteractiveServer boundaries. The explicit `@rendermode` set remains unchanged from 5.1.2 and the supplied 4.7.2/3.3.0 baselines; no extra nested circuit boundaries were introduced.
- Preserved the existing DevExpress function-call presentation in Chat, including the `LocalGptFormLayoutSurface` owner and the `DxFormLayout` argument/result regions inside `FunctionCallInfoContentTemplate`.
- Advanced LocalGPT to 5.1.3 and refreshed the maintained JavaScript/module cache-buster references that used the product version.
