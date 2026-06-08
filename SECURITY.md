Don't use it with AI Agents

# Security Policy

LocalGPT is designed as a local-first AI workbench. In the intended desktop mode, a
WinUI 3/WebView2 shell hosts the ASP.NET Core and Blazor app on loopback and talks
to local model hosts such as Ollama.

That is a real privacy advantage: code, prompts, chat memory, logs, generated
artifacts, and knowledge data can stay on the user's machine. It is not the same
as having no security concerns. LocalGPT has a smaller external exposure surface
by default, but it still has a powerful local execution surface.

## Intended Use

- Single-user desktop/WebView2 usage.
- Loopback-only ASP.NET Core hosting.
- Local model hosts such as Ollama or LM Studio unless the user configures a
  cloud provider.
- User-owned SQLite memory, council knowledge, logs, and generated artifacts.
- Backend-owned native command execution through explicit service and policy
  boundaries.

## Do Not Expose The Local Server Casually

Do not bind the ASP.NET Core server to `0.0.0.0`, a public interface, a VPN, or an
untrusted network unless you intentionally harden the application as a normal web
application.

If LocalGPT is hosted for coworkers or outside the local desktop boundary, the
threat model changes. Add and verify at least:

- authentication and authorization
- TLS and safe reverse-proxy configuration
- CSRF protection for browser-facing state changes
- rate limits and request-size limits
- audit logs
- command restrictions per user and workspace
- workspace isolation
- secrets management
- backup and retention rules for the SQLite database

## Native Commands And Generated Code

LocalGPT can help create projects, scripts, datapacks, and build commands. Treat
that as trusted-local automation, not as a security sandbox.

- Inspect generated scripts and projects before running them.
- Keep command execution behind backend services such as `INativeCommandRunner`.
- Restrict native commands to approved workspaces and allowlisted tools.
- Log command attempts, outputs, policy decisions, and artifact paths.
- Do not grant generated code permission to modify LocalGPT itself unless the user
  explicitly approves that integration step.

## AI Providers And Data

Local providers are preferred for private company code. Cloud providers are
optional and user-configured. If a cloud endpoint is enabled, prompts, code
fragments, logs, memory summaries, and generated artifacts may leave the machine
according to that provider's policy.

The local SQLite database can contain sensitive project information, chat history,
model thoughts, imported knowledge, diagnostics, and command logs. Back it up,
delete it, or share it with the same care as source code and internal project
notes.

## Knowledge Imports

Imported knowledge should be trusted, reviewed, or marked as unverified. Bad or
stale knowledge can influence generated code and setup advice even when no remote
attacker is involved.

Use the knowledge database approval fields to distinguish:

- source-backed seed knowledge
- user-verified knowledge
- model-suggested knowledge
- archived or rejected knowledge

## Reporting Security Issues

Do not post secrets, private code, tokens, logs, database files, or generated
artifacts containing confidential data in a public issue.

If GitHub private vulnerability reporting is available for the repository, use
that. Otherwise, contact the maintainer with a minimal description and keep
sensitive details out of public discussion until a fix path is agreed.

## Security Claim

A precise claim for this project is:

LocalGPT is safest when used as a single-user local desktop app. It avoids the
main cloud-AI risk by default: sending code, prompts, logs, and project memory to
an external provider. Its remaining risk is local capability risk: file access,
native command execution, generated scripts, and trusted knowledge management.
