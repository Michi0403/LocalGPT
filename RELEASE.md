# LocalGPT 2.8.7 Local Chat naming and legacy Council navigation cleanup

LocalGPT 2.8.7 corrects the user-facing Local Chat identity in every maintained localization. The local chat module is no longer labeled or translated as ChatGPT. The home page and navigation now present it as Local Chat (or the corresponding maintained translation), while the LocalGPT product name and the historical/technical ChatGPT references documented elsewhere remain untouched.

The obsolete standalone AI Council entry point is no longer advertised in the home-card grid or main navigation. The `/model-council` page and Council implementation remain present for direct testing and compatibility; active Council workflows continue through `/chat`, Council Teams, 1-Wire, and PublisherStudio integrations.

This release also carries forward the authoritative Windows compile repair `using System.Text.RegularExpressions;` in `CouncilTextService.cs` and removes the nullable warning in the Council failure-memory path without changing its behavior.

## Versions

- LocalGPT: 2.8.7
- LocalGPTWebviewWrapper: 2.8.7
- LocalGPTInstallerConsole: 2.8.7
- LocalGPT wire protocol: 2.1.1 (unchanged)

## Compatibility notes

- The intentionally observed Japanese startup/default-language quirk is not changed in this release.
- The `/model-council` route remains reachable directly for testing.
- Existing Council Teams, Council runtime services, Council logs, `/chat` Council sessions, and PublisherStudio 1-Wire integration are retained.
- No GitHub access, `dotnet`, MSBuild, Visual Studio build, publish, or package restore was used to validate this source archive. The Windows build remains authoritative.
