# Getting started

## Application shape

LocalGPT is one local application with several cooperating surfaces:

- **Chat** for ordinary provider-backed conversation.
- **AI Council** for role-based multi-model work.
- **Projects** and **Project Maintenance** for durable requirements, revisions, artifacts, workspaces, and toolchains.
- **SQLite Database** for inspected local data and persistent configuration.
- **DX Functions & 1-Wire** for bounded callable functions and peer/device operations.
- **Embedded workbench**, **Minecraft Mod Builder**, **Test Lab**, and other focused tools.
- **Help** for the generated DocFX site and API reference.

The desktop wrapper and web host expose the same backend application. The wrapper is a host, not a second architecture.

## Configure a model provider

Provider identity is more than a model name. LocalGPT addresses a model using:

```text
provider kind + provider name + endpoint + provider-native model name
```

This prevents an Ollama model and an LM Studio/OpenAI-compatible model with the same visible name from being merged accidentally.

A practical first setup is:

1. Configure or discover one local provider.
2. Test its endpoint.
3. Select one discovered model for Chat.
4. Send a harmless request before enabling Council or tool workflows.
5. Add credentials only to the exact endpoint that requires them.

Credentials are not copied to fallback endpoints and are not displayed in documentation or diagnostics.

## Understand local data

LocalGPT stores durable application state in SQLite through EF Core. Depending on the enabled features, this includes projects, revisions, requirements, Council configuration, reviewed knowledge, runtime policies, presets, collaboration requests, and bounded operational records.

The database is not an authority source by itself. A stored row can describe a prior approval or preference, but it cannot manufacture a new approval for a consequential action.

## Read-only work versus execution

LocalGPT separates four stages:

1. **Inspect** — read bounded files, metadata, provider catalogs, logs, or project state.
2. **Plan** — create a reviewable proposal, command plan, firmware plan, or change set.
3. **Approve** — collect the required user decision for the exact target and scope.
4. **Execute** — perform the bounded operation and record a sanitized result.

A failure at any stage remains visible. The application should report the missing dependency, source, permission, compiler, provider, or capability rather than claiming success.

## Theme and documentation preference

The documentation theme selector supports **Light**, **Dark**, and **Auto**. The selection is stored in both browser storage and a first-party cookie named `localgpt-docs-theme`, so it persists when revisiting the GitHub Pages site or the documentation hosted by the local application. The two hosts keep their own preference because browsers isolate storage by origin.

## Next step

Continue with [Chat and AI Council](chat-and-council.md), or jump to [Projects and workspaces](projects-and-workspaces.md) when your goal is code, documents, firmware, or another maintained artifact.
