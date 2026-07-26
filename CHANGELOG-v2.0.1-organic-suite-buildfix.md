# LocalGPT 2.0.1 organic-suite build fix

## Purpose

This is a narrow compiler/build correction over the complete 2.0.0 organic-suite workspace. Existing Council, database, scheduler, DXFunction, frontend-authority and 1-Wire architecture is retained.

## Corrected compiler diagnostics

- Added the missing `LocalGPT.BusinessObjects` import to `GetTimeAndStateNowFunction`, restoring the exact `IDxAiFunctionHandler` descriptor and invocation types.
- Replaced invalid model-road projections from `OneWireHardwareDescriptor` with its actual inventory fields: kind, index, name, vendor, memory, processor count, online state and lane key.
- Added `LocalGPT.Components.Shared` to the component imports so `CouncilHardwareRoadEditor` is compiled as a Razor component in Chat and Model Council.
- Kept the model-specific min/max token and resource-road data in the existing Council route/preflight structures rather than incorrectly attaching those fields to physical hardware inventory DTOs.
- Added a NuGet package readme declaration using `Content/None Update` semantics, avoiding both the missing-readme warning and SDK duplicate-item errors.

## Version and database continuity

- LocalGPT application, wrapper and installer advance to `2.0.1`.
- Protocol compatibility and the authoritative NuGet package remain `2.0` / `2.0.0`; no wire-contract change was required.
- Existing `2.0.0` LocalGPT Core project rows are advanced losslessly to the `2.0.1` seed revision without replacing user-edited project, team, regex or knowledge data.

## Validation performed here

- Installer/bootstrap and organic 1-Wire source contract: passed.
- Council spooler/shared protocol/source closure contract: passed.
- Frontend-authority/public-service exposure contract: passed.
- Version-2 compiler hotfix, package readme, database feed, component import and time/state contract: passed.
- XML/JSON/project-reference/archive and workspace-preservation checks are recorded in the delivery report.

## Build truth

A native .NET 10/Windows/DevExpress compiler was unavailable in this environment. This package is intended for immediate Visual Studio debugging on the maintainer machine.
