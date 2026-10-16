# LocalGPT 5.8.1 source validation

- No dotnet restore/build/publish, PowerShell execution, GitHub access or online package downloads were attempted.
- Verified the orientation split in `build/Build-Documentation.ps1`: front cover/TOC landscape, separate body chunks portrait and a versioned durable cache key.
- The complete repository from the supplied version is preserved in the output ZIP.
- The release artifact requires a Windows build to confirm final PDF page geometry, page counts, runtime UI behavior and any remaining compilation issues.
- Verified syntactic presence of the Council's on-demand full available snapshot retrieval, split inspection layout, theme heading isolation, and bounded unknown-text detection. The PDF printed page count reduction and per-model source-file-reading coverage are not established by this static check.
