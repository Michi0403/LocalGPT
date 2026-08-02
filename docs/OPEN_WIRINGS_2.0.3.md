# Open wiring boundaries in 2.0.3

## Empirical Ollama route autotuning

`localgpt.models.benchmark.autotune` is deliberately registered but returns `NotImplemented` through an underlying `NotImplementedException`.

Reason: a trustworthy tuner must execute against the user's actual Ollama build, model quantizations, drivers, VRAM/RAM and thermal conditions. This source environment has no .NET SDK, Ollama runtime or target GPU matrix, so silently persisting guessed route settings would be worse than leaving the boundary explicit.

The usable fallback is the seeded **Reactive ASCII Gameplay** preset: one model at a time, Auto GPU, compact context/output ranges, and small model candidates. The future empirical implementation should benchmark installed models, vary bounded context/output settings, stop a route search when improvement stays below five percent, and persist only a copied user-approved preset.
