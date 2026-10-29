# LocalGPT 6.6.6 — offline source validation

**Status: targeted build repair, not a successful native build.**

## User-reported failures addressed

- `MSB3073` from `Assert-ApplicationStaticPolicy.ps1`: seven newly introduced `private static` methods across CodeDOM, session-owned execution, and Hyperlight provider services converted to ordinary instance methods. All business behavior retained.
- `RZ2005` and `RZ1017` from `Chat.razor`: `@code` exit-code expression renamed to a non-directive identifier.
- `CS0246` from `Chat.Sandbox.razor.cs`: `LocalGPT.Components.Shared` imported to resolve existing internal `LocalGptSelectionOption<TValue>`.
- PowerShell-only sandbox regression assertions added; build-policy guards were not relaxed.

## Offline checks performed

- Scanned 839 LocalGPT C# files with a pattern reproducing the declaration constraints in the repository's application-static audit: **zero findings** after edits. (This is a source scan, not execution of the PowerShell script.)
- Checked the Razor exit-code expression and matching C# pattern variable, the Chat partial namespace import, and the declaration of `LocalGptSelectionOption<TValue>`.
- Parsed 12 C# project/MSBuild target XML files successfully.
- Confirmed seven existing runtime identifiers retained: win-x64, win-x86, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64.
- Compared against 6.6.5: **no baseline files omitted**, exactly seven existing files modified and two new documentation files added.
- Source package ZIP content and CRC integrity independently verified after creation.

## Not performed (toolchains unavailable)

- `powershell`/`pwsh` repository build/policy scripts, `dotnet build`, Razor generation, C# compilation, DevExpress runtime UI, or native Windows/Linux/macOS virtualization and guest smoke tests.

**Host acceptance:** open the updated 6.6.6 complete source tree in Visual Studio, run a normal Debug build, inspect the first remaining error if any, then optionally run `build/Assert-SessionSandboxIsolation.ps1 -RunGuestSmoke` on a host provisioned with the verified Hyperlight guest images. Do not interrupt an existing benchmark run merely to install this source update.
