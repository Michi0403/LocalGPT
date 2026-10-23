# LocalGPT 6.2.4 — PublisherStudio media and panel Council teams

## Baseline

Complete LocalGPT 6.2.3 source; archive-aware learning and its 64-bit cursor fixes are unchanged. Architecture audits, existing DX-function approvals and client disposal behavior are untouched.

## New maintained Council seeds

- `publisher-image`: inspect installed providers and `localai.image.generate` before creating an actual image; advise native PublisherStudio editing when it suffices.
- `publisher-video`: inspect `localai.video.generate` readiness and PublisherStudio's native video editing/conversion path. Require real artifact/receipt evidence for any reported generated video.
- `publisher-audio`: inspect installed audio generation options and prefer native Audio Studio for simple editing; do not pretend LocalGPT's image/video DX functions implement speech or audio generation.
- `publisher-panel`: inspect authorized Panel/Div organic module functions; create reviewable composition and OpenSCAD/3D source; obey active document identity, user approval and standalone export constraints.

All four teams are **editable seed definitions**, not automatically installed diffusion models, new executable engines or permission grants. Model lookup is explicitly delegated to existing `initial.setup.status` and separately human-approved `initial.setup.canirun.recommendations`. Attribute score evidence; do not embed user-expressed model rankings or send hardware information without approval. The `localai.image.generate` / `localai.video.generate` DX functions retain their own explicit human authorization.

## Limitations

- A Council can discuss, request approved DX function invocations, and report returned artifacts, but end-to-end PublisherStudio binary-media transfer/insertion is not implemented in this version.
- New seeds are not guaranteed to be selected by existing customized team stores until their normal seed reconciliation/availability is confirmed at runtime.
- No .NET compilation or cross-application live runtime integration was possible here.

See `VALIDATION-v6.2.4-source.md`.
