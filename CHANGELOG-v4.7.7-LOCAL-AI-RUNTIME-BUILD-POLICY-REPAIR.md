# LocalGPT 4.7.7

## Local AI runtime build-policy repair

- Repairs the 4.7.6 Local AI runtime source against the repository's real build gates reported by the owner-side Visual Studio build without rolling back the Local AI runtime, Hugging Face, Python.NET, media, Council, ASCII, Pixel, or tournament work.
- Adds the missing `Microsoft.Extensions.Options` imports and disambiguates LocalGPT's `ConfigurationRoot` from `Microsoft.Extensions.Configuration.ConfigurationRoot` in the new Local AI services and Python.NET coordinator.
- Makes every new asynchronous stream disposal explicit with `ConfigureAwait(false)` so the maintained async-continuation policy accepts the artifact and private-input copy paths.
- Brings the new Hugging Face, artifact, Local AI runtime, and Python.NET coordinator service methods under the repository's maintained try/catch + diagnostics contract. The 4.7.5 benchmark JSON-carrier helpers are covered as well.
- Removes the application-static policy violations reported for the 4.7.5 logger/JSON helpers by keeping those helpers instance-owned.
- Moves Local AI package/capability display joining out of Razor/component code and into `ILocalAiRuntimeService`, preserving the maintained text-service ownership boundary.
- Refreshes the maintained JavaScript diagnostics manifest for the already-reviewed game-console frontend source.
- Corrects the Python bridge signature adapter so required modality inputs such as `image` can no longer be accidentally accepted after the bridge itself detects that the selected pipeline does not support them. Optional `guidance_scale` to `true_cfg_scale` remapping remains supported.

## Compatibility and preservation

- Version advances from 4.7.6 to 4.7.7; all version slots remain single-digit.
- Existing `@rendermode InteractiveServer` coverage is preserved.
- PublisherStudio is unchanged.
- No GitHub access, `dotnet`, MSBuild, NuGet restore, build, or publish is used for this source repair; the owner-side compiler remains the authoritative runtime build check.
