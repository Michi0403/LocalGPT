# LocalGPT Capability Snapshot

LocalGPT is a local-first AI engineering workbench. It keeps the main work on the
machine, uses Ollama or other configured providers, and gives the model practical
tools instead of only chat text.

## What It Can Do Now

- Run DXAiChat with Ollama profiles, visible thinking parsing, SQLite memory, and
  resumable conversations.
- Run an AI Council where selected models discuss, correct each other, log the
  roster, save memory, and ask for user decisions when architecture is unclear.
- Give offline models source-backed engineering memory: Microsoft .NET and C#
  compiler docs, Windows developer docs, DevExpress/Bootstrap guidance, EF and
  business-object rules, local project architecture fingerprints, and setup logs.
- Generate safe downloadable artifacts through HTTP links: `.cs`, `.razor`,
  `.dll`, whole .NET solution zips, AI-host control-plane zips, and Minecraft
  datapack zips.
- Build Minecraft Java workspaces for datapacks, Paper plugins, Fabric mods, and
  NeoForge mods. Current datapack guidance targets Minecraft Java 26.1; 1.21.x
  remains available for legacy comparison and loader starter work.
- Let users inspect and edit SQLite memory, council knowledge, logs, and live
  database tables from the frontend.
- Use Test Lab and diagnostic routes to verify features without loading a heavy
  GPU model first.

## Guardrails

- Local-first is private by default, not magically risk-free.
- Native commands stay behind backend services and workspace policy.
- Generated code is sandboxed until the user approves integration.
- Large local models should run one at a time by default on consumer GPUs.
- Claims must match evidence: build output, diagnostic route output, package
  smoke output, or explicit user verification.

## Best Use

Use LocalGPT when you want a local assistant that can remember project context,
understand Windows/.NET/DevExpress work, create downloadable artifacts, and help
turn a prompt into a buildable milestone without sending everything to a cloud
agent by default.
