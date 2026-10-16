# LocalGPT 5.9.0 — source validation and Windows acceptance

## Offline source validation

This release was prepared by examining the user-provided `LocalGPT-5.8.9-source.zip` alone. No GitHub checkout, .NET build, Windows PowerShell execution, or DevExpress runtime verification was performed. Source packaging verification checks project XML, retained original entries, DocFX TOC/links, Mermaid blocks, targeted Chat phase ownership, and archive CRC.

## Recommended Windows tests

1. Run the normal Visual Studio **Build** on the full source. The existing `Assert-InteractiveServerRenderModes.ps1` is part of the build; it now checks that Chat's first interactive render is the event subscription boundary.
2. Open `/chat` with InteractiveServer prerender enabled. Confirm initial HTML is produced without JS interop or live notification subscription errors, and navigating before circuit attach does not overwrite selected Council configuration.
3. Allow the circuit to attach, then start/attach to a Council run. Confirm live collaboration, live session, and run-configuration events update the view once, without duplicate callbacks.
4. Navigate away and return. Confirm the first component unsubscribes and the new instance attaches correctly; a running Council job remains active in the spooler.
5. Exercise interrupted connections and reload/rejoin. Verify no lost conversations, duplicate Council launches, or stale interactive subscriptions.
6. Build DocFX documentation, open **Architecture → Reactive lifecycle and process equivalence (UML)**, and check all four diagrams are rendered, legible, and included in browser-printed PDFs.
7. Confirm no change to PDFsharp licensing/notices or PublisherStudio artifacts.

## Limitations

The diagrams describe reviewed runtime/business-process contracts, not inferred universal service equivalence. No broad service merger is attempted without source-backed UML state and side-effect analysis. Syntax and ZIP integrity checks are not substitutes for Windows runtime tests.
