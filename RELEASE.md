# LocalGPT 3.3.8

LocalGPT 3.3.8 cleans up the remaining DocFX site warnings and makes the long PDF phase observable instead of appearing frozen.

The 3.3.7 macOS release run confirmed that the isolated `System.Formats.Nrbf` documentation dependency probe works: DocFX metadata completed without warnings or errors. The remaining warnings came from links that referenced generated outputs before DocFX had produced them. The API link now points to its authored Markdown source, and the temporary PDF validation stub is included as a DocFX resource until the real handbook PDF replaces it.

The DocFX process wrapper now streams output live instead of buffering it until process exit. The PDF command runs with verbose diagnostics and announces that a four-digit page set can legitimately take several minutes. The cross-platform Node.js bootstrap, DevExpress license preflight, documentation source preflight, PowerShell parser guard, and installer/platform work remain intact.

See `CHANGELOG-v3.3.8-DOCFX-LINK-PROGRESS-REPAIR.md` and `VALIDATION-v3.3.8-source.md`.
