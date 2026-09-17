# LocalGPT 4.3.7 source validation

## Scope

Source-only validation of the reported chat configuration sliders, `/chat` reachability/navigation behavior, ASCII game/operator interaction and Council lifecycle cleanup. No .NET build, restore, publish, signing/notarization, GitHub access, GitHub API operation, or other online repository access was performed.

## Checks performed

- `node --check src/LocalGPT/wwwroot/js/localgpt-game-console.js` — passed.
- `python build/audit_release_4_3_7.py` — passed.
- `python build/audit_service_resilience.py --root . --product localgpt` — passed; 2,441 service methods were checked and the maintained iterator/direct-entry exclusions were reported by the audit.
- `python build/audit_xround_wiring.py` — passed.
- The three versioned `.csproj` files used by LocalGPT, the installer console and the WebView wrapper parse as well-formed XML.
- The release audit confirms every routable Razor page except the maintained error page still declares `@rendermode InteractiveServer`.
- The tracked documentation Pages ZIP passes `ZipFile.testzip()` and its index is synchronized to the 4.3.7 handbook name.

## Reported regression coverage

- Range sliders now use committed `change` events instead of continuous Blazor Server `input` events in the shared bounded-number editor, Council hardware-road model override and chat Council load control.
- `/chat` owns a reachable vertical scroll path, and query-only drawer navigation no longer keys the active page on the complete URI.
- The ASCII console observes parent/element resizes and ignores game shortcuts while text entry controls have focus.
- Council hot-seat turns and other active-run human requests have an in-ASCII interaction path.
- Chat, Game and Operator ASCII modes all share the fullscreen control and scaling contract.
- Council completion/cancellation closes still-running participant lanes and ends only game sessions tagged with the same Council run.

## Limitation

This validation is intentionally not a compiler or runtime claim. The supplied instruction explicitly excluded invoking a .NET environment, so C# and Razor changes were reviewed and exercised through repository static audits only. Runtime/build verification should be performed in the normal LocalGPT development environment before deployment.
