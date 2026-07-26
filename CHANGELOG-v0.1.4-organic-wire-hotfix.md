# LocalGPT v0.1.4 organic-wire hotfix candidate

## Root regression repair

- Restored the public read-only `Program.Port` compatibility contract and the fixed default port `5000` used by the installer and WinUI wrapper; callers cannot mutate the runtime value.
- Kept the installer positional port argument and added `--port`/environment/configuration resolution without removing the legacy bootstrap path.
- The wrapper now uses `Program.BaseUrl`; the downstream `WMC1006` is expected to disappear once `LocalGPT.dll` builds successfully.
- Added installer-port precedence: an optional organic TCP collision is reassigned instead of terminating the LocalGPT desktop/bootstrap path; TCP/UDP sharing is logged explicitly.
- Added a source contract test that fails when the installer, wrapper, project reference, default port, positional argument, distinct-port safeguard or organic wiring is removed.

## Organic plugin and council wiring

- Added the 1-Wire envelope, SHA-256 integrity, CRC32 transmission check, nullable encrypted payload container, capability/organ/skill metadata, sequential work spool and future transport interfaces.
- Added a fault-contained TCP listener and UDP discovery broadcast on dedicated ports. Optional protocol listener failure does not terminate the LocalGPT web/installer bootstrap.
- Added peer/capability discovery, work status APIs and a human-gated `organic.plugin.invoke` DX AI function. Approved external Council requests now resume automatically from the persistent Human Collaboration inbox instead of requiring an impossible new-correlation retry.
- Added revision-aware organic project context persisted through the existing project artifact database table.
- Added actual expert-preparation and leader-synthesis heartbeat stages for organic council runs.
- Added built-in `OpenSCAD Team` and `Spreadsheet Team` role blueprints.

## Validation status

This is an unverified source candidate. The repository's Node source-contract test was run in the delivery workspace; a .NET SDK and licensed DevExpress build environment were not available, so Debug/Release compiler validation was not claimed.

- Council completion and failure now return an explicit `Status`/`ResultJson` work result to the requesting organic peer, preventing silent failed heartbeats.
