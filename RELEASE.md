# LocalGPT 3.0.7 nullable exit-code compile repair

LocalGPT 3.0.7 is the source-only successor to 3.0.6. It preserves the AI-guided hardware/provider/model/benchmark setup and corrects the next compiler blocker reported by the user's authoritative local build.

## Repaired

- `ConsoleCommandService.ExecuteAsync`: `exitCode` is now explicitly `int?`, so the timeout/process-exit/`null` nested conditional has a valid target type and matches the nullable result model.
- The intended runtime behavior is unchanged: timeout uses `-2`, a terminated process uses its actual exit code, and absence of a usable exit code remains `null`.

## Preserved boundaries

The cross-platform ASCII/shared command console, explicit human-confirmation gates, CanIRun.ai opt-in/credit path, knowledge-backed Ollama/LM Studio bootstrap, installed-model handling, hardware-curated benchmark setup, Council integration, 1-Wire protocol `2.1.1`, and existing InteractiveServer architecture are retained. No EF migration is introduced.

## Versions

- LocalGPT: 3.0.7
- LocalGPTWebviewWrapper: 3.0.7
- LocalGPTInstallerConsole: 3.0.7

See `CHANGELOG-v3.0.7-source.md` and `VALIDATION-v3.0.7-source.md` for the source-only repair and validation record.
