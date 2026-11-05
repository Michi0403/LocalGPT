# LocalGPT 5.8.8 — Scoped Council policy service correction

## Why 5.8.7 failed its Windows build

The repository PowerShell application-static architecture audit correctly rejected
`private static bool IsBrokerTool(string name)` in
`src/LocalGPT/Services/CouncilAutomaticFunctionPolicyService.cs`.
The six dependency projects succeeded, and the LocalGPT static audit stopped the build
before its C# compilation stage. This is a source-architecture violation, not a
Windows/macOS or PDFsharp issue.

## Correction

- The broker-entrypoint classification is now a **private instance method** of the
  existing `CouncilAutomaticFunctionPolicyService`.
- The policy service is already registered **scoped** as
  `ICouncilAutomaticFunctionPolicyService` in `Program.ServiceRegistration.cs`;
  its registration/lifetime and consumers are unchanged. No singleton, static state,
  or extra DI service is necessary for this cohesive policy behavior.
- The method now participates in the service's existing logged try/catch exception
  policy; an unexpected invalid input is reported then rethrown.
- The strict allowlist continues to exclude the three catalog-wide broker tools,
  while ordinary Council teams retain directly preferred tools and on-demand
  access to the entire policy-approved catalog through the broker.
- The existing PowerShell static audit is **not weakened or bypassed**.

## Versions and project scope

- LocalGPT: `5.8.7` → `5.8.8`.
- ReleasePackaging, PDFsharp, PublisherStudio and the rest of the solution are unchanged.

## Verification required on Windows

1. Run the existing PowerShell static architecture audit, then the ordinary
   Visual Studio Debug build; confirm that it reaches C# compilation.
2. Verify normal Council preferred/broker access and strict allowlist isolation.
3. Run a Game Development Team workflow with a source archive and check the
   recorded successful source reads, game project binding, build and playtest tools.

The source checks here cannot substitute for an actual Windows/.NET build.
