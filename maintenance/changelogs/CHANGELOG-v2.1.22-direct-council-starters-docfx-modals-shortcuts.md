# LocalGPT 2.1.22

## Fixed

- Normalized compiler XML member collections in the DocFX fallback so PowerShell StrictMode never calls `.Count` on a scalar XML node.
- Direct **Open in Chat** routes now force a fresh Chat load, select AI Council, apply the requested team and model preset, clear the Council conversation and submit the maintained starter prompt.
- Form-heavy DevExpress dialogs now use the available viewport while the already-correct Chat configuration surface keeps its dedicated layout.
- Installer shortcut provisioning removes legacy or duplicate `LocalGPT*.lnk` and `LocalGPT*.url` entries only after resolving that they target the active AppData LocalGPT installation.

## Added

- Stable prompt keys and Council-team ownership for maintained pre-prompts.
- Direct Council starter buttons inside Chat for the selected team.
- Maintained starter prompts for general project work, adaptive benchmarks, GameDirector, C#, PowerShell, Java, Minecraft and ESP32/Arduino wiring.
- Explicit installer hint that quick-start cards create and submit a fresh Council session rather than merely navigating.

## Additional corrections in the packaged debug candidate

- Kept the Chat route canonical as `/chat` and closes restored configuration/details workspaces before a direct Council starter is submitted, preventing a transparent full-page click blocker.
- Made open Chat configuration workspaces explicitly opaque and interactive.
- Made Test Lab remote-import selections side-by-side, fully labelled, and explicit about preview approval.
- Kept documentation generation enabled during normal builds; the compiler-XML fallback now avoids scalar `.Count` access under PowerShell StrictMode and remains publishable through DocFX.
