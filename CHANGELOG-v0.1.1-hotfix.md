# LocalGPT v0.1.1 hotfix

## Compiler repair

- Removed four accidental `using static System.Net.WebRequestMethods;` imports.
- Prevented the `WebRequestMethods.File` and `System.IO.File` CS0104 collision.
- Explicitly qualified the reported benchmark check as `System.IO.File.Exists`.
- Added a repository source guard that rejects the forbidden static import before compilation.

## Peaceful collaboration guidance

- Replaced model-specific instruction wording with peaceful, friendly, Christian-inspired values centered on truth, humility, stewardship, care for others, free choice, and non-harm.
- Clarified that the values are ethical guidance only and grant no religious, legal, personal, or automated authority.
- Removed the model-specific instruction file and all active project, seed, architecture, and release-process references to it.
- Made the local-machine boundary explicit: AI-assisted maintenance must not start, stop, configure, probe, or connect to localhost services.
- Prohibited AI-assisted system changes, user-data access, credential access, repository-script execution, generated-program execution, and changes outside an isolated repository copy.
- Preserved requested cloud/disposable-workspace source editing as reviewable file changes only.

## README acknowledgment

- Added a plain acknowledgment that LocalGPT remains Michi0403's own architecture and implementation, developed from personal experience and earlier frameworks.
- Credited the repeated co-development work completed with OpenAI's ChatGPT.
- Credited `gpt-oss-20b` as instrumental in making the initial working system possible.
- Noted that LocalGPT's own review workflows produced dozens of missing-feature reports used during co-development with ChatGPT.
- Clarified that the final design decisions and responsibility remain with Michi0403.
- Clarified that no local coding agent operated LocalGPT or any localhost service during this cloud repair.

## Validation

- Parsed 8 JSON files and 3 XML project/resource files.
- Lexically scanned 151 C# files for balanced delimiters and unterminated comments or strings.
- Confirmed zero forbidden static `WebRequestMethods` imports.
- Confirmed zero remaining references to the removed model-specific instruction file.
- Confirmed no runtime SQLite databases or generated DevExpress license script are present in the source package.
- A real build remains owner-side because the repair environment does not contain the .NET SDK, DevExpress feed/license, Windows workloads, WebView2 runtime, or MSIX tooling.
