# LocalGPT 6.8.8 — Windows installer cleanup and SQLite recovery preservation

## Why this revision exists

LocalGPT **6.4.1** is the last user-confirmed canonical Windows runtime layout:
`%LOCALAPPDATA%\LocalGPT\winx64\LocalGPT.exe`. In 6.8.7 the installer already
normalized release ZIP wrappers, but it *moved* obsolete generated directories to
`runtime\recovered-installer-payloads` instead of actually deleting them. When the
canonical application executable was missing, the launch resolver still scanned
recursively and could start an arbitrary older versioned copy. Independently,
SQLite recovery could treat a stale `.recover` marker as sufficient reason to
replace an otherwise healthy database, and on failed backup moves it could delete
original database files.

## Changes

- Prepare and validate both application and setup ZIPs before modifying either
  installed payload. Activate them at `winx64` and `setupwinx64` with same-volume
  directory moves; on failure attempt to restore both previous payloads. An
  incomplete rollback is a hard error and retains its evidence for repair.
- Never run an automatic recursive `LocalGPT.exe` search to start an obsolete
  release. An explicitly supplied `--localgpt-exe` path remains supported.
- After both canonical launch files have been verified, delete only exact
  three-part generated `LocalGPT-<version>-win-x64` and
  `LocalGPT-Setup-<version>-win-x64` directories, including previous recovery
  quarantine entries. Skip links and folders that contain known user-data
  directories, SQLite or security key material; skip unknown nonempty folders
  without any application payload marker. Failure to delete a locked folder is
  logged and does not invalidate the newly installed application.
- For SQLite startup or on-demand recovery, a `.recover` marker is **not** proof
  of corruption. Verify the current live DB with `PRAGMA quick_check` again.
  Healthy files are retained, inconclusive checks do not trigger reset, and a
  confirmed corrupt file set is moved into a **uniquely named**, timestamped
  backup directory together with available WAL/SHM sidecars.
- If a database/sidecar move fails, roll back previous moves and refuse a new
  empty database. No fallback `File.Delete(sourcePath)` or overwriting a previous
  backup is permitted. A verified corruption recovery is logged at `Critical`.
- Update mandatory PowerShell source guards to reject old installer paths and
  the former destructive SQLite fallback.
- Version bump: LocalGPT `6.8.7 → 6.8.8`; installer console `4.9.9 → 5.0.0`
  following the single-digit minor/patch rollover convention.

## Boundaries and unverified behavior

This change does **not** alter Council execution, message persistence, rejoin
logic, the deterministic game engine, user SQLite schema, migration definitions,
or PublisherStudio. A running Windows process can prevent a directory rename or
cleanup; this is reported as a failure or a cleanup warning, never concealed.

The C# code and mandatory PowerShell guards were inspected offline. A Windows
install/rollback test, a PowerShell execution, a .NET compile and a live SQLite
recovery test were **not** executed here. These must pass before describing this
as a verified working release. A database smaller than previously observed may
also have unrelated causes; no claim of forensic causality is made.
