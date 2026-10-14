# LocalGPT 4.6.2 — project ingestion, 1-Wire team workflow, curator and semantic automation

LocalGPT 4.6.2 builds on the repaired 4.6.1 startup baseline and completes the project-ingestion/orchestration work requested after that hotfix. It does not bypass normal 1-Wire pairing: peer discovery, trust establishment and Council-host enrollment remain behind the existing 1-Wire security contract and explicit user decisions.

## Quarantine-first project ingestion

- Chat uploads now remain in an `original` quarantine tree until an ingestion gate has inspected them; ZIP payloads are no longer extracted as part of the initial upload operation.
- Raw-file count, per-file byte and total-byte limits are enforced again, alongside bounded ZIP entry count, individual expanded-entry and total expanded-size limits.
- Archive paths are normalized and constrained to the quarantine root before promotion.
- The gate records SHA-256 evidence, type hints, approved regex matches, approved knowledge hints, rejection reasons and independent review evidence in a durable `ingestion-gate.json` record.
- Promotion requires deterministic checks, the configured independent peer-review quorum and explicit user approval before files are copied/extracted into the promoted workspace.

## Hash-verified file/blob reconstruction

- Added bounded multi-file reconstruction sessions for file blobs transported as ordered base64 chunks.
- Each declared file carries a safe relative path, byte length, chunk count and SHA-256; a canonical manifest has its own SHA-256.
- Finalization rejects missing/duplicate/out-of-range chunks, path escapes, byte-length mismatches, digest mismatches and aggregate-size violations, then places reconstructed material into the same quarantine gate instead of making it immediately executable.

## Regex curator lifecycle

- Regex patterns now carry provenance, usage scope, review status, user-approval state, peer-review count, review notes and reviewer timestamps.
- Independent regex review records are durable database entities.
- AI/learning suggestions enter `NeedsReview` instead of becoming authoritative immediately.
- Only curator-approved patterns are consumed by security-sensitive project ingestion/classification.
- Reviewed regex evidence used during repository classification is persisted with the project so project/toolchain/game recognition has durable provenance.

## Generic project and toolchain recognition

- Repository classification now recognizes common .NET, Maven/Gradle Java, Node/npm/pnpm/yarn, Rust/Cargo, Go, Python and Minecraft/Fabric/Forge/NeoForge/Paper evidence without executing source content.
- Classification combines manifest/file evidence, curator-approved regexes and approved knowledge rather than assuming every repository is a `.csproj` tree.
- Missing framework/toolchain knowledge becomes a bounded human-collaboration question asking for exact versions, setup/build/publish commands and compatibility constraints; supplied knowledge remains part of the existing knowledge/review system.

## Prompt-visible normal 1-Wire team workflow

- Added AI/user functions and controller paths for 1-Wire status, normal pairing-ticket creation, trust establishment/revocation, trusted Council-host enrollment/removal and host listing.
- Council-host enrollment is accepted only for already trusted, unexpired peers; no function silently enables LAN transport or bypasses 1-Wire pairing/security.
- User-enrolled trusted LocalGPT work hosts are surfaced to the Council orchestration context for solo/team work distribution.

## Shared semantic ASCII interaction

- Added one semantic action contract shared by keyboard, gamepad, pointer/touch controls and approved AI/team invocation.
- ASCII/game buttons expose semantic-action metadata and pointer press state; AI calls resolve the same authoritative legal action set instead of synthesizing operating-system mouse coordinates.
- Semantic action listing/invocation is available through DX AI functions and a controller for the regular Chat/Council/ASCII workflow.

## Baseline repairs retained

- The 4.6.1 singleton/scoped service-lifetime repair remains enforced.
- The 4.6.0 Council rejoin compiler repair and UI/editor corrections remain intact.
- The 4.5.9 DevExpress popup, freshness, tournament and related behavior remains intact.

PublisherStudio source is unchanged by this LocalGPT-only release.
