# LocalGPT 3.3.5

LocalGPT 3.3.5 repairs the clean-source documentation build after the cross-platform prerequisite bootstrap reached the .NET build successfully on macOS.

The source distribution now contains the authored `docs/` payload that `LocalGPT.csproj` and `Build-Documentation.ps1` require: the conceptual documentation chapters, root/category TOCs, DocFX configuration, complete-PDF TOC/cover and Kawaii theme sources. The shared build prerequisite script verifies this payload before DevExpress/Node setup and before the long build, so an incomplete source archive reports its missing files immediately rather than failing later with `MSB3030` copy errors.

The 3.3.4 Windows/macOS/Linux portable Node.js bootstrap remains intact. DocFX assembly-reference repair is also expanded: unresolved metadata-only references are now probed in the NuGet global package cache after normal build output/shared-runtime probing. This specifically addresses references such as `System.Formats.Nrbf` without adding a synthetic LocalGPT runtime package dependency, while the release still requires zero unresolved DocFX assembly references.

See `CHANGELOG-v3.3.5-DOCUMENTATION-SOURCE-PACKAGE-REPAIR.md` and `VALIDATION-v3.3.5-source.md`.
