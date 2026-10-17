# LocalGPT 5.9.6 — Source verification

- Source baseline: `LocalGPT-5.9.5-source.zip`, supplied earlier in this conversation.
- Root layout, project structure, and every original ZIP entry preserved.
- Corrected the consolidated PDFsharp provenance paragraph to match `third_party/PDFsharp/SOURCE-NOTICE.md` **verbatim**.
- Independently verified consolidated notice contains all five required source notices after the exact release-guard normalization: `THIRD-PARTY-NOTICES.md`, `third_party/PDFsharp/LICENSE`, `third_party/PDFsharp/SOURCE-NOTICE.md`, `src/LocalGPT/wwwroot/css/open.iconic/ICON-LICENSE.txt`, and `src/LocalGPT/wwwroot/css/open.iconic/FONT-LICENSE.txt`.
- Confirmed guard-required product/license markers: PDFsharp 6.2.4, Bootstrap 5.3.1, Bootswatch themes 5.3.1, Kazuhiko Arase, DevExpress, Open Iconic fonts.
- Confirmed the original PDFsharp source notice, MIT license, and vendored implementation are unchanged byte-for-byte.
- Confirmed C# project version 5.9.6, XML validity, all entries retained, archive CRC, and unmodified release guard/entrypoint.
- No .NET/PowerShell runtime execution was performed; this is a static verification of the release blocker, not a successful publish claim.
