# LocalGPT 4.7.0 source validation

## Constraints

- The supplied LocalGPT 4.6.9 source ZIP is the baseline.
- No GitHub/network repository content was used.
- No `dotnet`, restore, NuGet, MSBuild, build, publish or installer command was invoked.
- Validation is static/source-level and does not claim a compiled runtime result.

## Verified source contracts

- Version-slot policy and synchronized LocalGPT/WebView/installer source identities are 4.7.0.
- Chat workspace upload remains idle-invisible; external drag/drop owns the transient landing overlay and its success state is intentionally held long enough to observe.
- LocalGPT/PublisherStudio/generic Git repository evidence is detected from bounded source markers including `.git/config`; `.git` remains excluded from tracked project content.
- Promotion synchronizes the canonical project revision, relates the previous revision, computes source delta evidence and stages Knowledge/regex candidates with review-required status.
- Local-native text/source/ZIP/image processing is separated from PublisherStudio-dependent Office/media processing using live 1-Wire `publisher.file.formats` metadata and media runtime state.
- Workspace OCR is path-bounded and exposed through reusable service, controller and DX-function boundaries.
- Controller Control/Cursor modes are additive to existing keyboard/pointer/context systems; native pointer input is not globally prevented and direct game-console controller input yields in Cursor mode.
- Application architecture, async-continuation, service-resilience, DevExpress UI, cross-platform, configurable-behavior, ConfigurationRoot, Chat ASCII and Kernel Creature Tournament policy audits pass.
- Maintained JavaScript parses and its diagnostics manifest is synchronized; JSON and XML/MSBuild source documents parse successfully.
- Final ZIP is CRC/path-safety checked, clean-extracted and byte-compared against the source tree before delivery.
