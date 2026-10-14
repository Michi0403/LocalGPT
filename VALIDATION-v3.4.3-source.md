# LocalGPT 3.4.3 source validation

This handoff was validated without invoking `dotnet` and without GitHub access.

Static checks cover:

- release identity and the single-digit minor/patch version rule;
- absence of PowerShell `String.Contains(value, StringComparison)` overloads in maintained `.ps1`/`.psm1` files;
- presence of the compatible `IndexOf(..., StringComparison) -ge 0` release-script check;
- the existing cross-platform boundary audit;
- the existing 3.4.2 documentation/PDF payload split invariants;
- ZIP integrity and repository-root layout.

A real .NET/PowerShell release build still has to be run on the target build machine.
