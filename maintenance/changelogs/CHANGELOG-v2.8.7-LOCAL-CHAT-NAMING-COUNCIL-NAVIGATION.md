# Changelog — LocalGPT 2.8.7

- Renamed the visible local chat module from the incorrect `ChatGPT`/`LocalChatGPT` wording to **Local Chat**.
- Added maintained Local Chat translations for German, English, Spanish, French, Japanese, and Ukrainian.
- Removed obsolete `LocalChatGPT` localization aliases so runtime fallback cannot reintroduce the wrong product name.
- Updated the home welcome text, chat card, chat page title/setup text, and main navigation to use Local Chat terminology.
- Removed the standalone **AI Council** home tile and `/model-council` main-menu item while retaining the page and route for direct testing.
- Preserved Council Teams & Workflows and all Council execution paths through `/chat`.
- Carried the confirmed Windows compile fix `using System.Text.RegularExpressions;` into `CouncilTextService.cs`.
- Removed the `CS8602` warning in the Council failure-memory path by flow-narrowing the request before persistence.
- Bumped LocalGPT, LocalGPTWebviewWrapper, and LocalGPTInstallerConsole from 2.8.6 to 2.8.7.
- Kept wire protocol version 2.1.1 unchanged.
- Added a source-only 2.8.7 regression audit for Local Chat naming, hidden legacy Council navigation, localization parity, compile repair presence, nullable warning repair, version policy, and render-mode count.
