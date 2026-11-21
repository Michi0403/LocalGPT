# LocalGPT 7.6.9 — approval scope binding and persisted build diagnostics

## Security fix: saved approvals cannot reach other tasks or projects
- Run a142abf7 (21 Nov) wrote and built review A142ABF7 although no human approved it. The gate reused
  HumanCollaborationRequest 22BD5384… (`dxai.function.codegen.review.execute`, approved 16 Nov for a different
  review, `ApprovalReuseScope=PersistentUntilChanged`, never consumed). `IsReusableDecisionForRequest` returned
  true for any later request of the same operation, and the review recorded "Approved by the current user."
- `HumanCollaborationService.Policy.IsReusableDecisionForRequest`: a saved decision with `IsSensitive=true`
  (every non-read-only function: write, build, execute) now authorizes only the exact same request
  (fingerprint = review, hash and project). Saved session/persistent reuse remains available for read-only
  operations. Owner rule: a human approval must come from the task or project it was given for.
- Existing saved decisions are not modified; the 16 Nov approval is now inert for other reviews.

## Build diagnostics persisted
- `CodeGenerationWorkflowService.Execution`: every attempted build writes `BUILD_DIAGNOSTICS.md` (status and
  summarized compiler output) into the generated workspace before the ZIP is created. Run a142abf7 failed with
  `TwitchDashboard.csproj:BuildFailed`, but its errors were only in the application logger.

## Open (next)
- DecisionNote must name the actual approval request and decider instead of the "current user" fallback.
- Greenfield project completeness: Program.cs used `App` while `App.razor`/`_Imports.razor` were never planned;
  `wwwroot` was placed outside the project directory (task #59, regex layout rules / SDK template base).

## Verification
- Not compiled in the editing environment; owner Debug build/run in Visual Studio required.
