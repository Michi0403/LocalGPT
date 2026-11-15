# LocalGPT LoRA trainer (portable)

One script, `localgpt_lora_trainer.py`, Python 3.10+. It never downloads anything and never changes the base model.

```
python localgpt_lora_trainer.py probe --parameters-billions 0.8 --lora-rank 8
python localgpt_lora_trainer.py train config.json [--check-only]
```

`probe` prints one JSON object: your OS, which libraries import, the chosen backend, memory, bf16/fp16/4-bit support, a list of problems with fixes, and a memory verdict ("fits", "tight", "does-not-fit") for 16-bit LoRA and 4-bit QLoRA. Run it first on every machine.

`train` reads one JSON file. Every value is required (no defaults): `base_model`, `train_dataset`, `eval_dataset`, `output_dir`, `epochs`, `max_steps`, `learning_rate`, `lora_rank`, `lora_alpha`, `lora_dropout`, `target_modules` (list or `"auto"`), `max_sequence_length`, `batch_size`, `gradient_accumulation_steps`, `gradient_checkpointing`, `precision` (`auto|bf16|fp16|fp32`), `quantization` (`none|4bit`), `backend` (`auto|cuda|rocm|mps|mlx|cpu`), `allow_cpu`, `seed`, `save_steps`, `logging_steps`.
Extra keys: `force` (true skips the memory refusal), `cancel_file` (training stops if this file appears), `mlx_num_layers` (required for MLX), `bnb_4bit_quant_type` and `bnb_4bit_double_quant` (required for 4bit). `max_steps` decides how long training runs.

Output is JSON lines on stdout (`event`, `step`, `loss`, `elapsed_seconds`). A failure is one JSON object with `error` and `remedy` and a non-zero exit code (1 bad input or environment, 2 unexpected, 3 cancelled). Results go into `output_dir/adapter`, plus `merged/` (PyTorch, not 4-bit) and `metrics.json`. The output folder must not exist yet.

## Which machine uses what

| Machine | Backend | Install (all versions: verify) | Realistic model size |
|---|---|---|---|
| Windows PC, Radeon 7900 XTX 24 GB | `rocm`, inside WSL2 Ubuntu | In WSL2: AMD ROCm for WSL, then the ROCm build of PyTorch from pytorch.org; `pip install transformers peft trl datasets psutil`. Check `torch.cuda.is_available()` is True and `torch.version.hip` is set. Native Windows is reported as a problem, not used. | up to about 7B with 16-bit LoRA (tight); 1-3B comfortable |
| Windows laptop, RTX 3060 (6 or 12 GB) | `cuda` | CUDA build of PyTorch from pytorch.org, same pip packages. Optional: `pip install bitsandbytes` for `quantization: "4bit"` (NVIDIA only). | 6 GB: about 0.5B 16-bit, up to about 1-3B with 4-bit. 12 GB: up to about 3B 16-bit, 7B with 4-bit |
| MacBook, Apple Silicon 8 GB | `mlx` (recommended, set it explicitly) or `mps` | MLX: `pip install mlx mlx-lm`. MPS: `pip install torch transformers peft trl datasets`. With `backend: "auto"` the order is cuda/rocm, then mps, then mlx. | 0.5-1B only. bf16 often missing on MPS, so fp16 or fp32 is used |
| Steam Deck, 16 GB shared | `cpu`, explicit opt-in | CPU PyTorch plus the pip packages; set `backend: "cpu"` and `allow_cpu: true`. | tiny models, about 0.5B or smaller; slow |

`auto` never falls back to CPU unless `allow_cpu` is true, and says so with a notice event.

## Notes

- The memory estimate is documented at the top of the script and is deliberately conservative. The training pre-flight reads the exact parameter count from the safetensors headers and refuses a "does-not-fit" verdict unless `force` is true.
- The PyTorch path assumes recent `transformers`, `peft` and `trl` (the original trainer's `SFTConfig(max_length=..., completion_only_loss=...)`, so trl 0.20 or newer is assumed).
- The MLX path runs `python -m mlx_lm.lora` and assumes mlx-lm 0.21 or newer flags and its log format. It writes the adapter only (no merged model).
- Under WSL2, pass Linux paths (`/mnt/c/...`) in the config.
- Not verified on real hardware: GPU training on every backend, bitsandbytes, MPS fp16, and the MLX command line.
