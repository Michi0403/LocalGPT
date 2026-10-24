# LocalGPT 6.2.9 source validation

- Source contract test added for 1/8-page scenarios; PowerShell runtime not present here, so not executed.
- All 20 existing Python source assertions passed after updating version expectations.
- Existing build and architecture guard files unchanged.
- Full .NET/pwsh build and live PDF rendering **not performed**.

Browser retries do not skip invalid chunks, omit pages, fall back to an unsafe monolithic render on low-memory hosts, or delete verified earlier parts. Existing metadata/API completeness checks remain authoritative.
