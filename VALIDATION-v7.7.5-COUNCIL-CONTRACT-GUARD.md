# LocalGPT 7.7.5 host validation

Run on a Windows machine from the repository root (Windows PowerShell 5.1 is supported by the existing build entrypoints):

```powershell
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\build\Assert-CouncilXRoundWiring.ps1
.\Build-LocalDevelopment.ps1
```

Expected: the old 'missing contract review.IsSourceDraftReview' and 'missing contract review.IsSourceDraftReview && !review.ApprovalConsumed' failures must not return. The Council master guard still invokes the more specific `Assert-ReviewedArtifactReceipts.ps1` and other existing guards; an independent new error must be reported separately.

If Visual Studio's NuGet restore complains that `LocalGPTWebviewWrapper` cannot be loaded, capture the underlying error in the Error List before modifying the wrapper, or run:

```powershell
dotnet restore .\src\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper.csproj --verbosity detailed
```

That restore problem is distinct from the deterministic Council contract failure.
