# LocalGPT 2.4.2 — UI, saved-session and in-app documentation repair

- Keeps the existing GitHub Pages/DocFX/PDF build and snapshot pipeline unchanged.
- Restores the documentation viewer to the interactive Help component boundary so HTML, PDF, API and status views open in-app again.
- Removes the in-app viewer's dependency on native-dialog JavaScript synchronization; the viewer is now a Blazor-owned fixed overlay with browser-tab fallback.
- Makes the first-start panel follow the active LocalGPT theme rather than forcing a dark surface in Office White.
- Makes Chat configuration content-sized with bounded viewport scrolling instead of reserving a mostly empty full-height modal.
- Keeps configuration action menus in document flow when opened so controls no longer cover neighbouring fields.
- Exposes all saved chats explicitly and adds Recall selected chat alongside Recall latest.
- Restores a saved chat to its matching provider session when available; legacy rows fall back to the current/first provider session and remain continuable.
