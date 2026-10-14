# LocalGPT 4.7.5 source validation

## Constraints

- LocalGPT 4.7.4 is the immediate source baseline.
- The supplied Visual Studio output and screenshot are treated as local diagnostic evidence only.
- No GitHub/network repository content was used.
- No `dotnet`, restore, NuGet, MSBuild, build, publish or installer command was invoked.
- Validation is static/source-level and does not claim a compiled runtime result.

## Root-cause evidence checked

- The supplied overnight output contains a high volume of first-chance `System.Text.Json.JsonReaderException` entries while provider benchmark work continues.
- 4.7.4 deliberately treated that noise as non-terminal and focused on the terminal `TaskCanceledException`; therefore the parser flood remained possible.
- `ProviderModelBenchmarkService.TryParseFirstJsonObject` called `JsonDocument.ParseValue` inside a loop over every `{` in untrusted model output and used `JsonException` as ordinary scan control flow. Under source/code-heavy benchmark output this can generate many debugger-visible first-chance exceptions per response.
- Textual DX-function recovery also parsed JSON-shaped model text after streaming and could contribute first-chance parser diagnostics for incomplete carriers.
- The file logger previously created one queue, background thread and `File.AppendAllText` writer per logging category, all targeting the same file. Under high concurrency this can cause sharing failures and dropped file entries. In Development configuration the file threshold was also `Error`, so normal Information-level benchmark progress did not refresh the file timestamp.

## Verified source contracts

- Benchmark JSON discovery now uses a quote/escape-aware balanced-object scan before invoking `System.Text.Json`, and skips over malformed complete candidates rather than parsing every opening brace.
- DX function text recovery rejects structurally incomplete JSON carriers before `JsonDocument.Parse`.
- The file logger provider owns one lazy shared sink; category loggers enqueue into that sink rather than opening competing writers.
- The shared writer retries transient I/O errors, reopens itself, allows concurrent readers/copy tools, and uses a fallback log path if the configured destination remains unavailable.
- Development file logging is `Information` so long-running debugger sessions leave continuously current file evidence.
- 4.7.4 provider-stream cancellation containment remains present.
- Existing 4.7.3 ASCII popup interaction/scaling fixes and 4.7.2 Council/tournament feature anchors remain present.
- Existing `@rendermode InteractiveServer` coverage remains present across the protected component surface.

## Static checks used before packaging

- XML parsing of the three versioned project files.
- JSON parsing of application settings and `docs/docfx.json`.
- JavaScript syntax validation for the LocalGPT game-console and chat UI scripts when Node is available.
- Static source assertions for the balanced benchmark JSON scanner, DX recovery guard, shared file sink, retry/fallback path, 4.7.4 cancellation-boundary markers, prior Council/tournament/popup anchors, version-slot policy and InteractiveServer coverage.
- ZIP CRC/path traversal checks and clean extraction byte comparison after packaging.
