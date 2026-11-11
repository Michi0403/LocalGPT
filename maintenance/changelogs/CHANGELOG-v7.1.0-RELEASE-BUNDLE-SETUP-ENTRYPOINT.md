# LocalGPT 7.1.0 — release-bundle setup executable path correction

**App version:** 7.1.0 (from 7.0.9)  
**Setup version:** 5.0.2 (from 5.0.1)

## Incident reproduced from the supplied Windows release log

All win-x64, win-x86 and win-arm64 application/setup publish steps succeeded, including `winx64.zip` and `setupwinx64.zip`. The final release-bundle stage nevertheless failed because it looked for `artifacts/release/setupwinx64/LocalGPTInstallerConsole.exe`. That historical publish-profile directory is *not* populated when `Publish-Runtime` overrides `PublishDir` to `artifacts/release/staging/<version>/win-x64/setup`.

## Repair

- Retrieve the upload-ready `LocalGPTInstallerConsole.exe` from **the same release-scoped win-x64 setup publish directory** used to generate `setupwinx64.zip`.
- Reject a missing staged setup executable or missing x64 setup ZIP **before moving any large release ZIP/PDF artifacts** to the upload-ready bundle.
- Do not consider an existing checksum-valid Windows x64 bundle complete unless it also contains `LocalGPTInstallerConsole.exe` and `setupwinx64.zip`.
- Remove the now-unused profile-directory resolver helpers that mistakenly referenced the obsolete directory.
- Bump the app and installer project versions according to their independent version policies.

## Scope and behavior preserved

No application feature, installer `Program.cs`, model/Ollama/learning-base operation, install destination, native macOS/Linux packaging, native startup script, or platform publish profile was changed. The portable ZIP root folder conventions remain `winx64`, `setupwinx64`, `linuxx64`, `setuplinuxx64`, `macosx64` and `setupmacosx64` (and corresponding arm/x86 RIDs).

## Verification status

Source assertions, relevant project XML parsing, full ZIP inventory, and release-script source inspection were run in the packaging environment. Windows PowerShell, macOS/Linux PowerShell, .NET builds, generated binary/ZIP execution, and real installations are **not** available for confirmation here. The supplied 7.0.9 build log demonstrates the original failure, but a successful 7.1.0 release build remains to be confirmed by the maintainer's Windows/macOS/Linux runs.
