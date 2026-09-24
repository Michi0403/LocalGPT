# LocalGPT 4.8.2 source validation

LocalGPT 4.8.2 was validated as a source package only. The handoff environment did not run `dotnet`, MSBuild, NuGet restore, publish, installers, or GitHub access.

The 4.8.2 source repair restores the intended bounded browser-PDF pipeline to ordinary IDE builds by making the repository-owned `LocalGPT.ReleasePackaging` merge helper a build-only dependency and passing it to `Build-Documentation.ps1`. Browser discovery is hardened for Windows while retaining the macOS/Linux paths, and adaptive browser parts are limited to 8/10/12 pages by default rather than 50-120 pages on larger hosts.

The maintenance guards remain enabled and were not relaxed. Static source validation passed for architecture, service resilience, text-service ownership, current version metadata, maintained JSON/Python syntax, and the 4.8.2 release-specific documentation-rendering contract.

See `VALIDATION-v4.8.2-source.md` for the detailed source-only validation boundary.
