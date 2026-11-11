# LocalGPT 7.1.0 source validation

The supplied 7.0.9 log confirms successful application and installer compiles and publishes for Windows x64/x86/ARM64, then failure in release-bundle assembly due to a stale setup executable path. In 7.1.0 the standalone setup executable path now matches the actual win-x64 release-scoped setup publish directory; the script checks it before moving archive artifacts.

Available checks: source-contract inspection, project XML parsing, unchanged-file audit, ZIP CRC/integrity. The actual .NET 10 Release build, PowerShell gates, Windows/macOS/Linux packaging, and real install/update/start operations **were not executed** here and still require testing. The 7.0.9 synthetic installer archive tests are historical and are not substitutes for actual operating-system installation.

See `maintenance/changelogs/CHANGELOG-v7.1.0-RELEASE-BUNDLE-SETUP-ENTRYPOINT.md`.
