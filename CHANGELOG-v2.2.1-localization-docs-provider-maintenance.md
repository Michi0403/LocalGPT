# LocalGPT 2.2.1

- Restores German and user-catalog localization with a native culture-selection form, an explicit query-string request culture, and the persistent culture cookie.
- Re-applies translated text after Blazor renderer updates and adds maintained German coverage for the current Chat, ASCII-game, session-tool, and documentation controls.
- Replaces the clipped AI-provider/Council combo with a native, accessible selector.
- Keeps the ASCII game guide mounted while moves are processed so the game viewport no longer jumps, and safely centers frames beside the guide.
- Preserves recursive installed-documentation discovery, now including nested versioned PDFs, and opens the PDF inline in the current application view.
- Preserves the responsive Test Lab workspaces and removes forbidden static service helpers from `DocumentationCatalogService`.
- Links the generated GitHub Pages API reference through `api/index.html` instead of the nonexistent `api/index.md`.
- Retains the compact Kawaii DocFX HTML/PDF presentation and separately versioned 1-Wire protocol package.

## Follow-up runtime corrections

- The language selector now navigates directly to a service-built culture endpoint URL. This bypasses Blazor form enhancement and always performs the full request required to apply the request-culture query and persistent cookie. The browser localizer now walks every eligible text node instead of relying on a limited element-name list.
- The ASCII game guide keeps a stable status area and retains button focus while moves are processed. A matching left gutter keeps the ASCII viewport centered against the complete stage whenever the guide is visible.
- PDF resolution now searches every trusted generated-documentation root plus bounded application/content-root fallbacks, so an installed `wwwroot/help-docs/LocalGPT-2.2.1.pdf` remains available even when another HTML root was selected.
