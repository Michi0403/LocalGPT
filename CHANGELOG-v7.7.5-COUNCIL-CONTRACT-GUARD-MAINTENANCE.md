# LocalGPT 7.7.5 — Council source contract guard compatibility

Baseline: supplied 7.7.4 source archive. No GitHub access or .NET SDK used.

## Fixed

- Align the two obsolete source token assertions in `build/Assert-CouncilXRoundWiring.ps1` with the 7.7.4 verifier's broader, stricter source-review contract.
- The general Council contract guard now requires `!review.ApprovalConsumed` rather than the removed `review.IsSourceDraftReview && !review.ApprovalConsumed` condition. This checks consumed human approval for direct reviews and coordinated source drafts alike.
- The second guard requires iteration of every reviewed file, rejects missing/ambiguous ZIP entries, and retains the immutable content SHA-256 check. It no longer requires the removed `review.IsSourceDraftReview` token.
- Keep `build/Assert-ReviewedArtifactReceipts.ps1` in the same existing validation chain. It independently forbids draft-only hash/status gating. No bypass or suppression of source integrity, approvals or build status has been added.
- Increment the LocalGPT application version from 7.7.4 to 7.7.5.

## Not changed

- The C# verifier, build workflow, Council model settings, platform targets, dependencies, and the PublisherStudio repository are unchanged.
- The `LocalGPTWebviewWrapper` NuGet restore warning is a separate Visual Studio project-load/restore diagnostic. The supplied build excerpt gives no underlying restore error code, so this revision does not speculate about or alter its project settings.

## Validation

- Static comparison of the two source contracts to the C# verifier completed; all required tokens are present.
- Source/archive structure and changed-file scope can be checked without running .NET.
- PowerShell and .NET execution were unavailable in the editing environment; a successful Windows compilation is not claimed.
- On Windows, run `powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File build/Assert-CouncilXRoundWiring.ps1` and then your normal Debug build.

## Separate Windows restore follow-up

If Visual Studio still reports that details for `LocalGPTWebviewWrapper` could not be loaded, capture the first concrete NuGet/SDK error in the Error List or run `dotnet restore src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj --verbosity detailed` on the Windows machine. The generic IDE message alone does not establish a source fix.
