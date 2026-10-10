# LocalGPT 5.5.0 source validation and maintainer acceptance

## Source-only verification

The release-preparation environment has no .NET SDK or Windows PowerShell and does not access GitHub or NuGet. The following source-only checks are intended to run before packaging; record exact outcomes in the release handoff:

- `python3 build/audit_documentation_pdf_merge_5_5_0.py`: input-list argument, original chunk order, durable recovery, unchanged managed PDF checks, and version format.
- `python3 build/audit_learning_checklist_5_4_9.py`: retained 5.4.9 file checklist and Council repairs, with an exact current-release version assertion.
- `python3 build/audit_powershell_portable_paths.py` and the maintained architecture/DevExpress/async audits: no new violations of the repository maintenance contract.

## Windows acceptance test

1. In the user's **existing** checkout and paths, build LocalGPT in the pinned .NET 10 environment normally. Do not set a PDF bypass switch or enable the monolithic fallback.
2. Confirm DocFX HTML generation/validation and required browser PDF chunk processing finish as before. The console should report a merge of the cover/index and all chunks using the repository-owned `LocalGPT.ReleasePackaging` helper and **must not** report a native-process filename/extension length error.
3. Confirm the versioned `LocalGPT-5.5.0.pdf` exists in the published documentation output, passes the existing PDF validation, opens, and contains both cover/index and API pages in their source order.
4. Interrupt/retry a documentation run after rendering multiple chunks: verify existing valid chunks are reused and the final merge succeeds without a new browser rerender for those chunks.
5. Verify required documentation artifacts and existing DevExpress UI, Human Collaboration approval gate, Ollama Council, per-file learning checklist, and all architecture/retention guards as part of the normal solution build.

The confirmed 5.4.9 failure occurred **after all chunks were rendered**, when invoking the merge executable with 127 absolute arguments. This patch targets that proven failure; a successful complete Windows merge cannot be asserted without the maintainer's build log.
