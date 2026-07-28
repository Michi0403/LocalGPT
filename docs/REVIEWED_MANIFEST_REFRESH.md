# Reviewed protection-manifest refresh

Use `build/Update-ReviewedProtectionManifest.ps1` after deliberately changing a file covered by the protected-repository or maintained-JavaScript inventories.

For an exact review:

```powershell
.\build\Update-ReviewedProtectionManifest.ps1 -ReviewedFiles 'LocalGPTWebviewWrapper/LocalGPT/Services/AiDiscoveryService.cs'
```

For ordinary non-safeguard source or JavaScript changes, the script can enumerate the current reviewed delta:

```powershell
.\build\Update-ReviewedProtectionManifest.ps1 -ReviewCurrentChanges
```

The command lists every changed path and uses PowerShell's high-impact confirmation before writing. Sensitive build/safeguard changes require the explicit `-ReviewedFiles` form. It runs security-rule preservation, 1-Wire, runtime-value ownership, JavaScript diagnostics, and protected-repository checks. If post-write validation fails, both manifests are restored.

The script never refreshes `security-rules-final19.sha256`, never approves a file listed by that security manifest, and never rewrites removal-only architecture baselines. To add a new protected architecture file, first add it to `$expectedFiles` in `Assert-ProtectedRepositoryFiles.ps1`, then explicitly review both the assertion and the new file.
