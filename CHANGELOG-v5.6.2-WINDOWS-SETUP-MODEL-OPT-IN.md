# LocalGPT 5.6.2 — Windows setup without automatic model downloads

Based on the complete LocalGPT 5.6.1 source release. InstallerConsole advances from **4.9.4 to 4.9.5**.

## Report and root cause

The setup transcript shows `InstallOllamaAsync` detecting an existing Ollama executable, then repeatedly running `ollama pull gpt-oss:20b` until the user cancelled. This behavior originated in `CliOptions.Parse`: an argument-free Windows setup inserted `--pull-models --range Slim`. The combined `--all` option also enabled model pulling. Those implicit downloads were no longer appropriate because LocalGPT's first-run guide links to `/install`, which supports user-selected Ollama and LM Studio model downloads.

## Changes

1. **No implicit model downloads:** argument-free setup and `--all` keep their other installation/setup actions but no longer enable `PullOllamaModels`. The Ollama installer still detects existing installations or installs Ollama using its existing Windows installer logic.
2. **Preserve explicit user choice:** `--pull-models` and separate `Pull-Models-*.cmd` files remain available only when deliberately invoked. `/install` continues to own interactive Ollama / LM Studio model selection, installation and configuration. No models are silently selected.
3. **First-run guide corrected:** the Game Council and Development installer profiles no longer include `--pull-models`; displayed model names remain *suggestions*. The guidance explicitly sends users to `/install` to choose and download models. No changes to persisted teams or presets.
4. **Documentation and versions:** LocalGPT **5.6.2**; Windows InstallerConsole **4.9.5**; updated browser-resource cache version, installer docs and quick-start README. Previous features and safety boundaries retained.
5. **Regression coverage:** `build/audit_windows_setup_no_auto_models_5_6_2.py` validates default/`--all` opt-in behavior, deliberate manual downloads, onboarding, launchers, identities and guidance. Existing retained feature audits were updated to expect 5.6.2 without reducing their checks. Historical 5.6.1 packaging audit is preserved as a pinned previous-release contract.

## Scope

PublisherStudio has no dependency on the default Windows installer behavior and remains unchanged. No external source or GitHub was used. No .NET build/publish, live Ollama install/pull, or interactive UI test was executed here. The Windows installer must be built on a compatible developer system before runtime validation.
