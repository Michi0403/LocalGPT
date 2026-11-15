# LocalGPT 7.2.3

- Add a hardware-portable LoRA trainer script, `Runtime/Python/localgpt_lora_trainer.py`, shipped next to the runtime bridge. It is the first step of the model-adaptation work; LocalGPT does not call it yet.
- `probe` reports the backend (NVIDIA CUDA, AMD ROCm through WSL2, Apple MPS or MLX, CPU), free memory, installed packages, problems with remedies, and whether a model of a given size fits for 16-bit LoRA or 4-bit QLoRA.
- `train` reads every training value from one JSON configuration and stops with an error that names a missing key. It never falls back to the CPU unless `allow_cpu` is set, refuses GGUF bases, never writes into the base model or an existing output folder, and refuses a run that does not fit in memory unless `force` is set. Progress and results are JSON lines.
- Reviewed and rewritten from the ChatGPT training patch; the patch's C# services, autonomous worker and Python tests are not included.

Not run here with a GPU on any backend. The memory estimator's coefficients are still built into the script and deliberately conservative (a 0.8B model may be reported as not fitting on 8 GB); they move to configuration with the C# integration.

## Check on each machine
- [ ] `python localgpt_lora_trainer.py probe --parameters-billions 0.8 --lora-rank 8` prints one JSON object with the expected backend and remedies.
