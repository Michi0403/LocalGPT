# LocalGPT package outputs

LocalGPT is the authoritative source owner for both shared package families used by PublisherStudio:

- `LocalGPT.WireProtocolVersion` — DLL-backed 1-Wire protocol contracts.
- `LocalGPT.ReleasePackaging` — .NET tool package for shared release packaging and managed PDF assembly/optimization.

`Build-Release.ps1` builds the authoritative release-packaging package from `src/LocalGPT.ReleasePackaging`, places it in the repository package cache, copies it to the per-user `LocalGPT/NuGet` cache, and includes it in the upload-ready LocalGPT release bundle. PublisherStudio consumes the resulting package; it must not carry a second copy of this source project.

Generated `.nupkg` files are build outputs and remain ignored by Git.
