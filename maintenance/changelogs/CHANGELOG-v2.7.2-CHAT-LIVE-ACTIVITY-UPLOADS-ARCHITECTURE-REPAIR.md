# LocalGPT 2.7.2 changelog

## Chat live activity and parallel Council visibility

- Preserved the existing ordered Council transcript/streaming behavior rather than replacing the working chat stream.
- Added a parallel live participant activity surface so work from every selected model is visible while it is happening, including models running on another configured machine/host.
- Live participant cards identify model, phase/role, hardware/host road and current status, and carry the same streamed model/tool/function status content while the ordered final transcript remains readable.
- Parallel participants now publish waiting/running/completed/error activity through `ICouncilLiveSessionService` instead of remaining visually hidden behind ordered transcript presentation.
- Added a compact scheduling selector for host-balanced versus configured hardware-road parallel execution while retaining the advanced hardware-road editor.

## Chat uploads and restored sessions

- Added mid-run Council upload support for text and files through the existing chat upload-workspace mechanism.
- File payloads are converted into the same bounded upload workspace/context supplied to Council models; the user-visible message carries attachment chips.
- The browser bridge now caches selected files as soon as DevExpress materializes them, preventing native input resets from leaving uploads visually stuck or silently dropping them during a running Council session.
- Accepted live uploads clear both the browser file input and the DevExpress upload-chip surface and trigger a layout refresh.
- Persisted chat messages now preserve lightweight attachment name/icon metadata so rejoined sessions can render the file indicator again.
- Added initial/rejoin layout stabilization so a long chat is measured after hydration/rendering instead of waiting for an unrelated expandable section to force a resize.

> Attachment labels that were never persisted by a pre-2.7.2 build cannot be reconstructed from an old database row that contains no attachment metadata. New/updated conversations saved by 2.7.2 preserve the display metadata going forward.

## Maintainable architecture/code-generation choices

- Generalized the `/chat` Architecture configuration away from the original C#/Minecraft-first assumptions.
- Added a database-provisioned language/toolchain choice covering repository-defined behavior plus C#/.NET, C++/CMake, Java/Maven/Gradle, JavaScript/TypeScript/Node, PowerShell, Python, Rust, Go, HTML/CSS/JS and other/custom toolchains.
- Expanded provisioned source/artifact text extensions for the corresponding languages and build/project formats.
- Architecture guidance now explicitly tells the Council to follow the selected/actual repository toolchain and not assume C#, .NET, Blazor or Minecraft.
- Preserved the existing code-generation DXFunctions, reviewed workspace file writer, PowerShell output, CodeDOM fallback, solution/repository output modes and approval gates.

## 1-Wire and previous fixes retained

- Retained 2.7.1 live post-link 1-Wire capability/skill updates and PublisherStudio peer-registry refresh behavior.
- Retained the removed arbitrary repository/import limits and database-backed runtime policies introduced in the preceding releases.
- `LocalGPT.WireProtocolVersion` remains 2.1.1 because this release does not require a new wire message contract.

## Version

- LocalGPT application/wrapper/installer: **2.7.2**.
