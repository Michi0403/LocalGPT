# LocalGPT 6.3.2 — default seeding coverage and completeness checks

Base: LocalGPT 6.3.1 source (keeps prior learning, DXFunction approval, Ollama and terminal work).

- Included all 18 mandatory KnowledgeFiles as assembly resources, including root policy documents, so installed copies can seed without a source checkout.
- Added a curated first-run core reference manifest: 33 general original SQL entries plus 9 safe generic built-in defaults recovered from the provided DB export. The original repair SQL remains documentation, never executed at startup.
- Startup fails visibly instead of marking initialization ready when a mandatory document, knowledge record, regex, prompt or system variable could not be created/verified. Verifies durable existence and shipped reference hashes for unchanged seed-owned records.
- Preserves edited user knowledge by checking source ownership and content hash before replacing an existing seeded entry; human unapproval, unpinning and review statuses are not reset by startup.
- Extended the maintained offline Ollama alias fallback with five model tokens present in submitted Council preset configurations; retains direct live provider discovery, manual model IDs and hardware-independent model pulling.
- Added `docs/SEED-AUDIT-6.3.2.md` with classification/exclusions and offline contract tests.

Not included: runtime DB export, machine configuration, chat/log/history, public-test build. No .NET build performed. Fresh install and actual Ollama availability remain to be checked on the user's Windows host.
