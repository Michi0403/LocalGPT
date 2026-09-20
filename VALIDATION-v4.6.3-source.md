# LocalGPT 4.6.3 source validation

GitHub was not used and `dotnet`, restore, build, MSBuild, NuGet or publish were not invoked in this environment.

LocalGPT 4.6.3 repairs the `/install` DevExpress UI regression, restores direct CanIRun.ai resolve/install actions, makes hardware numeric input invariant and reviewable, and compacts the benchmark/model presentation while preserving 4.6.2 project-ingestion/1-Wire/regex/ASCII behavior and the 4.6.1 startup-lifetime repair.

## Source checks

The release package is produced only after the maintained static/source checks pass, including the 4.6.3 release audit, architecture/service/async guards, DevExpress control audit, JavaScript syntax and integrity checks, JSON parsing and XML/MSBuild parsing.

## Packaging validation

The distributed ZIP is tested for ZIP CRC integrity, unsafe archive paths, clean extraction and byte-for-byte equality with this source tree, excluding transient interpreter cache files.

## Runtime limitation

No .NET executable is run here. Compile/startup/runtime interaction confirmation remains the user's Windows/macOS/Linux .NET environment.
