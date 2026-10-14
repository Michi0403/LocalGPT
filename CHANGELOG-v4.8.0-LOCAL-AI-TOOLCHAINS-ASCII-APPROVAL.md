# LocalGPT 4.8.0

## GitHub/upstream-first local AI acquisition

- Restores direct upstream/GitHub acquisition as the primary local-AI path instead of treating Hugging Face as the default provider.
- Adds a reviewed known-source catalog with OpenAI Whisper as the first executable Python-backed integration plus direct source entries for OpenAI CLIP, Segment Anything and Real-ESRGAN.
- OpenAI Whisper is installed from the upstream source ZIP through the LocalGPT-managed Python environment; the upstream Whisper package downloads and loads its real model weights without Hugging Face Hub.
- Keeps Hugging Face as an optional provider-bound integration and moves its client dependency into the optional `hub-huggingface` Python package profile.
- Adds approval-gated controller and DXFunction surfaces for listing, downloading and installing known local-AI sources without Git or GitHub CLI.

## Python and Whisper runtime control

- Adds persistent runtime policy for Python device selection, Whisper default variant, transcription/translation mode, language, initial prompt, queue capacity, bounded media limits and model caching.
- Exposes the runtime policy to the Setup UI, a dedicated local-AI runtime controller and approval-aware DXFunctions so either the local user or an AI team can propose changes through the common approval path.
- Extends workspace speech recognition so each job can override language, task, prompt, device and cache behavior while retaining configured defaults.
- Preserves the serialized Python.NET job lane and marks startup-only queue changes as restart-required instead of attempting unsafe in-process rebinding.

## Toolchains and environment workbench

- Moves Python discovery/linking beside .NET, Node.js and the other toolchains, while preserving the existing local toolchain discovery service.
- Adds reviewed direct HTTPS acquisition entries for Python, .NET, Node.js and selected upstream GitHub toolchain sources. Downloads are bounded, hashed, approval-gated and never auto-executed.
- Adds controller and DXFunction surfaces for the online acquisition catalog and downloads.
- Adds visible Effective, Process, LocalGPT Application, User and Machine environment scopes. Credential-like values are redacted from the UI/AI-facing inventory, and LocalGPT-owned application overrides persist to user configuration.
- Protects the shared application-override dictionary against concurrent UI/DXFunction access.

## ASCII operational console and ZIP approval integration

- Restores the shared operational console to a real monospace `<pre>` ASCII terminal surface instead of the DevExpress memo regression while keeping the existing bounded/coalesced activity feed behavior.
- Routes quarantined ZIP/project promotion through the common deferred Human Collaboration approval mechanism.
- Removes the old model-supplied `userConfirmed` promotion parameter: trusted confirmation now comes from the generic DXFunction approval gate, so an AI cannot self-assert promotion approval.
- Keeps generated artifact ZIP refresh on its existing approval-gated `council.artifact_workspace_zip` path.

## User-owned approval and function policy

- Changes the automatic DXFunction boundary so a consequential or non-automatic-safe function is no longer silently rejected merely because an AI initiated it. When the database policy does not pre-authorize it, LocalGPT creates the normal Human Collaboration approval request and queues the exact deferred call for execution after approval.
- Preserves the database permission catalog as authoritative: unavailable, disabled, or AI-hidden functions remain unavailable, while a user-saved `RequiresFrontendConfirmation = false` policy acts as explicit preauthorization.
- Exposes the same direct-invocation function catalog to individual provider models; the registry remains the authoritative approval/permission gate instead of filtering useful functions out before the model can request them.
- Repairs a disposal race in the Human Collaboration inbox so late service/game refresh events do not repeatedly log `ObjectDisposedException` after the owning InteractiveServer circuit has shut down.

## Generic database-backed toolchain process profiles

- Adds runtime-agnostic Toolchain BusinessObjects for reusable execution profiles and environment override records. Profiles reference the existing generic `ProjectCompilerInstallation` records rather than creating a Python-specific installation table.
- Persists process profiles and LocalGPT/toolchain environment overrides in the LocalGPT database through the existing `SystemVariable` BusinessObject storage contract, with scoped services as the only mutation/execution owner.
- Adds controller and DXFunction surfaces to list, save and execute toolchain profiles. Execution uses `ProcessStartInfo.ArgumentList` without a command shell, merges registered/toolchain/profile environment layers, and remains approval-gated by default.
- Adds Toolchains UI for profile key/name, capability, execution kind, installation, entry point, working directory, arguments, environment/configuration JSON, default selection, approval requirement, save and run.
- When Python is linked for embedded AI, LocalGPT also upserts it into the generic toolchain installation database and seeds a reusable `speech.whisper` module profile instead of hiding Whisper process behavior in a Python-only path.

## Council continuity, benchmark repetition and ASCII popup recovery

- Extends bounded Council conversation reconstruction from 12 to 24 user turns and retains up to four cleaned prior assistant consensuses; saved continuation context likewise keeps the latest 24 non-typing messages. Existing prompt-size caps still apply.
- Forces the existing provider repetition watchdog on during model benchmarks so pathological Phi/reasoning repetition cannot run indefinitely merely because the general runtime watchdog policy was disabled.
- Repairs rejoined live-Council chat ownership: the Council session and canonical message cache are synchronized before DevExpress rebinding and after direct human messages, preventing the transcript from disappearing until refresh/rejoin.
- Removes popup-only `height: 100%` forcing from the ASCII conversation/game stages so the console header and footer keep their grid space and the lower controls/footer are not clipped by the DevExpress popup body.

## Preservation

- Existing LocalGPT provider chat/Council behavior, project quarantine/review gates, toolchain discovery, Python.NET serialized execution and specialized `localai.*` DXFunctions remain in place.
- Razor interactive ownership is preserved: the set of `@rendermode InteractiveServer` pages/components is unchanged from 4.7.9.
- Version rolls from 4.7.9 to 4.8.0, avoiding a two-digit patch slot.
- PublisherStudio is unchanged in this release.
- No GitHub access, `dotnet`, MSBuild, NuGet restore, build, publish or installer execution was used for this source change.
