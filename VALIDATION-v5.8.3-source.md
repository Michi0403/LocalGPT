# LocalGPT 5.8.3 — source checks and Windows acceptance

## Static checks

- Verify `Chat.razor` has no native `<dialog>` / `<section>` elements; the Council popup continues to be owned by DevExpress `DxPopup` and `DxFormLayout`.
- Verify the two inspection regions retain both original `aria-label` values, CSS classes, and their content.
- Verify XML `<param>` tags match `runId` and `activity` on `ToggleCompletedCouncilActivityEvidence` and include `asGameProject` on the interface and implementation of `EnsureCouncilRunProjectAsync`.
- Validate the modified `.csproj` XML, the 5.8.3 version and all original files and ZIP members.

## Windows acceptance (not executed here)

1. Run the existing PowerShell maintenance targets unchanged. Confirm `Assert-RazorMaintenanceArchitecture.ps1` succeeds.
2. Build Debug with XML documentation warnings treated as errors. Confirm CS1573 is absent for all three reported locations.
3. Confirm the `Council answer · provider evidence` popup still opens with both scrollable panes and complete available server-side evidence.
4. Recheck Game Project Development preflight and source read receipts from 5.8.2, since runtime behavior cannot be verified from static source.

**Limit:** No .NET SDK, PowerShell, DevExpress runtime, or Ollama run was used to assert a full build or functional test result.
