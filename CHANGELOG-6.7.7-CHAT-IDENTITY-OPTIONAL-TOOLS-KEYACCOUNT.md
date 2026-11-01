# LocalGPT 6.7.7 — Chat identity, optional tool inputs and Keyaccount review

This is an additive **source repair candidate** based on 6.7.6; not a host-verified runtime release.

## Chat/Council recovery

- Persisted Chat conversation IDs and Council run IDs are separate browser-tab recovery identities. Automatic reload carries both in the URL.
- Chat initialization loads the explicitly requested SQLite conversation before restoring the server-owned Council run; it does not silently choose an unrelated latest conversation.
- When the restored conversation belongs to a non-Council provider session, initial Council attachment transfers the same messages into the displayed Council session. The existing conversation ID is retained; no second Chat conversation is intentionally created.
- Writes from the interactive Chat component are serialized to prevent concurrently launched saves from racing to create separate IDs.
- The preceding automatic reload/rejoin safeguards and human Stop behavior remain unchanged.

**Limit:** Messages not yet durably saved at the exact moment of a circuit failure cannot be restored from SQLite. The underlying ASP.NET Core `TryClaimPendingStream` failure remains undiagnosed; the recovery improvements do not fix the stream producer.

## Local AI image tools

- `negativePrompt` remains **optional** in both the image generation and image edit function schemas. A schema default of empty string is advertised.
- An omitted `negativePrompt` already passed schema validation; an explicit JSON `null` is now treated as an empty exclusion set when the field is optional. The handler normalizes null to empty before runtime dispatch.
- `modelInstallationId`, `prompt`, required workspace paths, scope checks and human approvals remain mandatory.
- No changes to user-defined function schemas or weakening of unrelated function validation are implied.

## Goal Keyaccount JSON reviewer

- Replaced the first-`{`/last-`}` JSON substring extraction with a bounded single-pass, quote/escape-aware object scanner.
- A review object is accepted only when valid JSON contains a boolean `complete` field.
- Failed or malformed reviews remain incomplete and require human review; Council receipts are preserved. No model output can substitute for tool/artifact evidence.

## Build and scope

- The existing build-wired PowerShell Council guard validates the new recovery, optional-prompt and reviewer source contracts.
- Retains the seven LocalGPT runtime identifiers and pre-existing build policies.
- No database schema or migration changes.

## Acceptance not yet demonstrated

1. `dotnet build` and PowerShell validation on Windows, Linux and macOS.
2. Live Council disconnect/reload with original chat text visible and new user messages appended to the **same** SQLite conversation ID.
3. Real image generation and edit calls with omitted and explicit-null `negativePrompt` through Ollama tool metadata, approvals and runtime binding.
4. Goal reviewer outputs: clean JSON, fenced JSON, trailing prose, two JSON objects, malformed pseudo-JSON and cancellation.
5. Attribution and elimination of the original `TryClaimPendingStream` connection failure.

None of these host-side tests were run in this environment.
