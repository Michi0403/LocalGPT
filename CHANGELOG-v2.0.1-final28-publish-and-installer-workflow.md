# LocalGPT 2.0.1 final28

- Made every RID application, setup and WinUI wrapper publish self-contained and explicitly multi-file.
- Synchronized release-script artifact folders with all Visual Studio publish profiles, including win-x86 and correctly named macOS profiles.
- Restored no-command setup behavior: double-click now performs a preservation-first install/update, checks Ollama and the Slim model set, provisions shortcuts and starts LocalGPT.
- Restored the extended command-launcher, URL-shortcut and Visual Studio startup-profile set.
- Added publish-configuration and installer-workflow safeguards to direct builds, local development and release builds.
- Existing LocalAppData is deleted only by an explicit destructive uninstall/force command.
- Synchronized installer and root documentation with the active no-command routine and added guards against reintroducing help-only or single-file guidance.
- Updated the stale final19 whitespace-pattern contract so it enforces the database-backed pattern data-service boundary instead of demanding a component/service-owned regex.
