# LocalGPT 7.1.0

**Release-build correction.** The Windows x64 setup executable in the final GitHub release bundle is now taken from the same version-scoped staging payload used to build `setupwinx64.zip`. The previous 7.0.9 build stopped after all Windows publishes because it searched the obsolete `.pubxml` output location.

The independently versioned installer is now **5.0.2**. The 7.0.9 PublisherStudio-derived installation workflow, app functionality, canonical install directories, and macOS/Linux native packaging code are retained.

See `maintenance/changelogs/CHANGELOG-v7.1.0-RELEASE-BUNDLE-SETUP-ENTRYPOINT.md`. The complete cross-platform release and installation paths have **not** been runtime-verified in this environment.
