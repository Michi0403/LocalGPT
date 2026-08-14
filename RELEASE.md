# LocalGPT 2.8.1 human-visible entity formatting release

LocalGPT 2.8.1 is a surgical presentation-boundary maintenance release.

- Repairs `&quot;`/HTML punctuation entity leakage in human-visible Chat, history, Council and spooler output.
- Routes Model Council Markdown through the maintained chat-content renderer so structured JSON and punctuation receive the same presentation normalization as Chat.
- Keeps markup-significant entities encoded on the MarkupString path and relies on normal Razor encoding for plain-text HTML decode surfaces.
- Does not change Council/provider behavior, persistence, DXFunctions, hardware presets, 1-Wire protocol or InteractiveServer topology.

The owner-side Windows .NET build remains authoritative for compilation and release publishing.
