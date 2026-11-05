# LocalGPT 6.0.6 — macOS release preflight, Git source assets, text-service ownership

Based on the complete, unmodified LocalGPT 6.0.5 source archive, retaining
all application functionality, license notices, PDF packaging, and existing
mandatory MSBuild gates.

- Fixed root `.gitignore`'s unanchored `images/` pattern, which Git demonstrably
  applied to `src/LocalGPT/wwwroot/images/`. `/images/` now matches only the
  repository-root scratch directory. Existing image source files remain in the
  complete source ZIP; no working-tree changes are silently committed.
- Run the existing text-service and static-asset guards before expensive
  release work through the normal `Build-Release.ps1`, while retaining their
  existing MSBuild invocations for Debug and Release.
- Keep the baseline-free text-service ownership architecture check; add four
  direct-operation probes so a damaged scanner cannot silently pass. The
  undefined `$known` variable failure reported from 6.0.4 was already fixed
  in 6.0.5, and its initialized zero-exemption set is retained here.
- Print each required missing authored source asset by repository-relative
  path, and print the missing CSS selector when the Council evidence popup
  layout contract fails. Both conditions still throw; validation is not
  weakened, redirected or disabled.
- Add the source-asset and ownership gate ordering to the build maintenance
  documentation and its UML sequence.
- Advance only the LocalGPT application project version to 6.0.6.

No PublisherStudio files, Council operation semantics, model integrations,
DevExpress UI, browser scripts or release-packaging helper code are changed.
