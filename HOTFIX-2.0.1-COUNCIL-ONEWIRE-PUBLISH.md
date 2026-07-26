# LocalGPT 2.0.1 Council, 1-Wire and publish hotfix

This source candidate repairs the runtime failures reported after the DI-startup repair. It is intentionally source-only because the delivery environment does not contain the .NET SDK.

## Council recovery

- Serializes DX function-catalog synchronization across application scopes.
- Refreshes runtime capability artifacts through a fresh `DbContext` and retries one stale-write conflict.
- Treats the persisted capability directory as a derived cache: failure to refresh it becomes a Council warning instead of aborting the Council run.
- Keeps the live DI function registry and current organic-skill list authoritative.
- Reduces database-backed log persistence to warnings and errors while retaining normal structured `ILogger` instrumentation.

## LocalGPT ↔ PublisherStudio 1-Wire recovery

- UDP discovery now sends only compact peer/endpoint metadata.
- Same-machine discovery also sends a loopback beacon.
- Full capabilities, skills, UI features, and hardware are requested over the approved TCP link.
- The maximum TCP protocol message is 8 MiB; discovery datagrams are capped at 32 KiB.

## Publish repair

- The protocol class library no longer packs automatically during a normal application build.
- Application publish properties such as `RuntimeIdentifier` and `SelfContained` are not propagated into the pack-only protocol project.
- A normal DLL-backed `LocalGPT.WireProtocolVersion.2.0.0.nupkg` is generated before the publish manifest is finalized and copied to `protocol/`.

## Maintainer verification

From the repository root, clean stale assets once and then build/publish with the RID you actually need:

```powershell
dotnet clean .\LocalGPTWebviewWrapper\LocalGPT\LocalGPT.csproj -c Release
Remove-Item .\LocalGPTWebviewWrapper\LocalGPT\bin, .\LocalGPTWebviewWrapper\LocalGPT\obj -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\LocalGPTWebviewWrapper\LocalGPT.WireProtocolVersion\bin, .\LocalGPTWebviewWrapper\LocalGPT.WireProtocolVersion\obj -Recurse -Force -ErrorAction SilentlyContinue

dotnet restore .\LocalGPTWebviewWrapper\LocalGPT\LocalGPT.csproj -r win-x64
dotnet publish .\LocalGPTWebviewWrapper\LocalGPT\LocalGPT.csproj -c Release -f net10.0 -r win-x64 --self-contained true
```

Replace `win-x64` with `linux-x64`, `linux-arm64`, `osx-x64`, or `osx-arm64` for the target release. Do not publish the protocol project itself with an application RID.

After startup, verify that a Council request proceeds even when the capability-directory cache reports a warning, and that PublisherStudio discovers LocalGPT, requests frontend approval, then receives the complete capability directory over TCP.
