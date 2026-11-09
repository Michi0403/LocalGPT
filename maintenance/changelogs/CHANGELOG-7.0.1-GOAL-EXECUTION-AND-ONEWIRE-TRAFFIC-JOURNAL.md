# LocalGPT 7.0.1 — goal execution that finishes, and a 1-Wire traffic journal

7.0.1 builds on 7.0.0. It does not add a new orchestration strategy. Most of what a Council needs to finish coding work already existed: X-Rounds, completion recovery, required completion receipts and change reviews. Several small stoppers kept those parts from reaching the models or from being enforced, and this release removes them. It also adds a journal of every 1-Wire packet, matching PublisherStudio 4.7.3.

**Not compiled.** No .NET SDK was available while this drop was written. Every edit was checked by reading and by the static emulations listed at the end.

## 1. Goal execution: the stoppers that were removed

### X-Round reasons reach the model
- When a step is revisited, its prompt now opens with **Why this step runs again — resolve this before anything else**, followed by the X-Round cause. Before, the cause was only stored as `XRoundCause` metadata, so a revisited model usually repeated its first answer.
- `MultiModelCouncilService.WorkflowDefinitionExecution.cs` writes the section and logs it at Information.

### Granted X-functions are offered as tools
- A step with `XFunctionsEnabled` now gets `council.x.status`, plus `revisit`, `return_text`, `start_single_model` and `start_council` according to its flags.
- They are added both to the strict step allow-list and to the preferred exposed list (`CouncilAutomaticFunctionPolicyService`).
- Before, a step that used a strict allow-list could not call the X-functions it had been granted.

### Required completion receipts are enforced
- Every function in a team's `RequiredCompletedFunctions` that has no explicit recovery rule now gets an implicit one (`MultiModelCouncilService.ImplicitCompletionRecovery.cs`).
  - It re-runs the last step that may call the function.
  - The step's allow-list decides; the team list is used when the step has no strict allow-list.
- Two new runtime values in `CouncilExecutionRuntimeParametersJson` control it:
  - `ImplicitCompletionRecoveryAttempts`, default 2, capped by `MaximumCompletionRecoveryAttempts`;
  - `ImplicitCompletionRecoveryPromptTemplate`, which states that only a completed result counts, not a plan or a claim.
- If a receipt is still missing after recovery, implementation artifacts are not generated, and the run says which receipt is missing. Before, a missing receipt produced only a warning.

### No placeholder change reviews
- `CreateCouncilChangeReviewAsync` returns no review when the final answer has no structured code-generation plan, and the run explains why.
- The fallback that wrote `CouncilFeaturePage` / `CouncilFeatureRequestExample` placeholder files is removed.

### Generated code keeps the parent revision
- `CodeGenerationWorkflowService` saves generated code as a new child revision of the selected revision and makes the child current. The parent stays untouched.
  - The child is named `<base> · generated <id8>`.
  - The workspace is registered and scanned on the child.
  - `CodeGenerationExecutionResult.GeneratedRevisionId` carries its id.
- Before, generation overwrote the selected revision.

### Compiler errors flow back into the loop
- New `IBuildDiagnosticSummaryService` extracts the distinct `: error CODE:` lines from build output. When there are none, it uses the output tail.
- Two new runtime values bound the extract: `BuildDiagnosticMaximumLines` (default 40) and `BuildDiagnosticMaximumCharacters` (default 6000).
- The extract is used in two places:
  - `CodeGenerationExecutionResult.BuildDiagnostics` for failed build targets. It is also part of the result summary returned to the Council after an approved `codegen.review.execute`.
  - The project-maintenance build review appends **Compiler diagnostics:** to a failed verification summary.

### The seeded compiler team closes its own loop
- The program compiler team now:
  - requires `codegen.review.create`;
  - lets `compiler-curate`, `compiler-toolchain` and `compiler-verification` call `council.x.revisit` with mode `reexecute` against `compiler-generate`, at most two transitions per step.
- Those steps' prompts now say: do not accept missing files or a failed build, and send the work back with the exact files or error lines.
- `compiler-generate` is told how to handle a revisit.
- The team seed version is now 52, so untouched seeded teams refresh. Teams you edited are left alone.

### Promoted source is approved source
- `ProjectIngestionService.PromoteAsync` now synchronizes the workspace with `sourceApprovedByUser: true`.
- Tracked files from that sync are marked `IsUserApproved`.
- Generated files are never auto-approved, and an existing approval is never revoked.

## 2. 1-Wire traffic journal

- `IOneWireTrafficJournal` records every packet. Journaling failures are logged and never interrupt a transport. It covers:
  - TCP envelopes in both directions. Rejected incoming frames are journaled with their reason, and failed sends are journaled too.
  - HTTP/JSON requests and responses, including rejected requests and profile lookups.
  - Each UDP discovery beacon LocalGPT broadcasts.
- It logs to the category `LocalGPT.Services.OneWire.OneWireTrafficJournal`:
  - Information: one metadata line per packet; Warning for rejected or failed packets.
  - Debug: the decrypted payload.
  - Trace: the raw frame.
- `appsettings.json` ships the category at Information. `appsettings.Development.json` sets it to Debug.
- Settings live in `OneWire:Traffic` and are re-read live:
  - `Enabled`
  - `PayloadMode`: `Full` (default), `Redacted` or `None`
  - `IncludeRawFrame` and `IncludeDiscovery`
  - `JournalCapacity`, `MaximumStoredCharacters` and `MaximumLoggedCharacters`
  - `RedactedPropertyNames`
- **1-Wire Security → Traffic** shows the live view:
  - search, direction, transport and problems-only filters;
  - pause and clear;
  - a packet grid, with the payload and raw frame of the selected packet.
- `Full` at Debug writes decrypted payloads to the log sinks. That is the requested default for debugging between your own applications; use `Redacted` or `None` when logs leave the machine. `docs/architecture/onewire-security.md` states this in its security invariants.

## Unchanged

- `GenerateImplementationArtifact` remains an explicit request option, and every write still goes through the existing change-review and human-approval gates.
- The default `council.x.revisit` mode is unchanged (`reconsider` when no mode is given). The seeded prompts ask for `reexecute` explicitly.
- The 1-Wire protocol, MFA trust, replay protection and approval flows are unchanged.

## Verification (static only)

- XML-documentation heuristic over every changed C# file: no new CS1591/CS1572/CS1573 findings.
- Brace balance of every changed file.
- No new lines over 600 characters.
- Text-ownership patterns in changed Components and Controller files.
- Component safety directives: `ILogger<T>`, `INotificationService Notifier` and `IComponentActivityService ComponentActivity` in the new panel.
- No native interactive controls in the new panel.
- For the build guards that read edited files, every guard token that matched in 7.0.0 still matches in 7.0.1 (only additions).

There is **no claim of a compiler run, a PowerShell guard run or a live Council or 1-Wire session**.

## Runtime checks worth doing first

1. Run the program compiler team on a small request with a deliberate compile error. Verification should send the error lines back to `compiler-generate`, and the child revision should appear under the parent.
2. Ask the compiler team for something vague enough that no structured plan comes back. After the recovery attempts, the run should report the missing `codegen.review.create` receipt and generate nothing.
3. Link PublisherStudio 4.7.3 and compare both traffic views for one round trip. The sequences should match, with one Sent on each side for every Received on the other.
