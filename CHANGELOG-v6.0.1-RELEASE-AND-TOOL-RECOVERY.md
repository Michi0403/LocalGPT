# LocalGPT 6.0.1 — One-command release and shared tool compatibility

- Release entrypoint automatically repairs cached or missing release-packaging NuGet packages; no separate manual `Publish-ReleasePackagingPackage.ps1` step is required.
- Package packing finalizes root `licenses/THIRD-PARTY-NOTICES.txt` and `licenses/PDFsharp-6.2.4-MIT-LICENSE.txt` from original source bytes, then retains strict SHA-256 verification. Increments the changed helper from 1.1.1 to 1.1.2.
- The local development entrypoint prepares the same helper automatically for complete PDF documentation; Debug and Release retain the PowerShell-only build architecture.
- Ollama native-tool provider-rejection handling retries a compact, general DXFunction broker and upload workspace tool set before falling back to text-only function recovery; non-strict full-catalog access is not removed. Explicit strict policies remain strict.
- Text-only fallback uses the broker's real discovery -> preview -> invoke handshake and limits verbose function directory size for unrestricted chat.
- First unresolved automatic function call per batch logs bounded tool exposure diagnostics without payloads.
- Added one-command build source contracts to the early repository PowerShell boundary check and UML process/state documentation to the Council architecture chapter.

## Scope / verification

No changes to PublisherStudio, the vendored PDFsharp source, runtime approvals, persisted Council team definitions, or strict game runtime policies. Static-only validation in this environment; neither PowerShell/.NET build nor provider/tool-runtime replay has been performed. The supplied macOS environment must establish the publish result, and an Ollama single-chat + Council upload/read test must establish runtime recovery.
