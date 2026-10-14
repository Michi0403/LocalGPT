# LocalGPT 3.8.8 source validation

This release repairs the missing Council knowledge seed-manifest build contract without introducing a second database seeding authority.

Validation targets:
- `docs/COUNCIL_KNOWLEDGE_SEED.sql` exists in the source tree and is copied by `LocalGPT.csproj`.
- the release prerequisite checks that file before expensive work;
- the file is explicitly non-mutating/non-auto-executed;
- Council knowledge remains seeded by the existing C# `InitialDataCatalog` / `DatabaseInitializationService` pipeline;
- the eight established `AddHostedService<T>` registrations remain intact;
- no 3.8.5/3.8.6 post-listen coordinator lifecycle is present.
- the complete C# and Razor XML documentation quality gate passes after documenting the provider/path APIs added in the 3.8.3/3.8.4 line.
