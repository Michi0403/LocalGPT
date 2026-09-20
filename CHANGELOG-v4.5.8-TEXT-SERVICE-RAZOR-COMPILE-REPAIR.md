# LocalGPT 4.5.8 — text-service ownership and Razor callback compile repair

## Windows build follow-up

- Preserved the maintained text-service ownership policy instead of adding the new ASCII layout signature to its baseline or weakening its scan.
- Moved ASCII console layout-signature composition from `ChatGameConsole.razor` into the already injected `AsciiChatTextService`, keeping the component presentation-only while retaining the 4.5.7 browser-layout caching behavior.
- Changed the Chat provider selector to an explicitly typed `string` `ValueChanged` lambda so DevExpress/Razor binds the callback as the required typed event instead of an untyped `EventCallback` method group.
- Kept the provider-session value model, dropdown portal repair, numeric-editor repair, inline approvals, cancellation handling, ASCII combat animation, tournament member failover and compiler/publisher Council workflows from 4.5.7 unchanged.

## Compatibility

- No EF migration.
- No wire-protocol change.
- Council seed remains 35 and runtime-class seed remains 7.
- InteractiveServer declarations remain at 15 direct islands/pages plus 5 explicit non-prerender islands.
- PublisherStudio source is unchanged.
