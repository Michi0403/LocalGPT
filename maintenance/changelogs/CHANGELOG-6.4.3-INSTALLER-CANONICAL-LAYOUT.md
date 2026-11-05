# LocalGPT 6.4.3 — Windows installer canonical layout repair

## Confirmed regression

The installer downloaded the intended `winx64.zip` and `setupwinx64.zip` release assets, but
extracted the archive's release-version wrapper into `%LOCALAPPDATA%/LocalGPT` rather than
normalizing the files into the stable `winx64/` and `setupwinx64/` folders. Launcher discovery
then recursively chose a versioned `Start.cmd`, hiding the misplaced deployment. The setup
log therefore reported a successful download of 6.4.1 while the original `winx64/` could
still contain stale application binaries.

## Repairs

- Stages and validates each release ZIP independently before updating the runtime.
- Accepts versioned release wrappers, canonical wrappers and legacy flat ZIPs; requires
  the correct top-level executable or launcher in each archive before extraction.
- Copies only the normalized payload into `%LOCALAPPDATA%/LocalGPT/winx64` and
  `%LOCALAPPDATA%/LocalGPT/setupwinx64`.
- Rejects ZIP path traversal, platform-rooted paths, raw backslashes and symlink entries.
- Verifies both canonical executable and Start.cmd before reporting successful installation.
- Stops installation on errors instead of proceeding to shortcut creation and browser launch.
- Refuses recursive shortcut-target recovery; only canonical `setupwinx64/*.cmd` is valid.
- Restricts installer `--force-delete` to the two runtime binary folders. **No database,
  MFA trust store, certificates, runtime settings, learning base, chats or logs are
  deleted during a force-refresh install.** The explicit uninstall path is separate.
- Adds mandatory `build/Assert-InstallerLayout.ps1` checks to LocalGPT and installer
  project builds and the release preflight. No existing maintenance rule is disabled.
- Raises application version 6.4.2 to 6.4.3 and setup console version 4.9.6 to 4.9.7.

## Acceptance criteria on Windows

1. Build and run the installer with a version-wrapped local release archive.
2. Verify `%LOCALAPPDATA%/LocalGPT/winx64/LocalGPT.exe` and
   `%LOCALAPPDATA%/LocalGPT/setupwinx64/Start.cmd` are current.
3. Verify Desktop/Start menu Start shortcut directly targets `setupwinx64/Start.cmd`.
4. Verify running LocalGPT reports the new assembly version, not merely the version
   downloaded by the setup utility.
5. Repeat with a legacy flat ZIP and with a malformed package (which must fail closed).
6. Check existing database, credentials and logs remain unchanged.

## Other previously reported issues

Council goal completion, graphical poll continuity, model-role specialization, live
transcript/chat recovery, PublisherStudio 1-Wire trust and frontend error exposure
remain separate unresolved issues. This patch does **not** claim to repair or bypass
those systems without independent source-contract and integration verification.
