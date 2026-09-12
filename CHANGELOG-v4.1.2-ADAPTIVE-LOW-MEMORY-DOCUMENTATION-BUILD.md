# LocalGPT 4.1.2 — adaptive low-memory documentation build

LocalGPT 4.1.2 reduces peak memory pressure in the documentation/PDF stage without weakening documentation completeness, release validation, native packaging, the 4.1.1 provider workbench, or the 4.1.0 Matrix operator control plane.

## Changes

- Documentation build policy now detects physical system memory on macOS, Linux, and Windows, with `GC.GetGCMemoryInfo()` only as a fallback.
- Browser PDF chunk size is memory-adaptive. Hosts around 8–10 GiB use 8-page chunks; 64 GiB-class hosts remain at 100 pages per chunk.
- Chromium JavaScript and Node.js heap ceilings scale with system memory instead of always permitting 4096 MiB heaps on small machines.
- DocFX HTML generation now uses the supported `--maxParallelism` switch with memory-sensitive limits; low-memory mode uses one worker.
- `FUTURE2_DOCUMENTATION_LOW_MEMORY=1` forces the conservative profile. Chunk pages, browser heap, Node heap, DocFX parallelism, and detected-memory bytes remain individually overrideable for controlled build hosts.
- On low-memory hosts, LocalGPT and PublisherStudio share a cross-product heavyweight-documentation lock so their DocFX/Chromium stages cannot accidentally compete for the same machine.
- The browser print-book builder no longer retains an unused second copy of every source HTML page.
- Front-matter/TOC generation keeps only page metadata instead of retaining every full page body.
- Temporary per-chunk HTML is removed immediately after a durable PDF part is produced/reused; low-memory mode performs a generation-2 collection between chunks so large transient strings do not accumulate in the long-lived PowerShell host.
- Documentation status records the detected memory, chosen chunk size, heap ceilings, DocFX parallelism, low-memory mode, and heavyweight-stage serialization decision.
- The 4.1.1 provider-management JSON helper no longer uses an iterator (`yield`) shape, which had correctly tripped `Assert-IteratorExceptionPolicy.ps1` before the RID-neutral release build. It now materializes the small model-object set under the normal logged service exception boundary, preserving behavior without adding an iterator-policy baseline exception.

## Compatibility

The durable chunk cache, PDF validation, early-completion browser termination, macOS signing protections, Pages PDF version hygiene, InteractiveServer render behavior, provider/Council behavior, installer operator controls, and application runtime code remain intact.
