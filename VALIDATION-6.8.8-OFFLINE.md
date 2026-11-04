# LocalGPT 6.8.8 — Offline validation and release acceptance

## Source validation available in the editing environment

- Original 6.8.7 archive and the independently supplied 6.4.1 baseline inspected.
- Canonical 6.4.1 Windows runtime path confirmed from supplied application logs.
- Mandatory `build/Assert-InstallerLayout.ps1` updated with transaction-order and
  cleanup checks and an invocation of `Assert-DatabaseRecoverySafety.ps1`.
- SQLite health service checked for marker re-verification, all-file move and
  rollback, unique backups, and absence of destructive fallback.
- Confirm both ZIP archive entries, XML project versions and ZIP CRC after packaging.

## Required Windows acceptance test (not performed here)

1. **Backup** `%LOCALAPPDATA%\LocalGPT` with the program stopped before testing.
   Especially preserve the main `.db`, `-wal` and `-shm` files and all Council
   logs, keys, certificates, user projects and existing recovery backups.
2. Run Windows PowerShell 5.1 guards from a clean extraction:
   `powershell -NoProfile -ExecutionPolicy Bypass -File build\Assert-InstallerLayout.ps1`.
3. Build Debug and Release and execute both setup and application ZIPs through
   the real installer. Confirm the process identity, shortcuts and working
   directory target the stable `%LOCALAPPDATA%\LocalGPT\winx64` path; setup
   scripts live under `setupwinx64`.
4. Test a deliberately malformed application ZIP **and** a malformed setup ZIP
   (missing required root marker). Confirm neither changes the installed files.
   Test a locked destination to confirm rollback rather than half-installation.
5. With **synthetic fixture directories only**, place known versioned wrappers,
   incomplete wrappers, a junction, a folder containing a `.db`, and an unrelated
   project folder under a disposable LocalGPT root. Confirm only validated
   obsolete installer debris is deleted, including prior quarantine entries.
6. With **synthetic SQLite databases only**, verify that healthy data with a
   stale `.recover` marker remains byte-identical; inconclusive/locked probe
   preserves all files; corrupt database+WAL/SHM is backed up before recreation;
   a blocked backup move aborts and rolls back; a verified backup is logged.
7. Restart LocalGPT and confirm previous chat sessions and Council evidence
   remain visible. A fully reliable rejoin integration test is **not** established
   by these installer/database changes alone.

## Limitations

This environment lacks `dotnet` and Windows PowerShell. Source-only validation
cannot demonstrate that runtime behavior or the release build passes. No user
installation or database was modified.
