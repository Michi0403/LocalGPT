# LocalGPT 5.6.1 — per-file maintenance and verification checklist

This checklist records **every changed/added file** for 5.6.1. The 5.6.0 release and its business-object/UI feature set are retained. No PublisherStudio file was changed.

| Changed / added file | Individual preservation or repair check | Verification |
|---|---|---|
| `Build-Release.ps1` | Request the new packaging helper before the documentation workflow; retain native packaging and cross-platform paths | Source audit |
| `build/Ensure-ReleasePackagingPackage.ps1` | Compare source-project and requested versions before cache reuse; reject installed helpers without manifest support before DocFX | Source audit |
| `build/Publish-ReleasePackagingPackage.ps1` | Refuse to publish a helper with a mismatched project version; keep repository NuGet package output | Source audit |
| `build/Build-Documentation.ps1` | Check helper capability before PDF-required documentation begins; retain ordered chunk manifest, compression and size ceiling | Source audit |
| `src/LocalGPT.ReleasePackaging/Program.cs` | Advertise the manifest capability only when the existing `pdf-merge --input-list` handler exists; retain PDF optimization | Source audit; compiler pending |
| `src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj` | Increment tool version to 1.0.4, consistent with package scripts | Source audit |
| `src/LocalGPT/LocalGPT.csproj` | Increment application version from 5.6.0 to 5.6.1; one-digit minor and patch | Source audit |
| `src/LocalGPT/Components/App.razor` | Advance JavaScript cache identity while retaining the existing Game Studio, console and Chat integration | Game Studio audit |
| `src/LocalGPT/Components/Shared/ChatMicrophone.razor` | Advance microphone module cache identity without changing audio/JS interop behavior | Source review |
| `build/audit_ascii_console_memory_5_6_0.py` | Keep CRLF, PowerShell shell choice, DevExpress list, fullscreen/popup and RAM/VRAM feature contracts | Source audit |
| `build/audit_game_studio_5_5_5.py` | Keep Game Studio/Godot world time, revisions and InteractiveServer/DevExpress checks | Source audit |
| `build/audit_game_revision_chat_5_5_8.py` | Keep the Council-authored revision list and Chat game playback contracts | Source audit |
| `build/audit_data_management_5_5_1.py` | Keep Data Management/Council business-object and safe approval contracts | Source audit |
| `build/audit_data_management_workspace_5_5_3.py` | Keep DevExpress entity editors, CRUD and relation paging | Source audit |
| `build/audit_data_management_text_service_5_5_4.py` | Keep database text ownership and protected primary-key editing | Source audit |
| `build/audit_learning_checklist_5_4_9.py` | Keep the persistent, UI-visible per-file learning checklist contract | Source audit |
| `build/audit_release_packaging_5_6_1.py` | Assert package version alignment, fail-fast checks, manifest, PDF compression/size guard and retained feature audits | Source audit |
| `CHANGELOG-v5.6.1-PDF-RELEASE-HELPER-HANDSHAKE.md` | Record confirmed root cause, exact repair and PublisherStudio impact | Documentation review |
| `RELEASE-CHECKLIST-v5.6.1.md` | Track every changed file and outstanding native runtime tests | Checklist completeness audit |
| `VALIDATION-v5.6.1-source.md` | Record executed source-only checks, untouched-file comparison and remaining .NET/macOS publish verification | Source report |

## Maintainer's runtime acceptance checklist

- [ ] Build LocalGPT 5.6.1 in .NET 10; all architecture/DevExpress/async/InteractiveServer guards must remain enabled and pass.
- [ ] Run `Build-Release.ps1` on the target publish host. Confirm `LocalGPT.ReleasePackaging 1.0.4` installs and its `capabilities` output includes `pdf-merge-input-list` **before** DocFX begins.
- [ ] Confirm the 195 durable PDF parts (when their cache fingerprint still matches) can be reused and merged, without repeating the render unnecessarily.
- [ ] Confirm the merged PDF is compressed as needed and still rejected above the configured sane size ceiling; verify no 2 GiB fallback.
- [ ] Verify the final `LocalGPT-5.6.1.pdf` and all expected runtime/platform publication artifacts are present.
- [ ] Spot-check Chat ASCII Console, Game Studio, Publisher-integration, Data Management, approval gate, Install hardware memory estimates and retained DevExpress controls.
- [x] PublisherStudio 4.1.8 source inspected: its `--input` PDF merge remains compatible with pinned packaging helper 1.0.2; no PublisherStudio release required for this issue.
- [ ] .NET compiler and actual cross-platform publish validation — not available in this workspace; do not mark complete until run by maintainer.
