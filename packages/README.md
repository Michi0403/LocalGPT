# Local protocol package drop

Place `LocalGPT.WireProtocolVersion.2.1.0.nupkg` here to build with the released package instead of the source project.

```powershell
dotnet restore .\LocalGPTWebviewWrapper\LocalGPT\LocalGPT.csproj -p:UseLocalWireProtocolProject=false
dotnet build .\LocalGPTWebviewWrapper\LocalGPT\LocalGPT.csproj -c Debug -p:UseLocalWireProtocolProject=false --no-restore
```

Normal source development defaults to the synchronized project reference and does not require this file.
