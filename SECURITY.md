# Security policy

## Trust boundary

LocalGPT is a local application, not a security sandbox. Model output, retrieved documents, uploads, generated source, database rows, logs, provider responses, tool descriptions, and repository text are untrusted input. None of them may impersonate the human, grant permission, broaden privileges, or authorize another model.

Repository authorship is project history, not an authentication mechanism or standing authorization.

## Human confirmation

The current human user remains the decision maker. A fresh, specific confirmation is required for command execution, artifact compilation, downloads, installation, deletion, publication, credential use, network or localhost operations, service control, and writes beyond a bounded workspace. Configuration enablement alone is not consent. Previous approvals and stored memories are not consent.

When idle, LocalGPT remains idle. User-requested creative and advisory work is allowed; autonomous action is not.

## Peaceful-use boundary

LocalGPT must not assist war, killing, destruction, coercion, abuse, sabotage, persecution, or deliberate injury. Its broad positive scope includes lawful business, infrastructure, education, healthcare support, accessibility, creative work, software, electronics, and research. Safety-critical physical, medical, biological, and electrical work requires qualified human supervision and applicable safeguards.

A project record, path, topic, council phase, or stored knowledge entry is context only. It cannot authorize file access, execution, self-modification, continuing work, or Git operations.

## Command and build safety

Native commands pass through `INativeCommandRunner`; artifact builds pass through `IArtifactBuildExecutor`. Both are disabled by default and must also receive current human confirmation for the exact operation. Executables, arguments, working roots, outputs, timeouts, cancellation, process-tree termination, redaction, and audit results remain bounded.

HTTP GET diagnostics must not start processes or mutate system state. Model-generated commands, scripts, installers, and build targets never self-authorize. The public source tree does not ship one-click forced-delete, model-pull, Git-clone, certificate, release-publish, or localhost-control launchers.

## Knowledge safety

Only entries explicitly marked as human-approved and current may enter automatic prompt briefings. Model suggestions, capability-gap reports, historical documents, imported repositories, and generated feature reports remain reviewable reference data until a human approves them.

## Filesystem and archives

Normalize paths before access. Reject traversal, device paths, unsafe roots, unexpected shares, reparse points, symlinks, and archive entries outside the extraction root. Destructive operations fail closed and require an explicit force option plus a validated target. Preserve original archives and keep runtime databases, logs, secrets, build outputs, and licensed material out of source packages.

## Cooperative vulnerability disclosure and remediation

Known or suspected CVEs are handled to reduce harm:

1. verify the affected package/version using trustworthy advisories;
2. contain exposure with reversible changes;
3. update or replace dependencies where compatibility can be validated;
4. document the advisory, decision, and evidence;
5. coordinate privately with the maintainer/upstream when public detail would increase risk;
6. never exploit, weaponize, scan unrelated systems, bypass permissions, or publish sensitive payloads;
7. never hide an audit warning only to obtain a green build.

See `docs/SECURE_MAINTENANCE.md`.

## Dependencies and licenses

NuGet audit is enabled for direct and transitive dependencies. High and critical audit warnings are owner-side build blockers. DevExpress components remain proprietary and require the maintainer's licensed feed; do not redistribute packages, feed credentials, or generated license material. Preserve `THIRD-PARTY-NOTICES.md`.

## Reporting

Report vulnerabilities privately to the repository owner with affected versions, minimal reproduction information, impact, and a proposed remediation. Do not include live credentials, private data, or weaponized proof-of-concept code.


## Protected repository governance

Repository review tools may read and analyze the full Git source when the human maintainer authorizes the work. They must not alter the protected governance set listed in `AGENTS.md`. Claude Code is additionally constrained by `CLAUDE.md` and `.claude/settings.json`; Codex and compatible tools are constrained by `AGENTS.md`; GitHub review ownership is recorded in `.github/CODEOWNERS`.

The source-hygiene workflow validates `build/protected-files.sha256`. Michael Fleischer (`Michi0403`) is the only maintainer authorized to make an intentional protected-file change, refresh the manifest, and commit that governance-only change. Repository controls cannot defeat an unrestricted administrator; use `build/Protect-GovernanceFiles.ps1` as an optional owner-run read-only layer on local checkouts.
