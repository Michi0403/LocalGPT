# LocalGPT 3.3.4

LocalGPT 3.3.4 completes the cross-platform PowerShell documentation prerequisite path exposed by the macOS release build.

`Build-Release.ps1` and `Build-LocalDevelopment.ps1` now run an early prerequisite bootstrap that keeps the DevExpress license preflight, checks `dotnet`, and resolves Node.js before the long build. A compatible Node.js 20-22 installation is reused when available; otherwise LocalGPT downloads portable Node.js 22.23.2 for the current Windows, macOS or Linux architecture into a per-user cache, verifies the archive against the official Node.js SHA-256 manifest, and exports it to DocFX/Playwright without requiring an administrator/root install.

The documentation pipeline uses the same shared resolver. Large macOS/Linux manuals now bypass the direct Chromium print path above 1000 pages and use the DocFX PDF plug-in directly, avoiding the noisy Edge `Printing failed` path observed on the 1132-page LocalGPT manual. The 3.3.3 DocFX assembly-reference closure repair remains included, so unresolved framework references are still repaired/retried and release documentation still requires zero unresolved assembly references.

See `CHANGELOG-v3.3.4-CROSS-PLATFORM-DOCUMENTATION-RUNTIME.md` and `VALIDATION-v3.3.4-source.md`.
