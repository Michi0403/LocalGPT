# LocalGPT 6.1.0 — Console component source ownership cleanup

Behavior-preserving source organization from 6.0.12. The next version follows the single-digit minor/patch convention.

- `ChatGameConsole.razor` remains the existing, byte-for-byte Razor markup, retaining all DevExpress widgets, UI callbacks, game/Council/session controls, browser element identifiers, and CSS isolation scope.
- Moved its existing `@code` members **without modification** to `ChatGameConsole.StateAndConversation.razor.cs` and `ChatGameConsole.GameInteractionAndRender.razor.cs` (both below 1,500 lines), preserving the original `ChatGameConsole` partial class, DI service ownership, logging, and async lifecycle.
- `AsciiChatTextService`, the Council/game/operator services, BusinessObjects, and approval workflows remain the source of reusable behavior. This source split adds no new runtime policies or team-specific logic.
- The console's markup was deliberately not moved into a child component because `ChatGameConsole.razor.css` is CSS-isolated and shifting DOM nodes across the component boundary would be visually observable.
- `localgpt-game-console.js` and `localgpt-game-studio.js` are still global, shared browser runtime assets. Their registration, JS namespace and consumer contracts are unchanged. No pretend collocation wrapper was added.

This release does **not** claim completion of broader service/JavaScript refactoring or media model generation work. Windows .NET compilation, architecture guards and runtime console interaction remain to be run locally. See `VALIDATION-v6.1.0-source.md`.
