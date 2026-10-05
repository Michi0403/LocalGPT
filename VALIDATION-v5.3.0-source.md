# LocalGPT 5.3.0 source validation

## Scope

Upload evidence, safe quarantine extraction, source-backed learning, progressive whole-file reading, and non-game Council DXFunction availability repair.

## Confirmed source checks

- LocalGPT project and browser cache-buster identity is aligned at **5.3.0**.
- ZIP uploads remain quarantined as originals while bounded path-safe extraction is created immediately under the workspace `extracted/` tree for read-only evidence.
- Immediate extraction retains archive-entry count, per-entry byte, aggregate extracted-byte and workspace-root/path-traversal checks, uses create-new output semantics, and does not execute, build, publish, promote, trust or write back into the uploaded repository.
- `chat.upload_workspace_file` and `chat.upload_workspace_context` expose substantial progressive reads with `CharacterOffset`, `CharactersReturned`, `HasMore` and `NextOffsetCharacters`; a 2,000-character request is clamped to the maintained 64,000-character minimum and no longer defines completion.
- The maintained Council DXFunction result default is 1,100,000 characters so substantial file results are not immediately cut back to the historical 32,000-character default. Only the untouched historical `32000` persisted value is evolved; user-edited policy values are preserved.
- Text-gateway continuation evidence scales with the selected model's effective context instead of a fixed 24,000-character boundary.
- Extracted repository files are labeled source-backed read-only evidence. Learning stages can inspect them immediately, and the final `localgpt.learning.maintain` stage is instructed to synchronize repository-shaped project/version/revision/tracked-file evidence while promotion remains separate.
- Non-game Council workflows receive the complete registered policy-approved DXFunction catalog when automatic/native functions are enabled. Team/step function lists remain guidance outside games.
- Council teams containing `games.*` runtime classes retain strict hard allow-list semantics.
- The maintained Council seed version is **40**, allowing unchanged built-in teams to receive the corrected Learning Round instructions.
- Current Council Teams and upload-advisor wording matches the new runtime policy and no longer tells users/models that all configured function lists are exclusive or that extraction necessarily waits for promotion.

## Source audits executed

The following repository-maintained source audits passed on this source tree:

- application architecture / static policy / C# structure;
- async continuation ownership;
- async-only component/service architecture;
- provider-qualified Council wiring;
- Razor maintenance architecture;
- service resilience;
- cross-platform boundaries;
- DevExpress Blazor control retention;
- transient UI-state ownership; and
- code-generation/DXFunction wiring.

The repository-wide XML documentation audit is not green in the supplied 5.2.9 baseline. The baseline and 5.3.0 working tree produced byte-identical audit output: **2,608 existing C# XML findings plus 21 existing Razor XML findings**, so this change set introduces no additional XML-documentation finding.

## Packaging and maintenance integrity

- No file under `build/` is changed relative to the supplied 5.2.9 source tree.
- Python audit cache artifacts are removed before packaging.
- Historical changelogs and historical validation files are retained unchanged.

## Environment limitation

The .NET SDK/MSBuild and PowerShell runtime are not used in this preparation environment. No restore/build/publish is claimed. No GitHub or online repository access was used. Maintainer builds remain authoritative for compiler/runtime verification.
