# LocalGPT 7.6.9 — strict StringBuilder ownership (intentionally build-breaking)

## Requested maintenance policy

`StringBuilder` is forbidden in **all first-party C# / Razor source files** except files under `src/LocalGPT/Services/` which actually declare a Text or Regex service class matching the source filename (partial-class source files are supported). The check is intentionally lexical and strict: comments and string literals containing the exact token also fail, so prohibited source files cannot retain or conceal references. This is not a best-practice suggestion, a warning, a baseline or a debt waiver. Existing violations **must fail the build immediately**.

## Integration

- `build/Assert-StringBuilderOwnership.ps1`: enumerates first-party `src/**/*.cs`, `src/**/*.csx`, `src/**/*.razor`, skips generated `bin/obj`, checks the exact `StringBuilder` identifier, reports **each file and line** and throws on any violation. Only declared Text/Regex service classes qualify as owners. No exclusion list or suppression argument.
- `Directory.Build.targets`: mandatory `AssertLocalGptStringBuilderOwnership` target for ordinary LocalGPT builds, both Debug and Release, with **no skip switch**. Missing guard also stops build.
- `build/Invoke-RepositoryValidation.ps1`: invokes the guard before compilation.
- `build/Assert-RepositoryPowerShellBoundary.ps1`: additionally checks the target remains present and unskippable.
- Existing source, DI registrations, DevExpress components, live-session UI, PublisherStudio and database files are **unchanged**.

## Intentional initial failure

The 7.6.8 source contains numerous legacy `StringBuilder` usages outside owned services. They are reported in the accompanying audit, not grandfathered. 7.6.9 is therefore **intentionally not buildable until the offending implementations are migrated into existing or appropriate Text/Regex services**. Do not replace the forbidden identifier with a similarly verbose general-purpose string-accumulation pattern just to pass the test. Refactor responsibility to the existing DI-managed text services and simplify call sites.

## Verification boundary

The source/archive and a separate full repository scan were inspected; PowerShell and .NET compilation were **not executed** in the editor environment. Run `build/Assert-StringBuilderOwnership.ps1` and the normal Windows Debug/Release builds. The first build should fail with individual violations, as requested.
