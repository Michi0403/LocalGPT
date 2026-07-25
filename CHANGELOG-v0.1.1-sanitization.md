# LocalGPT v0.1.1 security sanitation

The semantic version remains **v0.1.1**. This pass reviews the owner's newly built dependency update without presenting the sanitation work as a new product release.

## Human–AI boundary

- Preserved Michael Fleischer / Michi0403 as original developer in README, package metadata, and project-history documentation.
- Clarified that attribution, remembered context, prior consent, identity, model strength, or a database record never grants permanent authority or permission.
- Added a human–AI collaboration contract: LocalGPT remains idle without a request; harmless creative assistance is welcome when requested; consequential operations require fresh, specific human confirmation.
- Defined each AI Council phase as one bounded contribution inside a current user-directed run, not an autonomous agent or continuing process.
- Replaced old tool/person-specific operational directives with neutral, reviewable engineering history.

## Peaceful and constructive use

- Added a peaceful-use covenant for business, infrastructure, hospitals, schools, accessibility, children's learning, music, art, software, electronics, ESP/PCB work, assistive technology, and other lawful constructive projects.
- Prohibited war, killing, destruction, coercion, sabotage, abuse, persecution, deliberate harm, and autonomous harmful action.
- Kept safety-critical medical, biological, electrical, and physical work advisory and subject to qualified human oversight, applicable law, and domain safeguards.

## Knowledge safety

- Replaced wildcard runtime documentation seeding with an explicit reviewed allowlist.
- Automatic knowledge briefings now require non-archived, unexpired, explicitly user-approved knowledge with current review state and source-backed or user-verified provenance.
- Model/council suggestions are stored as `NeedsUserReview` and are not automatically approved.
- Retired the raw SQL knowledge seed from runtime output.
- Sanitized historical capability/workflow documents so attribution is not interpreted as authority, identity, consent, or permission.

## Projects and council cooperation

- Added database-backed projects with purpose, optional path text, versions, topics, and reviewable links to council knowledge.
- Treats stored project paths as metadata only; selecting a project does not access its filesystem path.
- Added bounded project context to council runs and optional linking of generated knowledge to a selected topic.
- Project writes, council artifact generation, and project-topic linking require separate fresh confirmations and reset after one request, including failure paths.
- Exposed only read-only `GET` project lookup through the AI function catalog; project mutations remain explicit UI/API actions.
- Git is recommended only. The project feature does not initialize, stage, commit, reset, clean, push, alter remotes, or enforce repositories.

## Execution and diagnostics safety

- Added fresh human-confirmation gates to native commands and artifact builds.
- Kept native commands, PowerShell workspace scripts, and artifact builds disabled by default with separate configuration switches.
- Prevented read-only diagnostics and automatic council/benchmark paths from silently launching processes.
- Disabled the generic function-invocation endpoint; AI-visible function descriptions are restricted to bounded read-only `GET` operations.
- Replaced diagnostic HTTP responses that exposed exception text, local paths, request objects, and service details with generic failures.
- Removed raw request, prompt, generated-content, and result-object serialization from exception logs in the reviewed council/artifact/workspace paths; logs keep the exception and operation name only.
- Updated artifact-workspace briefings so writes and ZIP refreshes explicitly require one current human confirmation and cannot be inferred from generated text or a prior run.
- Removed broad instructions to process all tasks or revise every file.
- Changed council artifact generation to off by default and prevented query-string activation.

## Installer safety

- Changed no-argument startup to show help and perform no installation.
- Removed raw command-line logging and the default pull of an uncensored model.
- Made failed downloads, target discovery, filename sanitation, Windows checks, and critical path resolution fail closed.
- Required an exact platform, architecture, and setup-mode release asset; no arbitrary fallback package is accepted.
- Required HTTPS GitHub browser-download URLs.
- Added safe deletion-target, Start Menu path, archive traversal, and archive symlink validation.
- Removed external archive-extraction fallback execution.
- Corrected force-delete ordering and uninstall launcher arguments.
- Preserved the learning-base directory during uninstall, including forced uninstall.

## Dependency and CVE policy

- Preserved the owner's updated .NET 10.0.10, MessagePack 3.1.8, System.Security.Cryptography.Xml 10.0.10, EF Core 10.0.10, and related dependency references.
- Added repository-wide authorship/license metadata and NuGet auditing for direct and transitive packages.
- High and critical NuGet advisories are owner-side build blockers.
- Added cooperative CVE handling rules: verify, contain, patch or replace, document, validate, and never exploit, weaponize, scan unrelated systems, or suppress findings merely to make a build green.

## Compiler and workflow hygiene

- Removed every `using static System.Net.WebRequestMethods;` import and fully qualified the reported `System.IO.File.Exists` call to prevent `CS0104` namespace collisions.
- Added a source guard that rejects that static import before compilation.
- Corrected the theme-switcher component's false async/disposal template: initialization is synchronous and disposal releases its notifier reference instead of retaining a stale component.
- Kept production detailed errors disabled.
- Added security-policy and dependency-audit scripts for owner-side validation.
- Generated IDE/build/runtime artifacts are excluded from the sanitized package.

## Public/private knowledge separation

- Kept original-developer attribution in README, license, package metadata, and a non-runtime provenance document.
- Removed personal names from active runtime bootstrap prompts, AI-facing architecture summaries, and coding-assistant instructions.
- Excluded `docs/PROJECT_IDENTITY.md` from automatic database knowledge seeding so public attribution cannot become personalized model authority.
- Left existing private/local runtime databases untouched and excluded every database file from the public source package.
- Replaced the historical all-in-one PowerShell installer/model/repository bootstrap with a fail-closed notice because its side effects were too broad for a safe repository helper.
- Removed automatic maintainer attribution from unrelated generated Minecraft artifacts.

## Public action-script cleanup

- Removed historical release, certificate, Git-clone, localhost-test, model-pull, and environment-repair scripts from the public source tree.
- Removed one-click installer `.cmd` launchers, especially combinations containing forced deletion, downloads, learning-base imports, model pulls, or automatic startup.
- Removed a hard-coded certificate password by removing the obsolete publishing script that contained it.
- Kept only non-destructive source formatting, security-policy, and dependency-audit helpers.
- Added documentation directing maintainers to review and run consequential owner-side commands manually.
