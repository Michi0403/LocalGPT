# LocalGPT 7.4.2 acceptance tests

## A. Compilation / architecture gates (Windows, Linux, macOS)

After applying, invoke `powershell -NoProfile -File .\review\Invoke-7.4.2-Checks.ps1` to check the source contract and execute the real MSBuild architecture gates.

From your actual source checkout, first apply the diff to a clean `cce9574` working tree. Run the repository's normal PowerShell architecture and documentation gates and a **Debug** .NET 10 build. No gates were disabled. Pay attention to CS1573 XML param docs and any DI validation at startup.

```powershell
dotnet restore .\src\LocalGPT\LocalGPT.csproj
dotnet build .\src\LocalGPT\LocalGPT.csproj -c Debug --no-restore
```

The repository has local package and native dependency requirements; success on a developer machine does not prove a different host's package environment.

## B. New 3-file HelloMath draft, default knowledge-assisted subagents

1. Choose the saved **Knowledge-Assisted Coding Subagents** team and automatic resource mode.
2. Ask for `HelloMath.csproj`, `Arithmetic.cs`, and `Program.cs`, with code-generation review, real `dotnet build`, approval and ZIP.
3. After `begin`, verify the persisted manifest contains 3 distinct file paths. After each `complete`, verify receipt count progresses 1/3, 2/3, 3/3. Verify a byte-identical duplicate `complete` does not add an extra file; a changed-content duplicate must be rejected without modification.
4. Make the file-author's model say `SOURCE_DRAFT_READY` *early*. The loop must **continue** until the persisted count reaches 3/3.
5. Make the file-author **omit** the marker on the third completed file. The loop must still terminate and move to `submit`, based on persisted receipts.
6. Verify only then the immutable hash-bound review becomes visible. Approve it in Human Collaboration and inspect the actual build receipt, ZIP and 3 files. A pending approval must never be called success.

## C. Resume and existing-project compatibility

1. Interrupt/rejoin the same Council run after 1/3 file completions; the next work order must be the second file (not a re-creation of the first).
2. Rerun `submit` after a successful submission: it must return the same review ID/hash and not recreate or bypass the original approval.
3. Test an existing-repository staged workspace with **no** source-draft row. Its previous marker behavior should remain intact.
4. Test DB read failure: LocalGPT must **not** report a completed draft based only on model prose.

## D. Existing 7.4.1 functionality regression gates

- Hardware-aware GoalExecution / Configuration Steward visible in Chat; automatic and manual modes.
- Existing configured Council teams and role assignments; compact context recovery, no duplicate Council runs.
- MacBook Neo 8 GB small model: short work packets, no 262K speculative context allocation.
- Windows GPU model: guarded context, no repeated identical ROCm crash request.
- Source-archive learning with multiple files and offsets; drag-and-drop uploads.
- 1-Wire work/Excel/generation and human approval boundaries.
- Chat session detachment/rejoin retains the same run and its transcript.

**Status:** These are acceptance tests to execute locally, **not** claims of passed runtime tests.
