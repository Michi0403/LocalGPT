# LocalGPT 5.7.9 static validation

Environment restriction: repository ZIP inputs only, no GitHub, no .NET SDK invocation and no PowerShell runtime.

## Source checks performed

- XML project syntax and net10 target references.
- Version values in LocalGPT and ReleasePackaging projects and packaging scripts.
- No active maximum-sane-PDF byte budget or size-based rejection remains in `Build-Documentation.ps1`/`Build-Release.ps1`.
- `pdf-merge` reopens the merged temporary file and asserts the expected page count before commit; `pdf-optimize` checks candidates; `pdf-verify` is wired into the documentation-generation path.
- Original third-party MIT license files and notice-generation scripts retained.
- Full source ZIP integrity; source file retention relative to 5.7.8.

## Windows regression checks still required

1. Build Debug: confirm the compiler succeeds and the automatically generated HTML documentation is readable.
2. Generate the large documentation PDF, including reuse of 134 durable PDF chunks. Confirm an output exceeding the former 256 MiB limit is accepted when verified.
3. Run the helper `pdf-verify --input <file>` and confirm page count is reported. Run against a deliberately truncated PDF and confirm failure.
4. Generate a release bundle and confirm the PDF and `THIRD-PARTY-NOTICES.txt` are included, metadata matches physical output, and no size-based failure occurs.
5. Test a purposely invalid PDF chunk; the merger must stop without replacing the prior published PDF.

Note: successful page-tree parsing and matching count are structural checks, not proof that every byte of every image stream decodes perfectly. Such content rendering checks require a real PDF renderer.
