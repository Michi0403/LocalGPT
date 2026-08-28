# LocalGPT 3.4.4 source validation

This handoff was validated without invoking `dotnet` and without GitHub access.

Static checks cover:

- release identity at `3.4.4` and the single-digit minor/patch version rule;
- matching XML `<param>` entries for every constructor parameter named in the 15 `CS1573` warnings from the supplied 3.4.3 build log;
- preservation of the PowerShell 5.1-compatible `IndexOf(..., StringComparison) -ge 0` release-script fix;
- existing cross-platform and documentation/PDF source invariants;
- ZIP integrity and repository-root layout.

A real .NET/PowerShell release build still has to be run on the target build machine.
