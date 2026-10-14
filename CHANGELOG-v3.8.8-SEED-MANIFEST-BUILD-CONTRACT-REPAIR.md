# LocalGPT 3.8.8 — seed manifest/build contract repair

- Restores the legacy `docs/COUNCIL_KNOWLEDGE_SEED.sql` path required by the current project and preserved in the configurable `KnowledgeFiles` collection.
- Keeps the authoritative Council knowledge seed ownership in `InitialDataCatalog` and `DatabaseInitializationService`; the compatibility SQL manifest is deliberately non-mutating and is never auto-executed.
- Adds the seed manifest to the clean-source release prerequisite check so an incomplete source archive fails before package/tool/documentation work.
- Retains the 3.8.7 normal eight-hosted-service lifecycle and boot dependency-cycle repair; no post-listen coordinator is reintroduced.
