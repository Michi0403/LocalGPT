# LocalGPT 5.9.2 — source validation and Windows acceptance

## Static source review

- Full original ZIP source retention and CRC audit: pass when packaged.
- `LocalGPT.csproj` XML and 5.9.2 version contract: pass.
- `wwwroot/css/site.css` syntax checked using `tinycss2`; no top-level parse errors.
- Assessed parser call path: fixed-tag scan precedes optional JSON validation; the invalid start `i` cannot reach `JsonDocument.Parse`. An incomplete opening tag is not replaced and does not invoke a wildcard regex.
- Bounded cache stores fingerprint and length, not source content; shared state guarded by lock. Sampling prevents exception logs on every render.
- Existing PowerShell guard sources updated without removing or weakening their existing checks. PowerShell execution unavailable here.

## Windows runtime acceptance

1. Build LocalGPT with the current Debug/Release PowerShell architecture checks enabled. The C# compiler and build guard results are authoritative.
2. Stream a long Council response with an unclosed self-assessment opening tag; verify the UI remains responsive, source remains visible, and no self-assessment regex timeout is thrown.
3. Send literal and HTML-encoded assessment tags with valid JSON (including mixed closing names): confirm structured Markdown and raw JSON disclosure work.
4. Send a tagged payload starting `i`, a partially emitted `{...`, a brace-balanced invalid document, and JSON containing quoted braces. Confirm all malformed payloads are inert and repeated updates do not flood first-chance `JsonReaderException` messages.
5. Check sampled warning fields: reason, occurrence, payload/source size, JSON line/byte position where available. Ensure model/user text is absent from logs.
6. Open Council inspection on desktop and narrow screen. Confirm both panes fill the popup's content height, scroll independently and retain existing 90vw × 90dvh bounds; empty provider stream also fills available height.

## Known limits

- A distinct malformed, brace-balanced JSON payload may still cause one caught `JsonException` before its fingerprint enters the bounded cache; rejecting every possible malformed JSON without ever invoking a parser would require duplicating JSON grammar, which this change deliberately avoids.
- Other unrelated JSON parsers/regexes in LocalGPT can still throw first-chance exceptions; this repair targets the formatter identified by the supplied stack traces.
- Visual Studio may still show caught exceptions from network cancellations or unrelated parsing; use stack traces to attribute them.
