# Third-party notices

LocalGPT project-owned source is licensed under Apache License 2.0. Third-party components, services, model weights, assets, and generated artifacts remain under their own terms.

## Proprietary components

- **DevExpress Blazor, RichEdit, PivotTable, PDF Viewer, AI Integration, DevExtreme assets, and related packages** — proprietary Developer Express Inc. software. The maintained source build requires an appropriately licensed DevExpress development environment. Current .NET package restore uses NuGet.org; the developer license identity is a separate build-time requirement. This repository does not grant or redistribute a private DevExpress developer license or credential. Any DevExpress runtime redistribution remains governed by DevExpress's own terms.

See `docs/architecture/frontend-and-themes.md`.

## Direct package/runtime dependencies declared by LocalGPT

- .NET / ASP.NET Core / Blazor / SignalR / Entity Framework Core / Microsoft.Extensions.AI / System.CodeDom — Microsoft terms applicable to each package and runtime.
- Azure.AI.OpenAI and Microsoft.Extensions.AI.OpenAI — package licenses plus the configured service/provider terms.
- OllamaSharp — MIT license; Ollama runtime and individual model weights are separately governed.
- Markdig — BSD-2-Clause license.
- MessagePack for C# and SignalR MessagePack protocol support — MIT and applicable Microsoft package licenses.
- SQLitePCLRaw / bundled SQLite native library — upstream package licenses and SQLite public-domain dedication apply as distributed.
- DocFX — MIT-licensed documentation build tool restored through the repository-local .NET tool manifest. It is used to generate HTML/API documentation and a versioned PDF; it is not part of LocalGPT's runtime authority model.

## Included browser/UI assets

- Bootstrap — MIT license.
- Open Iconic icon files — MIT license; its font files are governed by the SIL Open Font License. The repaired source ZIP omits binary font files; license texts and CSS references remain for authorized restoration from upstream/original assets.
- Other image, icon, sample, and font assets already present in the project must be reviewed for provenance before redistribution. Their presence in a working tree does not relicense them under Apache-2.0.

Generated projects or artifacts may add dependencies. Review their manifests, transitive dependency notices, model licenses, and asset provenance before distribution.


## Optional open-source learning sources and game configuration studies

- `id-Software/DOOM` may be downloaded only when a user explicitly selects that optional knowledge source. LocalGPT uses it as a source-code architecture reference for an original ASCII/runtime-class configuration. The original repository's license and notices apply. Commercial DOOM data files, WADs, trademarks, artwork, sounds, levels, and the original engine runtime are not included.
- `lotgd/lotgd` may be downloaded only when a user explicitly selects that optional knowledge source. Its own license and notices apply. LocalGPT's Green Dragon runtime story is an original configuration example and is not an official LOTGD distribution.

LocalGPT is not affiliated with, endorsed by, or sponsored by id Software, ZeniMax, Bethesda, LOTGD, or their contributors. Names are used only to identify optional upstream source references selected by the user.
