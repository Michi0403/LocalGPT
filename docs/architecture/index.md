# Architecture

LocalGPT is a modular monolith: one deployable local application with explicit service boundaries, provider adapters, persistence, Blazor UI, and optional host wrappers.

## Read in this order

1. [System overview](system-overview.md)
2. [AI Host control plane](ai-host.md)
3. [Chat and Council runtime](council-runtime.md)
4. [Projects, workspaces, and persistence](project-data.md)
5. [Organic 1-Wire and security](onewire-security.md)
6. [Frontend, DevExpress, and themes](frontend-and-themes.md)

## Dependency direction

```
flowchart TD
    UI[Components and controllers] --> SVC[Application services]
    SVC --> CONTRACTS[Interfaces and business contracts]
    SVC --> ADAPTERS[Provider / transport / tool adapters]
    ADAPTERS --> EXT[External runtimes and devices]
    SVC --> DATA[EF Core persistence]
    DATA --> DB[(SQLite)]
```

Components coordinate user interaction. Services own operations. Adapters own protocol details. Persistence owns durable state. Static helpers may format or parse immutable data, but application-owned mutable state belongs to scoped, singleton, or hosted services with explicit lifetimes.

##### Important

Architecture documents describe boundaries; they do not authorize execution. The current user interaction remains the authority source.
