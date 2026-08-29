# LocalGPT 3.5.2

LocalGPT 3.5.2 is the **Release Packaging Pipeline Contract** maintenance release.

The application itself already compiled in the supplied Windows release run; the failure was later, when PowerShell captured `dotnet` progress output together with the release-packaging executable path and passed the resulting multi-value object to the Linux packaging script. This release makes package/tool helper return values single-purpose and validated before native packaging begins.

The intended installer matrix remains: Windows uses the one-click installer console, Linux uses native package formats without a setup console, and macOS uses `.app`/DMG packaging without a setup console. The LocalGPT-owned release-packaging tool remains shared with PublisherStudio alongside the 1-Wire package. See `CHANGELOG-v3.5.2-RELEASE-PACKAGING-PIPELINE-CONTRACT.md` and `VALIDATION-v3.5.2-source.md`.
