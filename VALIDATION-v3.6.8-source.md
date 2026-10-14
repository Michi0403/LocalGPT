# LocalGPT 3.6.8 source validation

Validation is source-only in this environment. No `dotnet`, MSBuild, GitHub access, package publish, Apple signing, Apple notarization, or application launch was performed. PowerShell itself is also unavailable in this validation container, so the macOS release logic is checked by source audit and delimiter/string-aware lexical validation rather than execution.

Checked statically:

- all LocalGPT application projects report 3.6.8 and retain the single-digit minor/patch version policy;
- `Build-Release.ps1` invokes the new macOS trust preflight automatically and exposes only an explicit `-AllowUnsignedMacPackages` escape hatch;
- the preflight defaults a standard public build to `MACOS_REQUIRE_NOTARIZATION=1`, discovers both Developer ID identities, selects `future2-notary` when no alternate credential path is configured, and validates that keychain profile with `notarytool history` before the expensive release work;
- native packaging still signs every discovered Mach-O payload with hardened runtime and secure timestamping, now signs nested code containers deepest-first, verifies the nested signatures, and signs/verifies the enclosing app last;
- generated DMGs are Developer ID Application-signed before notarization and receive signature/staple/Gatekeeper-open validation;
- generated PKGs remain Developer ID Installer-signed, notarized, stapled and `pkgutil`-validated, with a visible final local Gatekeeper installer assessment;
- Apple Developer ID is explicitly kept away from Windows PE output because Windows requires a separate Authenticode trust identity;
- Apple-Silicon/Rosetta architecture hardening, dynamic macOS endpoint startup, the 3.6.4 background-service continuation fix, and 15 `@rendermode InteractiveServer` directives remain present;
- the async-continuation source audit passes;
- no repository-local `src/**/bin` or `src/**/obj` build state is included.

The authoritative end-to-end verification remains a real macOS `pwsh Build-Release.ps1` run on the Developer ID/notary-configured machine. Its expected trust chain is: Developer ID nested/app signing -> signed DMG or Installer-signed PKG -> Apple notarization Accepted -> staple validation -> local signature/Gatekeeper diagnostics.
