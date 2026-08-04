# DevExpress asset boundary

LocalGPT uses proprietary DevExpress packages and controls. The repository license does not grant a DevExpress license and the source package must not contain private NuGet-feed credentials, license files, generated runtime-license keys, or DevExpress binaries.

## Developer setup

1. Configure the DevExpress NuGet feed on the licensed developer/build machine.
2. Restore the solution with the DevExpress version declared in `LocalGPT.csproj`.
3. When DevExtreme browser components require a runtime key, generate it with the official DevExpress tooling for that licensed environment.
4. Save the generated browser script as `LocalGPTWebviewWrapper/LocalGPT/wwwroot/js/devextreme-license.js` only in the local working tree or release staging directory.
5. Never commit that generated script. The repository contains `devextreme-license.example.js` only as a location marker.

The generated runtime script may contain customer-linked metadata even when it is intended for client delivery. Treat it as a build artifact, not project source.

## Font assets in shared source archives

Binary font files are not included in the repaired source archive. Restore legally obtained font assets from the original checkout or their upstream packages before building a release that depends on them. The Git patch does not delete unchanged font assets from an existing clone.
