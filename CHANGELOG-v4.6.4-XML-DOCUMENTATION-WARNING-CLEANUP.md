# LocalGPT 4.6.4 — XML documentation warning cleanup

LocalGPT 4.6.4 is a narrow documentation-quality repair on top of 4.6.3. Runtime behavior, `/install` UI behavior, CanIRun.ai model resolution/install, invariant numeric editors, 4.6.2 project-ingestion/1-Wire/regex/ASCII capabilities, and the 4.6.1 startup-lifetime repair are unchanged.

## XML documentation cleanup

- Added the missing `ProjectType` and `Toolchains` parameter documentation for `LearningProjectWorkspaceSyncService.RepositoryInspection`.
- Added the missing `regexCuratorService` constructor parameter documentation for `LearningRoundService`.
- Added the missing `regexCurator` constructor parameter documentation for `UpsertRegexPatternFunction`.
- Added a release guard that checks those exact XML documentation contracts so the reported `CS1573` warnings cannot silently return.

PublisherStudio source is unchanged.
