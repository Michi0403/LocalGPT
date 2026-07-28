# LocalGPT 2.0.1 final29

## Installer launcher publish contract

- Restored compatibility between the unchanged final19 1-Wire architecture safeguard and the final28 wildcard launcher deployment.
- Every reviewed LocalGPT command launcher is now also declared explicitly in the installer project with `CopyToOutputDirectory=Always` and `CopyToPublishDirectory=Always`.
- The wildcard deployment remains present so newly reviewed launchers cannot silently disappear from published setup output.
- No installer command, default preservation-first routine, security rule, 1-Wire rule, application data behavior, publish mode, or runtime identifier was changed.
