# LocalGPT 3.1.0 — .NET 10 toolchain and DevExpress 25.2-line upgrade integration

## User-supplied upgrade source retained

This release starts from the LocalGPT archive supplied after the user's .NET and DevExpress upgrade. It does not replace that tree with an older 3.0.9 source package.

The supplied source carries these .NET 10 upgrade changes:

- `global.json` now selects **.NET SDK 10.0.400**.
- `Microsoft.AspNetCore.SignalR.Client` is **10.0.11**.
- `Microsoft.AspNetCore.SignalR.Protocols.MessagePack` is **10.0.11**.
- `Microsoft.EntityFrameworkCore.Design` is **10.0.11**.
- `Microsoft.EntityFrameworkCore.Sqlite` is **10.0.11**.
- `System.CodeDom` is **10.0.11**.
- the Windows WebView wrapper uses `System.Security.Cryptography.Xml` **10.0.11**.

LocalGPT's DevExpress package references intentionally remain on the existing **`25.2.*`** patch lane. The supplied source therefore permits the upgraded licensed 25.2 patch release to resolve without introducing a new hardcoded patch pin that was not present in the user's archive.

## Database and migration preservation

No database schema or migration change was added while preparing 3.1.0. The complete `src/LocalGPT/Migrations` tree and `DatabaseMigrationCompatibilityService.cs` are preserved byte-for-byte from the user's supplied upgrade archive. This specifically avoids reintroducing database/migration edits the user had already reverted.

The supplied EF Core 10.0.11 product-version metadata that remains in that archive is preserved as-is; 3.1.0 adds no migration IDs, tables, columns, model changes, or migration code.

## Authored documentation source restored

The supplied upgrade archive contained generated help/GitHub Pages products but omitted the authored `docs/` tree. The latest authored LocalGPT DocFX/Kawaii source from the 3.0.9 source baseline is restored so a clean checkout can build the Kawaii documentation normally. Generated `wwwroot/help-docs` remains ignored by `.gitignore` and is recreated from source during the normal documentation target.

## Versioning

The application, installer console, and WebView wrapper move from 3.0.9 to **3.1.0**. Under the maintained versioning rule, the patch slot does not advance to `.10`.

## Preserved behavior

The 3.0.9 all-model five-profile benchmark, live Council lanes, provider-qualified routing, human collaboration semantics, InteractiveServer boundaries, component/service safety rules, ConfigureAwait policy, and 1-Wire protocol **2.1.1** remain unchanged by this toolchain-integration release.
