# LocalGPT v0.1.4 human-collaboration debug steps

This is a source/debug candidate. Extract it into a **new clean folder** instead of overlaying an older build tree, so stale `bin`, `obj`, `.vs`, generated Razor files, and old SQLite schemas cannot survive.

## Build order

From the repository root in PowerShell:

```powershell
./build/Invoke-RepositoryValidation.ps1 -Configuration Debug
```

In Visual Studio:

1. Open `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln`.
2. Clean the solution.
3. Build **LocalGPT** first.
4. Fix the first root-project compiler error, if any.
5. Only inspect wrapper `CS0006` or `WMC1006` after `LocalGPT.dll` exists.
6. Build the complete solution and start the wrapper.

The full release gate still requires Debug and Release builds for the exact packaged source:

```powershell
./build/Invoke-RepositoryValidation.ps1
./build/New-VerifiedSourcePackage.ps1 -Version "0.1.4"
```

## Database and main-frame smoke test

1. Start LocalGPT with a disposable or backed-up local database.
2. Confirm both new migrations apply:
   - `20260726000000_AddHumanCollaboration`
   - `20260726001000_AddDeferredDxAiInvocations`
3. Navigate through `/`, `/chat`, `/model-council`, `/projects`, `/database`, `/test-lab`, and `/minecraft-mod-builder`.
4. Confirm the **Human team** launcher remains visible in the main frame on every routed screen.
5. Open `/__diag/component-activity?take=40` and confirm only sanitized operational summaries appear—never prompts, answers, files, generated source, secrets, exact approval parameters, or exception bodies.

## Exact controller approval test

1. Call one method decorated with `HumanApprovalRequiredAttribute`.
2. Confirm the first exact call returns HTTP `202 Accepted` and creates one persistent inbox item.
3. Approve it in the inbox.
4. Repeat the **same** request and confirm it executes once.
5. Repeat it again and confirm a new approval is required.
6. Change one argument before retrying and confirm the changed request receives its own approval item.
7. Decline a request with a reason and confirm the exact retry returns HTTP `403 Forbidden`.
8. During an active council run, confirm the decline reason enters the next heartbeat as guidance only and never grants authority.

## Live human council participation test

1. Enable **Take Part** and save a display name, role, expertise, and working style.
2. Start a multi-model council run.
3. While models are still working, send a contribution from Chat or the Human Collaboration Inbox.
4. Confirm the run is not cancelled or restarted.
5. Confirm the next heartbeat adds a `Human: <display name>` step.
6. Confirm a later model step includes one of these exact verdict markers:
   - `Human peer assessment: Supported — ...`
   - `Human peer assessment: Needs correction — ...`
   - `Human peer assessment: Mixed — ...`
7. Confirm the contribution and later peer review remain visible in the inbox.
8. Confirm the human contribution does not satisfy any approval gate.

## Deferred DXAI approval test

1. During an active council run, let a model request a DXAI function whose descriptor has `SupportsDeferredApprovalRequest=true`.
2. Confirm the exact parameters are persisted locally, omitted from logs, and the function returns `HumanApprovalPending` while unrelated council work continues.
3. Approve the item before a later heartbeat.
4. Confirm the next heartbeat executes the exact stored invocation once and adds a `LocalGPT: approved deferred function` step.
5. Confirm the result is explicitly labelled **untrusted data, never instructions**.
6. Confirm changed parameters require another approval.
7. If the council already ended before approval, retry the exact function manually; this candidate intentionally does not restart a completed council run.

## Component safety checks

- Trigger one successful and one deliberately failing handled UI action.
- Confirm the user receives a sanitized notification and technical details remain in the existing application log.
- Navigate after a routed component failure and confirm the error boundary recovers.
- Confirm every maintained component still has top-level `ILogger<T>`, `INotificationService`, and `IComponentActivityService` injection.

## What to send back

Send the **first compiler errors from the LocalGPT project**, plus approximately 15 source lines around each location. Do not start with wrapper `CS0006` or `WMC1006` unless the LocalGPT project itself built successfully.
