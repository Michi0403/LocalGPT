# LocalGPT 3.1.10 — Chat Attachment MIME & Draft Recovery

## Why this release exists

A real 3.1.9 published run showed that files could be selected successfully in the native DXAiChat paperclip UI, but sending the prompt could fail with `File has no MIME type. Please ensure that the attached file has an extension.` When that pre-send validation failed, DevExpress had already cleared the composer, so the typed prompt disappeared and no corresponding user message was committed to the chat transcript.

This repair is intentionally narrow. It does not redesign upload handling and does not touch the working 3.1.9 Chat layout.

## Browser MIME normalization

- Kept the native `DxAIChat` upload surface and automatic send pipeline.
- Added a capture-phase normalization step for the existing DXAiChat `<input type="file">` only.
- A selected file with a non-empty browser MIME type is passed through unchanged.
- A selected file whose browser `File.type` is blank is represented by a browser `File` clone with the same name, bytes and `lastModified` value and the generic MIME type `application/octet-stream`.
- This specifically covers extensions that Chromium/Windows may select successfully but expose without a browser MIME value, such as script/source/command or uncommon binary formats.
- No allow-list was tightened and no file extension restriction was added. The existing broad LocalGPT upload policy remains authoritative.

## Failed native-send draft recovery

- A normal DXAiChat send now keeps a short-lived snapshot of composer text and pending `File` objects before DevExpress processes the send action.
- The snapshot is passive during successful sends.
- If the specific missing-MIME pre-send validation message appears after submission, LocalGPT restores the cleared composer text and pending files so the user can retry rather than losing the request.
- The recovery does not synthesize a fake sent user message. Once validation succeeds, the native DXAiChat flow commits the real user turn normally.
- The custom live-Council mid-run send path remains unchanged and continues to use its existing `application/octet-stream` fallback.

## Backend defense in depth

`CouncilTextService.ExtractUploadFiles(...)` now normalizes an empty `DataContent.MediaType` to `application/octet-stream` before creating the upload-workspace input record. This preserves a non-empty content type at the service boundary without changing file bytes, names, analysis, workspace behavior or execution policy.

## UI/layout preservation

- `Chat.razor` is byte-identical to 3.1.9.
- `Chat.razor.css` is byte-identical to 3.1.9.
- Chat Configuration is untouched.
- The `DxAIChat` subtree is untouched.
- Team / Models / Performance quick preset row is untouched.
- Attach / Send / Stop and the memo editor are untouched.
- No new CSS was added.

## Preserved contracts

No EF migration, BenchmarkEvidence schema migration or 1-Wire protocol change is introduced. Existing Council recovery, repetition watchdog, benchmark evidence, coverage truth guard, renderer-affinity policy and XML documentation enforcement remain in place.
