# Third-party notices

LocalGPT project-owned source is licensed under Apache License 2.0. Third-party components, services, model weights, assets, and generated artifacts remain under their own terms.

## Proprietary components

- **DevExpress Blazor, RichEdit, PivotTable, PDF Viewer, AI Integration, DevExtreme assets, and related packages** — proprietary Developer Express Inc. software. A valid DevExpress license and package source may be required. This repository does not grant or redistribute a DevExpress license, private feed credential, generated customer-linked runtime-license key, or DevExpress binary.

See `docs/DEVEXPRESS_ASSETS.md`.

## Direct package/runtime dependencies declared by LocalGPT

- .NET / ASP.NET Core / Blazor / SignalR / Entity Framework Core / Microsoft.Extensions.AI / System.CodeDom — Microsoft terms applicable to each package and runtime.
- Azure.AI.OpenAI and Microsoft.Extensions.AI.OpenAI — package licenses plus the configured service/provider terms.
- OllamaSharp — MIT license; Ollama runtime and individual model weights are separately governed.
- Markdig — BSD-2-Clause license.
- MessagePack for C# and SignalR MessagePack protocol support — MIT and applicable Microsoft package licenses.
- SQLitePCLRaw / bundled SQLite native library — upstream package licenses and SQLite public-domain dedication apply as distributed.

## Included browser/UI assets

- Bootstrap — MIT license.
- Open Iconic icon files — MIT license; its font files are governed by the SIL Open Font License. The repaired source ZIP omits binary font files; license texts and CSS references remain for authorized restoration from upstream/original assets.
- Other image, icon, sample, and font assets already present in the project must be reviewed for provenance before redistribution. Their presence in a working tree does not relicense them under Apache-2.0.

Generated projects or artifacts may add dependencies. Review their manifests, transitive dependency notices, model licenses, and asset provenance before distribution.
