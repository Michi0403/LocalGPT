# LocalGPT v0.1.4 compile-fix candidate

This source package fixes the compiler diagnostics reported after the first v0.1.4 archive and adds compiler-backed release gates.

It is intentionally called a **candidate** because this isolated environment does not have the owner’s licensed DevExpress feed, Windows SDK/workloads, or a working .NET SDK installation. It has passed the included source, lexical, configuration, protected-file, and package-hygiene checks, but the owner-side command below remains authoritative:

```powershell
./build/Invoke-RepositoryValidation.ps1
```

Do not rename or publish this candidate as a verified release until that command completes both Debug and Release builds. After success, create the normal package with:

```powershell
./build/New-VerifiedSourcePackage.ps1 -Version "0.1.4"
```
