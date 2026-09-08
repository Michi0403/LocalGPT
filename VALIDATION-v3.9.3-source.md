# LocalGPT 3.9.3 source validation

This patch is prepared under the maintained source-only boundary: no `dotnet` build/publish and no PowerShell release build are run in this environment.

## Runtime evidence addressed

The supplied Windows log shows Ollama discovery succeeding with 54 models while `CanIRunHardwareRecommendationService` fails because the previous recommendation endpoint redirects and LocalGPT rejects the redirect. The saved Windows HTML consequently contains the full Setup form and installed-model list but no recommendation/provider mapping in step 4.

The supplied macOS log shows the earlier `Interop.Sys.GetCwd()` / application-bundle log-path failure and the Setup form not rendering. The final 3.9.2 source already contained the direct working-directory/logger repair, but the macOS launcher log lacked the new launcher marker and executed the older logger/probe stack. Inspection of the release scripts found that same-version notarized/native artifacts could be reused without a source identity check.

## 3.9.3 regression checks

The source-specific audit verifies that:

- all three shipped executable projects report `3.9.3`, with one-digit version slots;
- the CanIRun request starts at `https://www.canirun.ai/api/recommend`;
- automatic redirects remain disabled and manual redirects are bounded, HTTPS-only, and revalidated against CanIRun.ai;
- the CanIRun response parser retains its 512-object traversal bound and no 24/32/96 downstream display/service truncation exists;
- provider-specific Ollama metadata is retained when the JSON response exposes it;
- recommendation rows remain present when one provider mapping is not safe, and unsafe one-click installation is disabled rather than issuing a guessed shell token;
- installed-model matching compares the complete provider model token instead of collapsing all size/tag variants to the same family;
- Setup snapshot hardware, provider-profile, provider-model and onboarding-status failures are isolated;
- the Razor Setup component can render a provider-first recovery snapshot rather than remaining permanently on the loading placeholder;
- the installed runtime can resolve its release version from assembly/package metadata when the source project file is absent;
- remaining current-directory consumers use the safe runtime resolver rather than directly depending on an inherited CWD;
- LocalPathExplorer and project-maintenance process fallbacks no longer use `Environment.CurrentDirectory`, and optional file-logger construction falls back to a null logger instead of escaping into UI rendering;
- the file logger remains independent of `Directory.GetCurrentDirectory()` and defaults to per-user logs;
- release reuse requires a matching source fingerprint and final bundles contain `SOURCE-SHA256.txt`;
- the generated macOS launcher still starts from the durable per-user runtime directory;
- all required unified Setup controls remain present;
- exactly 15 Interactive Server render boundaries and eight hosted service registrations remain present.

## Static validation boundary

The final tree is run through the release-specific Python audit and the repository's maintained architecture, async-continuation, cross-platform, code-generation/DXFunction, configurable-behavior, provider-stream, X-Round, provider-qualified Council, SQL-seed, service-resilience, and XML/Razor documentation audits.

Passing those checks is source/static validation, not a claim that the signed Windows/macOS packages were built here.
