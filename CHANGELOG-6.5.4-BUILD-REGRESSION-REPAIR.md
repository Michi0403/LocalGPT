# LocalGPT 6.5.4 — Windows build regression repair

Base: full LocalGPT 6.5.3 source ZIP supplied in this conversation. No changes to PublisherStudio.

## Confirmed fixes

- **MSB3073 in `Assert-JavaScriptDiagnostics.ps1`**: Reviewed and updated the normalized SHA-256 manifest hashes for both `src/LocalGPT/wwwroot/js/javascript-diagnostics.js` and `src/LocalGPT/wwwroot/js/localgpt-reconnect.js` as shipped in 6.5.3. Only the hash inventory changed; the PowerShell guard and the JavaScript are unchanged. Both stale hashes were identified although the reported build stopped on the first one.
- **CS0815 in `Chat.UploadAdvisor.razor.cs:71`**: The lambda passed to `InvokeAsync` was bound to a `Task`-returning overload, making `var submitted = await ...` a void assignment. The code now awaits an explicitly `Func<Task>`-typed dispatcher callback and captures the actual `bool` result of `StartCouncilPromptAsync` in a local variable. Its failed-submission warning is preserved; no false success, fire-and-forget, or security bypass is introduced.
- Increased `src/LocalGPT/LocalGPT.csproj` from `6.5.3` to `6.5.4`, updated the existing version assertions and the affected source regression test, and added tests covering these two build errors.

## Existing behavior retained

Upload processing preference for Workspace Curation, JS-disconnect handling, browser reconnection, Council authorization, saved identities and 1-Wire transport have not been changed.

## Verification limitations

Static/offline tests and JavaScript syntax checks can be run here, but **no Windows .NET/Razor build or NuGet restore was performed**. An earlier Visual Studio restore warning involving `LocalGPTWebviewWrapper` is not proven fixed: the submitted log contains no underlying NuGet error details for it, and the solution proceeded to build dependent projects. The live SignalR/Blazor circuit timeout remains separately unverified.
