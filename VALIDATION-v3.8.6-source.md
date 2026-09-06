# LocalGPT 3.8.6 source validation

This source revision repairs the mandatory operational-diagnostics guard to match the post-listen worker lifecycle introduced in 3.8.5.

Static/source checks for this revision require:

- exactly one direct LocalGPT `AddHostedService<T>` registration: `LocalGptPostListenHostedServiceCoordinator`;
- all eight maintained application workers remain concrete singleton registrations;
- the coordinator waits on `IHostApplicationLifetime.ApplicationStarted` before resolving the workers;
- the coordinator resolves and starts each preserved worker after that boundary;
- the operational-diagnostics guard rejects direct startup registration of those eight workers;
- the 15 reviewed `@rendermode InteractiveServer` boundaries remain unchanged;
- the release pipeline runs `build/Test-LocalGptStartupHealth.ps1` after the RID-neutral build and before documentation generation;
- existing provider onboarding, packaged knowledge, user-data path, notarization, and cross-platform boundaries remain present.

No real `dotnet`/PowerShell runtime is available in the assistant environment, so the final authority remains the repository guard plus real `dotnet build`/release execution on a supported host. The specific 3.8.5 contradiction that caused the uploaded build to exit with MSB3073 is removed in source.
