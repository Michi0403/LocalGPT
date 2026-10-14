# LocalGPT 4.7.0

## Repository-aware workspace intake, OCR and controller modes

- Preserves the 4.6.9 idle-invisible Chat drag/drop surface. External file drag is the only event that projects the landing overlay; normal Chat remains unchanged while idle.
- Holds the successful workspace-analysis animation briefly after drop so processing is visible instead of disappearing immediately.
- Recognizes LocalGPT, PublisherStudio and generic Git repository evidence in direct workspaces and bounded ZIP inspection, including `.git/config`, while excluding `.git` internals from tracked source/Knowledge content.
- After the existing independent-review and explicit-promotion gate, synchronizes a promoted repository into the canonical project/revision model, links it to the previous revision, computes added/changed/removed source evidence, and stages review-required Knowledge plus repository-identity regex candidates. Nothing is silently trusted or approved.
- Routes text/source, bounded ZIP/source archives and images through LocalGPT-native processing. Office and richer media are offered only when a connected PublisherStudio advertises the matching live 1-Wire format/runtime capability.
- Adds bounded workspace-image OCR architecture: safe workspace path resolution, Ollama/DeepSeek OCR capability probing, reusable service contract, HTTP controller and discoverable DX functions. Arbitrary server paths are rejected.
- Allows an explicit OCR model to use a configured Ollama host without forcing that model to become the normal Chat model.
- Adds additive controller **Control** and **Cursor** modes while preserving keyboard, mouse/touchpad, pointer and existing context-menu ownership. Cursor mode provides analog pointer motion, activation and context-menu invocation; native pointer input remains available for Steam Deck hybrid use.
- Game-console controller actions respect application Cursor mode instead of competing with pointer navigation.
- Orders regex-curator review metadata before regex persistence so interruption cannot leave a newly suggested pattern looking legacy-approved.

## Validation scope

Source-only validation is used for this source ZIP. No GitHub access, `dotnet`, restore, NuGet, MSBuild, build, publish or installer execution is used in this environment.
