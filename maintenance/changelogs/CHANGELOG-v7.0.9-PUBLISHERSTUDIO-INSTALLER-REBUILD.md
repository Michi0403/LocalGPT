# LocalGPT 7.0.9 — PublisherStudio-derived installer workflow revision

**Application version:** 7.0.9 (retained as specifically requested)
**Setup version:** 5.0.1 (increased from 5.0.0)

The existing application and its modern functions are unchanged. The installer deployment path is reconstructed around PublisherStudio 4.7.6's working behavior, retaining LocalGPT-only Ollama, model and learning-base operations.

- Windows installed setup self-detaches into a temporary executable before trying to replace its own files.
- Application and setup ZIPs are acquired by exact platform asset names; explicit paired offline ZIP paths are supported; direct latest-release download falls back to the GitHub release API.
- Both archives are validated before touching the installed application, and the installed application (not similarly named test copies) is stopped on Windows before overlay extraction.
- Canonical runtime folders and executable names are derived for all seven supported runtimes. A post-install version check uses application assembly metadata; setup version markers are checked, plus the Windows installed setup executable's file version.
- Restores `Install.cmd`, `Update.cmd`, `Start.cmd` as non-destructive ordinary entry points. The original `LocalGPT-Install-Start.cmd` no longer incorrectly forces whole-root deletion. Explicit `Install-Force-Delete-Start.cmd` remains opt-in.
- Normal updates keep the existing LocalGPT AppData root, databases, models, settings, sources and logs. They do not make extra version-specific directories or automatically delete legacy leftovers.
- Windows release ZIP layout remains stable `winx64` / `setupwinx64` etc. macOS/Linux now also provide stable-folder portable app/setup ZIPs alongside their pre-existing native DMG/PKG/TAR.GZ/DEB/RPM outputs. WSL importer forwards those portable archives.
- Separate obsolete installer-layout build guard stays removed.

Validation: project XML, WSL shell syntax, seven-RID synthetic ZIP overlay/preservation checks, source-diff inventory, archive integrity. No Windows/macOS/Linux installation, .NET compile, PowerShell execution or actual native packaging was performed in this environment.

The release build has source-fingerprint validation and can be forced to rebuild same-version artifact caches using `-ForceRebuildArtifacts`. Do not publish a release as verified until real install/update/start tests have passed on all target systems.
