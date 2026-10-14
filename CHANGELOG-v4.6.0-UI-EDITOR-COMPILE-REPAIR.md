# LocalGPT 4.6.0 — UI editor layout and Council rejoin compile repair

## Fixed

- Corrected the `Chat.LiveCouncil.razor.cs` bounded-rejoin warning callback so `InvokeAsync` receives a valid block-bodied `Action`. This removes the `void`/`Task` lambda mismatch reported by the Windows compiler (`CS0029`, `CS1662`, `CS0201`) without changing server-owned Council recovery behavior.
- Kept the Chat provider editor on `DxFormLayout`, but restored its wide responsive flow instead of forcing every item into a full-width one-column row. The provider selector and three DevExpress checkbox settings now use 5/3/2/2 medium-width columns and collapse responsively through the DevExpress layout.
- Replaced the malformed Chat Council hardware-load `DxSpinEdit` surface with the maintained `BoundedNumberEditor`, keeping the same 0–100%, step-5 behavior and existing persistence/runtime update path.
- Removed Bootstrap `form-control` styling from the Benchmark Council `DxSpinEdit` editors for “Even steps per model” and “Reviewers per recommendation”, plus the individual provider benchmark “Even steps” editor, preventing DevExpress spin editors from expanding into oversized editor blocks.
- Wrapped Direct Council starter title/description content in an explicit internal layout container so the title and starter text no longer concatenate visually inside the DevExpress button.
- Corrected the Architecture action grid from three to four desktop columns, so consent, extra direction, “Add Decision To Chat”, and “Reset” each occupy a stable slot instead of the Reset action stretching across the next row.
- Retained the 4.5.9 body-level DevExpress dropdown/list portal stacking repair for Chat configuration and other fixed workbench surfaces.

## Preserved

- InteractiveServer render-mode topology and existing DevExpress-first control policy.
- 4.5.9 stale-knowledge workflow, exact-source approval isolation, Kernel Creature Tournament team/joint/timeline behavior, and bounded Council rejoin ownership semantics.
- No PublisherStudio source change was required.

## Validation scope

Source/static validation only. No `dotnet`, NuGet restore, MSBuild, publish, GitHub, or online repository access was used for this package. The user-provided Windows compiler output was used to identify the concrete C# regression, and the supplied UI recording was reviewed frame-by-frame for the editor/layout defects repaired here.
