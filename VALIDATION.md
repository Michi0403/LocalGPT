# Validation

## Completed in the repair environment

- confirmed the uploaded ZIP contained an outer repository and a complete nested repository, each with its own Git metadata;
- compared normalized source content and selected outer commit `35b5fcf53681129d39e461e41ef3d2a691679a25` as the newer canonical base;
- rebuilt one clean working tree and confirmed no nested `.git` directory remains;
- removed tracked runtime SQLite databases and the generated DevExtreme runtime-license script;
- added ignore rules for DB/WAL/SHM/log/IDE/generated-clone/license state;
- parsed both appsettings files as JSON and `LocalGPT.csproj` as XML;
- ran `git diff --check` with no whitespace errors;
- scanned 144 C# source files with a lexical balance checker for unclosed strings, characters, comments, braces, brackets, and parentheses;
- checked for unresolved merge markers, remaining `Database.EnsureCreated` calls, nested repository roots, obvious provider-secret logging, and generated license-key content;
- reviewed formatter behavior for plain text, `<think>` tags, Harmony, split markers, completion flushing, and thinking without final output;
- exercised structural live-render cases for partial/completed thinking and partial/completed council panels, including temporary render-only closures and live-state removal;
- verified the AI Council stream bridge is event-driven rather than two-second polling, and that streamed member presentation is ordered to avoid crossing nested markup;
- verified database path, health probing, recovery, migration, and seeding are service-owned, with no active caller of the removed static SQLite recovery/path helpers;
- verified the Chat page reads its output-token, context-window, GPU-layer, and default Ollama endpoint values from the seeded variable service, retaining compiled values only as failure fallbacks;
- reviewed native command policy for default denial, separate PowerShell opt-in, workspace containment, timeout, cancellation, process-tree termination, and redacted audit arguments;
- moved direct generated-artifact/benchmark `dotnet build` launch code into a bounded service, disabled it by default, and changed generated AI-host native-runner templates to opt-in;
- reviewed the runtime bootstrap, `llms.txt`, Copilot instructions, and knowledge seed trust metadata so models/documents cannot impersonate the human owner or grant permissions to one another;
- verified the source-package staging rules exclude Git metadata, runtime databases, logs, generated license material, and font binaries;
- verified the supplemental source patch excludes the removed database payloads and generated DevExpress license content, with an exact-path reviewable cleanup script supplied instead.

## Not executable in the repair environment

The environment did not contain a .NET SDK/compiler, DevExpress package feed or license, Windows workloads, MSIX tooling, or a WebView2 runtime. Network access was unavailable for installing the SDK. Therefore no claim is made that the solution compiled, restored DevExpress packages, launched the Windows wrapper, ran the installer, published, signed, or executed UI/runtime integration tests.

Structural checks supplement a real build; they do not replace one.

## Required owner-side validation

Run every step in `RELEASE.md` on the licensed development machine. Fix compiler/runtime findings before merging, with priority on the new formatting, persistence, command-runner, and provider-adapter files. Keep state behind the new service boundaries rather than moving it back into global statics.
