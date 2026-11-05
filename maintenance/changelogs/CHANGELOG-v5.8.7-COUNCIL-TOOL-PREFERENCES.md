# LocalGPT 5.8.7 — Council tool preferences, intentional strictness and proactive software assistance

## Design correction

The saved function names on an ordinary Council team/step are **preferred native tools**, not a sandbox. The complete registered, policy-approved catalog remains accessible through `dxai.tools.discover` / `dxai.tools.preview` / `dxai.tools.invoke`. Native Ollama metadata is kept intentionally compact rather than advertising thousands of schemas on every model request.

- **AllPolicyApproved**: expose the broker; when the team or step already has preferred names, attach those tools directly as well. The exact registered function and human approval policies remain the authority.
- **TeamAllowList / ExactAllowList**: for ordinary Council teams, expose the selected preferred functions as native tools **plus** the broker. Preserve the previous hard-restriction behavior for existing `games.*` runtime teams.
- **StrictTeamAllowList / StrictStepAllowList**: new explicit opt-in exclusive modes for any team. Omit broker tools and strip any manually included broker entrypoints from the strict set so strict mode cannot accidentally bypass its restriction through the general dispatcher. An empty strict set disables native tools.
- **Disabled** and tool-suppressed rounds continue to expose nothing. Previously saved enum values and user-owned team definitions are not rewritten.

## Proactive maintenance support

- Enabled unrestricted Council steps are now instructed to use available policy-approved local tools proactively when the actual user task concerns software defects, databases, coding, logs, or workspaces. This reuses the existing DXFunction registry and does not invent unrestricted database repair privileges.
- Consequential actions, human checkpoints, migrations and configured approvals are never bypassed. Models must report observed repair evidence, not a claimed fix without a tool result.
- In the maintained **Game Project Development Team first-run template**, mechanics, AI integration and final handoff steps are no longer silently tool-disabled; they can retrieve the project/source or runtime state through preferred functions and discover others on demand. The existing user-edited database presets remain authoritative.
- Council Teams editor now explicitly differentiates preferred and strict modes; provider evidence uses accurate exposure language rather than calling every preferred list "restricted".
- PowerShell source-contract audit checks the new strict and preferred paths.

## Versions and unchanged projects

- LocalGPT `5.8.6` → `5.8.7`.
- PDFsharp, ReleasePackaging, PublisherStudio, documentation generation and packaging formats are unchanged.

## Runtime verification still required

1. In Windows, rebuild LocalGPT and run ordinary General / Learning / Game Project Development teams. Confirm preferred native functions are callable, and that other approved functions remain discoverable and invocable through the broker.
2. In the Council Teams UI select StrictStepAllowList and verify an unlisted function cannot be dispatched through a broker; confirm Disabled exposes no tools.
3. Confirm normal software maintenance steps can inspect actual local evidence and request authorized repairs. Approval-gated database changes must still pause for approval.
4. Existing saved teams are not automatically overwritten. The Game Development seed improvements apply to new/default templates; review saved workflow-step settings in Council Teams if they were previously customized or persisted.

No .NET build, Ollama request, or Windows PowerShell execution was performed in this environment.
