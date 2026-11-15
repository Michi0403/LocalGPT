# LocalGPT 7.2.4

- Add identity training integration around the packaged LoRA trainer (`Runtime/Python/localgpt_lora_trainer.py`, now shipped as project content).
- `localgpt.training.probe` (read-only, automatic) reports the machine, backend and whether a model of a given size fits.
- `localgpt.training.identity.prepare` (needs human approval) writes `train.jsonl` and `eval.jsonl` (prompt/completion format) into a new job folder below the training root.
- `localgpt.training.run` (needs human approval) trains on a prepared job and a local safetensors base model into a new output folder. The base model is never changed and GGUF is refused.
- The dataset teaches who the model is and where it runs: application name and version, machine, operating system, user-data layout (relative folder names), Council teams and roles, registered toolchains and connected 1-Wire peers, and the habit of using the knowledge tools for exact versions. It contains no secrets, no file contents and no private notes.
- All tunables are database runtime values added on startup (existing rows untouched): Python override, trainer path, training root and file names, timeouts, list limits, `ModelTrainingDefaultsJson` (all trainer settings and the eval split), `ModelTrainingIdentityTemplate`, `ModelTrainingIdentityListFormatJson` and the `ModelTrainingScrubbedEnvironmentMarkers` collection.
- The trainer runs with secret-like environment variables, `PYTHONPATH` and `PYTHONSTARTUP` removed, a timeout and process-tree kill. Trainer output lines go to the log.
- New build guard `build/Assert-ModelTrainingContract.ps1`, wired in `Directory.Build.targets`.

## Not built here

- No autonomous queue, background worker or scheduled training.
- No import step: the trained adapter/merged model is not converted or registered with a provider yet.
- Not compiled and not run: this version was written without a .NET SDK, PowerShell or GPU. Training itself is unverified on every backend.
- The Python used is the override, else the one bound to LocalGPT, else the first discovered. It needs torch, transformers, peft, trl and datasets (see `LORA-TRAINER.md`).

## Check

- Build LocalGPT (all guards, XML documentation) and fix anything the compiler reports.
- In chat, call `localgpt.training.probe` with a model size and read the memory verdict.
- Prepare a dataset with `localgpt.training.identity.prepare`; approve it; open `train.jsonl` and `eval.jsonl` and read every line.
- On a machine whose probe says "fits", run `localgpt.training.run` with the job folder and a safetensors base model; approve it and watch the log.
