#!/usr/bin/env python3
"""LocalGPT hardware-portable offline LoRA trainer.

Subcommands
  probe  [--parameters-billions X --lora-rank N ...]   describe this machine as one JSON object
  train  CONFIG.json [--check-only]                    run LoRA training from one JSON config

Contract with the host application
  * stdout carries JSON lines only (progress events and the final result). Library chatter is
    redirected to stderr.
  * Any failure is ONE JSON object {"error": ..., "remedy": ..., ["key": ...]} as the last stdout
    line and a non-zero exit code (1 = bad input/environment, 2 = unexpected failure, 3 = cancelled).
  * Every tunable value comes from the caller's config file. This script contains no tunable
    default. The only constants are the documented memory-estimator model constants (ESTIMATOR)
    and wire-format/technical invariants (offline mode, trust_remote_code=False).
  * Never downloads anything and never writes into the base model folder.

Backends
  cuda-nvidia  PyTorch CUDA (optional bitsandbytes 4-bit QLoRA)
  rocm         PyTorch ROCm (torch.version.hip set; Linux or WSL2)
  mps          PyTorch on Apple Metal (fp16/fp32; bf16 only if a runtime test passes)
  mlx          Apple MLX via `python -m mlx_lm.lora`, including prequantized 4-bit MLX models
  cpu          PyTorch on CPU, only when the config sets allow_cpu = true

Memory estimate (used by `probe` and by the `train` pre-flight; deliberately conservative)
  P = parameters in billions, N = P * 1e9, r = LoRA rank, T = batch_size * max_sequence_length
  weights16    = 2.0 bytes * N                      (16-bit base weights)
  weights4     = 0.7 bytes * N                      (4-bit packed + scales + embeddings kept 16-bit)
  trainable    = N * r * 0.0015                     (assumes all-linear targets; overestimates big models)
  opt_state    = 16 bytes * trainable               (fp32 weights 4 + grads 4 + Adam moments 8)
  activations  = (0.8 GiB + 0.5 GiB * P) * (T / 1024) * (1 if gradient checkpointing else 3)
  logits       = 3 copies * T * vocab * 4 bytes     (vocab = config.json vocab_size, else 152000)
  overhead     = 0.7 GiB (framework/context) [+ 0.3 GiB dequantisation workspace for 4-bit]
  total        = (weights + opt_state + activations + logits + overhead) * (1.15 on mps/mlx, else 1.0)
  Available memory: free VRAM (cuda/rocm); min(recommended Metal working set, free RAM) on
  mps/mlx, falling back to 2/3 of total RAM; free RAM on cpu.
  Verdict: "fits" if total <= 0.80 * available, "tight" if total <= available, else "does-not-fit";
  "unknown" when available memory could not be determined.
"""
from __future__ import annotations

import argparse
import importlib
import importlib.util
import inspect
import json
import math
import os
import platform
import re
import shutil
import struct
import subprocess
import sys
import time
from pathlib import Path
from typing import Any

_OUT = sys.stdout  # real stdout; sys.stdout is pointed at stderr once main() starts
_T0 = time.monotonic()

BACKENDS = ("cuda-nvidia", "rocm", "mps", "mlx", "cpu")
ESTIMATOR = {
    "bytes_per_param_16bit": 2.0,
    "bytes_per_param_4bit": 0.7,
    "trainable_fraction_per_rank": 0.0015,
    "bytes_per_trainable_param": 16.0,
    "activation_gib_base": 0.8,
    "activation_gib_per_billion": 0.5,
    "reference_tokens": 1024,
    "no_checkpoint_factor": 3.0,
    "logits_copies": 3,
    "logits_bytes": 4,
    "assumed_vocab_size": 152000,
    "framework_overhead_gib": 0.7,
    "dequant_workspace_gib": 0.3,
    "unified_memory_factor": 1.15,
    "fit_ratio": 0.80,
    "unified_memory_usable_fraction": 2.0 / 3.0,
    "cpu_unknown_free_fraction": 0.5,
}
GIB = 1024 ** 3
MIB = 1024 ** 2


class TrainerError(Exception):
    """A failure with a plain-language remedy, reported as the single JSON error object."""

    def __init__(self, error: str, remedy: str, key: str | None = None, code: int = 1):
        super().__init__(error)
        self.error, self.remedy, self.key, self.code = error, remedy, key, code


class Cancelled(Exception):
    pass


def emit(event: str, **fields: Any) -> None:
    record = {"event": event, **fields, "elapsed_seconds": round(time.monotonic() - _T0, 2)}
    print(json.dumps(record, ensure_ascii=True), file=_OUT, flush=True)


def emit_error(err: TrainerError) -> None:
    obj: dict[str, Any] = {"error": err.error, "remedy": err.remedy}
    if err.key:
        obj["key"] = err.key
    print(json.dumps(obj, ensure_ascii=True), file=_OUT, flush=True)


# --------------------------------------------------------------------------- environment probing

def _dist_version(*names: str) -> str | None:
    from importlib import metadata
    for name in names:
        try:
            return metadata.version(name)
        except Exception:
            continue
    return None


def _pkg(module: str, *dists: str, do_import: bool = False) -> dict:
    """Report importability; heavy/fragile packages are really imported, others only located."""
    entry: dict[str, Any] = {"importable": False, "version": _dist_version(*(dists or (module,))), "error": None}
    try:
        if do_import:
            mod = importlib.import_module(module)
            entry["importable"] = True
            entry["version"] = getattr(mod, "__version__", None) or entry["version"]
        else:
            entry["importable"] = importlib.util.find_spec(module) is not None
    except Exception as exc:  # broken installs (missing DLL/CUDA library) land here
        entry["error"] = f"{type(exc).__name__}: {exc}"[:300]
    return entry


def _ram_mib() -> tuple[int | None, int | None]:
    """(total, available) system RAM in MiB: psutil, then /proc/meminfo, sysconf, Windows API."""
    try:
        import psutil  # type: ignore
        vm = psutil.virtual_memory()
        return int(vm.total // MIB), int(vm.available // MIB)
    except Exception:
        pass
    total = avail = None
    try:
        with open("/proc/meminfo", encoding="ascii") as handle:
            for line in handle:
                if line.startswith("MemTotal:"):
                    total = int(line.split()[1]) // 1024
                elif line.startswith("MemAvailable:"):
                    avail = int(line.split()[1]) // 1024
        if total:
            return total, avail
    except Exception:
        pass
    try:
        total = os.sysconf("SC_PAGE_SIZE") * os.sysconf("SC_PHYS_PAGES") // MIB
    except Exception:
        total = None
    if sys.platform == "win32":
        try:
            import ctypes

            class _Mem(ctypes.Structure):
                _fields_ = [("l", ctypes.c_ulong), ("load", ctypes.c_ulong), ("tp", ctypes.c_ulonglong),
                            ("ap", ctypes.c_ulonglong), ("tpf", ctypes.c_ulonglong), ("apf", ctypes.c_ulonglong),
                            ("tv", ctypes.c_ulonglong), ("av", ctypes.c_ulonglong), ("ae", ctypes.c_ulonglong)]
            m = _Mem()
            m.l = ctypes.sizeof(_Mem)
            if ctypes.windll.kernel32.GlobalMemoryStatusEx(ctypes.byref(m)):  # type: ignore[attr-defined]
                return int(m.tp // MIB), int(m.ap // MIB)
        except Exception:
            pass
    return total, avail


def _read(path: str) -> str:
    try:
        return Path(path).read_text(encoding="ascii", errors="ignore").strip()
    except Exception:
        return ""


def _list_gpus() -> list[dict]:
    """Best-effort list of installed GPUs without torch (vendor/name only)."""
    gpus: list[dict] = []
    vendors = {"0x1002": "AMD", "0x10de": "NVIDIA", "0x8086": "Intel"}
    if sys.platform.startswith("linux"):
        for card in sorted(Path("/sys/class/drm").glob("card[0-9]")):
            vid = _read(str(card / "device" / "vendor")).lower()
            if vid:
                gpus.append({"vendor": vendors.get(vid, vid), "name": None})
    elif sys.platform == "win32":
        for cmd in (["powershell", "-NoProfile", "-Command", "(Get-CimInstance Win32_VideoController).Name"],
                    ["wmic", "path", "win32_VideoController", "get", "name"]):
            try:
                out = subprocess.run(cmd, capture_output=True, text=True, timeout=15).stdout
                names = [l.strip() for l in out.splitlines() if l.strip() and l.strip().lower() != "name"]
                if names:
                    for n in names:
                        up = n.upper()
                        vendor = "AMD" if ("AMD" in up or "RADEON" in up) else "NVIDIA" if ("NVIDIA" in up or "GEFORCE" in up) else "Intel" if "INTEL" in up else "other"
                        gpus.append({"vendor": vendor, "name": n})
                    break
            except Exception:
                continue
    return gpus


def _platform_facts() -> dict:
    is_windows = sys.platform == "win32"
    is_linux = sys.platform.startswith("linux")
    is_mac = sys.platform == "darwin"
    is_wsl = is_linux and "microsoft" in platform.release().lower()
    dmi = (_read("/sys/devices/virtual/dmi/id/sys_vendor"), _read("/sys/devices/virtual/dmi/id/product_name"))
    steam_deck = is_linux and (dmi[0] == "Valve" or dmi[1] in ("Jupiter", "Galileo"))
    return {
        "os": platform.system(), "os_release": platform.release(), "os_version": platform.version()[:120],
        "machine": platform.machine(), "python": sys.version.split()[0], "python_executable": sys.executable,
        "is_windows_native": is_windows, "is_wsl": is_wsl, "is_macos": is_mac,
        "is_apple_silicon": is_mac and platform.machine().lower() in ("arm64", "aarch64"),
        "is_steam_deck": steam_deck,
    }


def _mps_bf16_works(torch: Any) -> bool:
    try:
        a = torch.ones((8, 8), dtype=torch.bfloat16, device="mps")
        return bool(torch.isfinite((a @ a).float()).all().item())
    except Exception:
        return False


def _unified_usable_mib(total: int | None, free: int | None, recommended_bytes: int | None) -> tuple[int | None, str]:
    if recommended_bytes:
        usable, basis = int(recommended_bytes // MIB), "recommended Metal working set"
    elif total:
        usable, basis = int(total * ESTIMATOR["unified_memory_usable_fraction"]), "2/3 of total RAM (heuristic)"
    else:
        return None, "unknown"
    if free is not None and free < usable:
        usable, basis = free, "free RAM"
    return usable, basis


def detect() -> dict:
    """Collect everything `probe` reports and `train` needs. Never raises."""
    real_stdout = sys.stdout
    sys.stdout = sys.stderr  # imports may print
    try:
        return _detect()
    finally:
        sys.stdout = real_stdout


def _detect() -> dict:
    facts = _platform_facts()
    packages = {
        "torch": _pkg("torch", do_import=True),
        "transformers": _pkg("transformers"),
        "peft": _pkg("peft"),
        "trl": _pkg("trl"),
        "datasets": _pkg("datasets"),
        "bitsandbytes": _pkg("bitsandbytes", do_import=True),
        "mlx": _pkg("mlx", "mlx"),
        "mlx_lm": _pkg("mlx_lm", "mlx-lm", "mlx_lm"),
        "psutil": _pkg("psutil"),
    }
    if packages["mlx"]["importable"]:  # really load the native library
        try:
            importlib.import_module("mlx.core")
        except Exception as exc:
            packages["mlx"]["importable"], packages["mlx"]["error"] = False, f"{type(exc).__name__}: {exc}"[:300]
    gpus = _list_gpus()
    ram_total, ram_free = _ram_mib()

    torch = sys.modules.get("torch") if packages["torch"]["importable"] else None
    available: list[str] = []
    detail: dict[str, dict] = {}
    torch_info: dict[str, Any] = {}
    if torch is not None:
        hip = getattr(getattr(torch, "version", None), "hip", None)
        cuda_ver = getattr(getattr(torch, "version", None), "cuda", None)
        torch_info = {"cuda_build": cuda_ver, "hip_build": hip}
        try:
            cuda_ok = bool(torch.cuda.is_available())
        except Exception:
            cuda_ok = False
        if cuda_ok:
            name = "unknown"
            try:
                name = torch.cuda.get_device_name(0)
            except Exception:
                pass
            free = total = None
            try:
                f, t = torch.cuda.mem_get_info(0)
                free, total = int(f // MIB), int(t // MIB)
            except Exception:
                pass
            try:
                bf16 = bool(torch.cuda.is_bf16_supported())
            except Exception:
                bf16 = False
            backend = "rocm" if hip else "cuda-nvidia"
            available.append(backend)
            detail[backend] = {"device_name": name, "total_mib": total, "free_mib": free, "usable_mib": free,
                               "usable_basis": "free device memory" if free is not None else "unknown",
                               "bf16": bf16, "fp16": True}
        try:
            mps_ok = bool(getattr(torch.backends, "mps", None) and torch.backends.mps.is_available())
        except Exception:
            mps_ok = False
        if mps_ok:
            rec = None
            try:
                rec = int(torch.mps.recommended_max_memory())
            except Exception:
                pass
            usable, basis = _unified_usable_mib(ram_total, ram_free, rec)
            available.append("mps")
            detail["mps"] = {"device_name": "Apple GPU (MPS)", "total_mib": ram_total, "free_mib": ram_free,
                             "usable_mib": usable, "usable_basis": basis,
                             "bf16": _mps_bf16_works(torch), "fp16": True}
    if packages["mlx"]["importable"] and packages["mlx_lm"]["importable"] and facts["is_apple_silicon"]:
        rec = None
        try:
            import mlx.core as mx  # type: ignore
            info = mx.metal.device_info() if hasattr(mx, "metal") else mx.device_info()
            rec = int(info.get("max_recommended_working_set_size", 0)) or None
        except Exception:
            pass
        usable, basis = _unified_usable_mib(ram_total, ram_free, rec)
        available.append("mlx")
        detail["mlx"] = {"device_name": "Apple GPU (MLX)", "total_mib": ram_total, "free_mib": ram_free,
                         "usable_mib": usable, "usable_basis": basis, "bf16": True, "fp16": True}
    if torch is not None:
        available.append("cpu")
        detail["cpu"] = {"device_name": platform.processor() or platform.machine() or "cpu", "total_mib": ram_total,
                         "free_mib": ram_free,
                         "usable_mib": ram_free if ram_free is not None else (
                             int(ram_total * ESTIMATOR["cpu_unknown_free_fraction"]) if ram_total else None),
                         "usable_basis": "free RAM" if ram_free is not None else "50% of total RAM (heuristic)",
                         "bf16": False, "fp16": False}

    accelerated = [b for b in available if b != "cpu"]
    # Auto order required by LocalGPT: cuda/rocm > mps > mlx. CPU is never auto-selected silently.
    order = [b for b in ("cuda-nvidia", "rocm", "mps", "mlx") if b in available]
    backend = order[0] if order else ("cpu" if "cpu" in available else "none")
    four_bit = backend == "cuda-nvidia" and packages["bitsandbytes"]["importable"]
    env = {"facts": facts, "packages": packages, "gpus": gpus, "torch_info": torch_info,
           "available_backends": available, "accelerated_backends": accelerated, "backend": backend,
           "detail": detail, "ram_total_mib": ram_total, "ram_free_mib": ram_free,
           "four_bit_available": four_bit}
    env["problems"] = _problems(env)
    return env


def _problems(env: dict) -> list[dict]:
    f, pk, out = env["facts"], env["packages"], []

    def add(severity: str, problem: str, remedy: str) -> None:
        out.append({"severity": severity, "problem": problem, "remedy": remedy})

    if sys.version_info < (3, 10):
        add("error", f"Python {f['python']} is too old.", "Install Python 3.10 or newer and recreate the training environment.")
    vendors = {g["vendor"] for g in env["gpus"]}
    accel = env["accelerated_backends"]
    tv = env["torch_info"]
    if f["is_steam_deck"]:
        add("warning", "Steam Deck detected: its AMD APU has no supported GPU training path.",
            "Train on CPU only with a tiny model: set backend to \"cpu\" and allow_cpu to true, or train on another machine.")
    if not pk["torch"]["importable"]:
        extra = f" ({pk['torch']['error']})" if pk["torch"]["error"] else ""
        add("error", "PyTorch is not installed or cannot be imported" + extra + ".",
            "Install a PyTorch build that matches the hardware (see README.md). On Apple Silicon you can alternatively install mlx and mlx-lm.")
    else:
        if f["is_windows_native"] and "AMD" in vendors and "rocm" not in accel:
            add("error", "Windows with an AMD GPU: PyTorch cannot use the GPU on native Windows here.",
                "Use WSL2 Ubuntu with a ROCm build of PyTorch (torch.cuda.is_available() must be True and torch.version.hip set), and run this trainer inside WSL2.")
        if f["is_windows_native"] and "rocm" in accel:
            add("warning", "PyTorch ROCm on native Windows is unreliable.", "Prefer WSL2 Ubuntu with ROCm PyTorch for the AMD GPU.")
        if "NVIDIA" in vendors and "cuda-nvidia" not in accel:
            if not tv.get("cuda_build"):
                add("error", "An NVIDIA GPU is present but the installed PyTorch is a CPU-only build.",
                    "Reinstall PyTorch with a CUDA wheel from https://pytorch.org/get-started/locally/ (pick the CUDA version matching the driver).")
            else:
                add("error", "An NVIDIA GPU is present but torch.cuda.is_available() is False.",
                    "Update the NVIDIA driver and make sure the PyTorch CUDA build matches the driver's CUDA version.")
        if "AMD" in vendors and f["is_wsl"] and "rocm" not in accel:
            add("warning", "AMD GPU under WSL2 but ROCm PyTorch is not active.",
                "Install the AMD WSL2/ROCm stack and a ROCm build of PyTorch; check that /dev/dxg exists.")
        if "AMD" in vendors and not f["is_windows_native"] and not f["is_wsl"] and not f["is_steam_deck"] \
                and "rocm" not in accel and not tv.get("hip_build"):
            add("warning", "An AMD GPU is present but the installed PyTorch is not a ROCm build.",
                "Install the ROCm build of PyTorch from https://pytorch.org/get-started/locally/ and a supported ROCm release.")
        if f["is_apple_silicon"] and "mps" not in accel:
            add("warning", "Apple Silicon but PyTorch MPS is not available.",
                "Use a native arm64 Python (not Rosetta) with a recent PyTorch, or use the mlx backend.")
        for lib in ("transformers", "peft", "trl", "datasets"):
            if not pk[lib]["importable"] and any(b in accel + ["cpu"] for b in env["available_backends"]):
                add("error", f"Python package '{lib}' is missing (needed by the PyTorch training path).",
                    f"pip install {lib}  (see README.md for tested version ranges).")
    if env["backend"] == "mps" and not env["detail"]["mps"]["bf16"]:
        add("warning", "bf16 does not work on this MPS/macOS version.", "precision \"auto\" will use fp16; use fp32 if the loss turns NaN.")
    if "NVIDIA" in vendors and "cuda-nvidia" in accel and not pk["bitsandbytes"]["importable"]:
        add("warning", "bitsandbytes is not installed, so 4-bit QLoRA is unavailable (optional).",
            "pip install bitsandbytes (NVIDIA only) if 4-bit training is wanted.")
    if f["is_apple_silicon"] and not (pk["mlx"]["importable"] and pk["mlx_lm"]["importable"]):
        add("warning", "MLX is not installed (optional on Apple Silicon, lower memory use than PyTorch MPS).", "pip install mlx mlx-lm")
    if not accel and "mlx" not in env["available_backends"]:
        add("warning", "No GPU training backend is available.",
            "Training is refused unless the config sets backend \"cpu\" (or \"auto\") together with allow_cpu true; expect only tiny models to be practical.")
    if env["backend"] == "none":
        add("error", "No training backend is usable on this machine.", "Install PyTorch (or mlx and mlx-lm on Apple Silicon); see README.md.")
    return out


# --------------------------------------------------------------------------- memory estimate

def estimate_memory(parameters_billions: float, lora_rank: int, tokens_per_step: int | None = None,
                    gradient_checkpointing: bool = True, vocab_size: int | None = None,
                    unified_memory: bool = False, checkpoint_bytes: int | None = None,
                    checkpoint_is_quantized: bool = False, checkpoint_compute_bytes: int = 4) -> dict:
    """Estimated memory need in MiB for FP32 LoRA, 16-bit LoRA and 4-bit QLoRA."""
    e = ESTIMATOR
    n = parameters_billions * 1e9
    tokens = tokens_per_step if tokens_per_step else e["reference_tokens"]
    trainable = n * lora_rank * e["trainable_fraction_per_rank"]
    opt = trainable * e["bytes_per_trainable_param"]
    act = (e["activation_gib_base"] + e["activation_gib_per_billion"] * parameters_billions) * GIB \
        * (tokens / e["reference_tokens"]) * (1.0 if gradient_checkpointing else e["no_checkpoint_factor"])
    logits = e["logits_copies"] * tokens * (vocab_size or e["assumed_vocab_size"]) * e["logits_bytes"]
    overhead = e["framework_overhead_gib"] * GIB
    factor = e["unified_memory_factor"] if unified_memory else 1.0
    w16 = e["bytes_per_param_16bit"] * n
    w4 = e["bytes_per_param_4bit"] * n
    estimates = {
        "lora_fp32_mib": int(math.ceil((w16 * 2 + opt + act * 2 + logits + overhead) * factor / MIB)),
        "lora_16bit_mib": int(math.ceil((w16 + opt + act + logits + overhead) * factor / MIB)),
        "qlora_4bit_mib": int(math.ceil((w4 + opt + act + logits + overhead + e["dequant_workspace_gib"] * GIB) * factor / MIB)),
        "assumed_tokens_per_step": tokens,
        "assumed_gradient_checkpointing": gradient_checkpointing,
        "assumed_vocab_size": vocab_size or e["assumed_vocab_size"],
    }
    if checkpoint_bytes is not None:
        workspace = e["dequant_workspace_gib"] * GIB if checkpoint_is_quantized else 0
        checkpoint_total = checkpoint_bytes + opt + act * (checkpoint_compute_bytes / 2) + logits + overhead + workspace
        estimates["checkpoint_mib"] = int(math.ceil(checkpoint_total * factor / MIB))
    return estimates


def verdict(need_mib: int, available_mib: int | None) -> str:
    if available_mib is None:
        return "unknown"
    if need_mib <= ESTIMATOR["fit_ratio"] * available_mib:
        return "fits"
    return "tight" if need_mib <= available_mib else "does-not-fit"


def estimate_report(env: dict, backend: str, parameters_billions: float, lora_rank: int, **kw: Any) -> dict:
    det = env["detail"].get(backend, {})
    est = estimate_memory(parameters_billions, lora_rank, unified_memory=backend in ("mps", "mlx"), **kw)
    avail = det.get("usable_mib")
    report = {
        "parameters_billions": parameters_billions, "lora_rank": lora_rank, "backend": backend,
        "available_mib": avail, "available_basis": det.get("usable_basis"),
        "assumptions": {k: est[k] for k in ("assumed_tokens_per_step", "assumed_gradient_checkpointing", "assumed_vocab_size")},
        "lora_fp32": {"need_mib": est["lora_fp32_mib"], "verdict": verdict(est["lora_fp32_mib"], avail), "available": True},
        "lora_16bit": {"need_mib": est["lora_16bit_mib"], "verdict": verdict(est["lora_16bit_mib"], avail), "available": True},
        "qlora_4bit": {"need_mib": est["qlora_4bit_mib"], "verdict": verdict(est["qlora_4bit_mib"], avail),
                       "available": bool(env["four_bit_available"] and backend == "cuda-nvidia")},
    }
    if "checkpoint_mib" in est:
        need = est["checkpoint_mib"]
        report["checkpoint"] = {"need_mib": need, "verdict": verdict(need, avail), "available": True}
    return report


# --------------------------------------------------------------------------- probe

def verify_training_api(backend: str) -> dict:
    command = "Prepare LoRA training under /install → Local AI runtimes."
    target = "MLX LoRA CLI" if backend == "mlx" else "PyTorch"
    try:
        if backend == "mlx":
            importlib.import_module("mlx.core")
            result = subprocess.run([sys.executable, "-m", "mlx_lm.lora", "--help"],
                                    capture_output=True, text=True, timeout=30, check=True)
            missing = [flag for flag in ("--train", "--data", "--fine-tune-type", "--adapter-path",
                       "--iters", "--max-seq-length", "--save-every", "--val-batches",
                       "--grad-accumulation-steps", "--mask-prompt", "--grad-checkpoint")
                       if flag not in result.stdout]
            if missing:
                return {"ready": False, "problem": "Unsupported MLX CLI options: " + ", ".join(missing), "remedy": command}
        else:
            importlib.import_module("torch")
            target = "Accelerate"
            importlib.import_module("accelerate")
            target = "Datasets"
            from datasets import load_dataset
            target = "Transformers"
            from transformers import AutoModelForCausalLM, AutoTokenizer, TrainerCallback
            target = "PEFT"
            from peft import LoraConfig, prepare_model_for_kbit_training
            target = "TRL"
            from trl import SFTConfig, SFTTrainer
            required = ((SFTConfig, ("completion_only_loss", "max_length", "eval_strategy", "use_cpu")),
                        (SFTTrainer, ("processing_class", "peft_config", "eval_dataset")))
            for function, parameters in required:
                signature = inspect.signature(function).parameters
                missing = [parameter for parameter in parameters if parameter not in signature]
                if missing:
                    return {"ready": False, "problem": f"{function.__name__} is missing " + ", ".join(missing), "remedy": command}
    except Exception as error:
        missing_module = error.name if isinstance(error, ModuleNotFoundError) else None
        problem = f"{target} could not load ({type(error).__name__}"
        if missing_module:
            problem += f", missing {missing_module}"
        return {"ready": False, "problem": problem + ").", "remedy": command}
    return {"ready": True}


def cmd_probe(args: argparse.Namespace) -> int:
    env = detect()
    backend = args.backend or env["backend"]
    det = env["detail"].get(backend, {})
    # Some third-party imports print warnings to stdout. Keep the probe JSON-only.
    from contextlib import redirect_stdout
    with redirect_stdout(sys.stderr):
        readiness = verify_training_api(backend)
    if backend not in env["available_backends"] and readiness["ready"]:
        readiness = {"ready": False, "problem": f"The {backend} device is unavailable to this interpreter.",
                     "remedy": "Use a native interpreter and a backend build supported by this machine, then check readiness again."}
    report: dict[str, Any] = {
        "event": "probe",
        **{k: env["facts"][k] for k in ("os", "os_release", "os_version", "machine", "python", "python_executable")},
        "platform": {k: env["facts"][k] for k in ("is_windows_native", "is_wsl", "is_macos", "is_apple_silicon", "is_steam_deck")},
        "packages": env["packages"], "torch_build": env["torch_info"], "gpus_seen": env["gpus"],
        "backend": backend, "available_backends": env["available_backends"],
        "training_readiness": readiness,
        "device_name": det.get("device_name"),
        "memory_total_mib": det.get("total_mib"), "memory_free_mib": det.get("free_mib"),
        "memory_usable_mib": det.get("usable_mib"), "memory_usable_basis": det.get("usable_basis"),
        "supports_bf16": bool(det.get("bf16", False)), "supports_fp16": bool(det.get("fp16", False)),
        "four_bit_available": env["four_bit_available"],
        "problems": env["problems"], "estimate": None,
        "estimator_constants": ESTIMATOR,
    }
    if args.parameters_billions is not None or args.lora_rank is not None:
        if args.parameters_billions is None or args.lora_rank is None:
            raise TrainerError("--parameters-billions and --lora-rank must be given together.",
                               "Pass both, e.g. probe --parameters-billions 0.8 --lora-rank 8.")
        if args.parameters_billions <= 0 or args.lora_rank < 1:
            raise TrainerError("--parameters-billions must be > 0 and --lora-rank >= 1.", "Pass positive values.")
        report["estimate"] = estimate_report(
            env, backend, args.parameters_billions, args.lora_rank,
            tokens_per_step=args.tokens_per_step, gradient_checkpointing=not args.no_gradient_checkpointing)
    del report["event"]
    print(json.dumps(report, ensure_ascii=True), file=_OUT, flush=True)
    return 0


# --------------------------------------------------------------------------- config

REQUIRED = {  # key -> kind
    "base_model": "str", "train_dataset": "str", "eval_dataset": "str", "output_dir": "str",
    "epochs": "int>=1", "max_steps": "int>=1", "learning_rate": "float>0", "lora_rank": "int>=1",
    "lora_alpha": "float>0", "lora_dropout": "float[0,1)", "target_modules": "modules",
    "max_sequence_length": "int>=1", "batch_size": "int>=1", "gradient_accumulation_steps": "int>=1",
    "gradient_checkpointing": "bool", "precision": ("auto", "bf16", "fp16", "fp32"),
    "quantization": ("none", "4bit"), "backend": ("auto", "cuda", "rocm", "mps", "mlx", "cpu"),
    "allow_cpu": "bool", "seed": "int", "save_steps": "int>=1", "logging_steps": "int>=1",
}
# Optional keys. mlx_num_layers is required when the backend resolves to mlx; the bnb_* keys when
# quantization is "4bit". force defaults to false; cancel_file defaults to none.
OPTIONAL = {"force", "cancel_file", "mlx_num_layers", "bnb_4bit_quant_type", "bnb_4bit_double_quant"}


def _missing(key: str, what: str) -> TrainerError:
    return TrainerError(f"Missing or invalid config key '{key}': expected {what}.",
                        f"Set \"{key}\" in the training config JSON; the trainer has no default for it.", key)


def load_config(path: str) -> dict:
    try:
        raw = json.loads(Path(path).read_text(encoding="utf-8"))
    except FileNotFoundError:
        raise TrainerError(f"Config file not found: {path}", "Pass the path of an existing JSON config file.")
    except (OSError, ValueError) as exc:
        raise TrainerError(f"Config file is not readable JSON: {exc}", "Fix the JSON syntax of the config file.")
    if not isinstance(raw, dict):
        raise TrainerError("Config must be a JSON object.", "Wrap the values in { }.")
    for key, kind in REQUIRED.items():
        if key not in raw:
            raise _missing(key, kind if isinstance(kind, str) else "one of " + "/".join(kind))
        v = raw[key]
        if isinstance(kind, tuple):
            if v not in kind:
                raise _missing(key, "one of " + "/".join(kind))
        elif kind == "str":
            if not isinstance(v, str) or not v.strip():
                raise _missing(key, "a non-empty string")
        elif kind == "bool":
            if not isinstance(v, bool):
                raise _missing(key, "true or false")
        elif kind.startswith("int"):
            if isinstance(v, bool) or not isinstance(v, int) or (kind == "int>=1" and v < 1):
                raise _missing(key, "an integer" + (" >= 1" if kind == "int>=1" else ""))
        elif kind.startswith("float"):
            ok = isinstance(v, (int, float)) and not isinstance(v, bool) and math.isfinite(v)
            if not ok or (kind == "float>0" and v <= 0) or (kind == "float[0,1)" and not 0 <= v < 1):
                raise _missing(key, "a number " + ("> 0" if kind == "float>0" else "in [0, 1)"))
        elif kind == "modules":
            if not (v == "auto" or (isinstance(v, list) and v and all(isinstance(x, str) and x for x in v))):
                raise _missing(key, "\"auto\" or a non-empty list of module names")
    unknown = sorted(set(raw) - set(REQUIRED) - OPTIONAL)
    if unknown:
        raise TrainerError(f"Unknown config key(s): {', '.join(unknown)}.", "Remove or correct the key (typo?).", unknown[0])
    if "force" in raw and not isinstance(raw["force"], bool):
        raise _missing("force", "true or false")
    if raw.get("cancel_file") is not None and not isinstance(raw["cancel_file"], str):
        raise _missing("cancel_file", "a path string")
    return raw


def _sniff_jsonl(path: Path, key: str) -> int:
    if not path.is_file():
        raise TrainerError(f"Dataset file not found: {path}", "Stage the immutable train/eval JSONL files before training.", key)
    count = 0
    with open(path, encoding="utf-8") as handle:
        for number, line in enumerate(handle, 1):
            if not line.strip():
                continue
            try:
                row = json.loads(line)
            except ValueError:
                raise TrainerError(f"{path.name} line {number} is not valid JSON.", "Regenerate the dataset as one JSON object per line.", key)
            if not isinstance(row, dict) or not isinstance(row.get("prompt"), str) or not isinstance(row.get("completion"), str):
                raise TrainerError(f"{path.name} line {number} needs string fields 'prompt' and 'completion'.",
                                   "Regenerate the dataset in prompt/completion format.", key)
            count += 1
    if count == 0:
        raise TrainerError(f"{path.name} contains no examples.", "Provide a non-empty dataset.", key)
    return count


def validate_paths(cfg: dict) -> dict:
    """Fail-closed checks carried over from the original trainer."""
    base, out = Path(cfg["base_model"]), Path(cfg["output_dir"])
    if not base.is_dir() or not (base / "config.json").is_file():
        raise TrainerError("The base model folder must be an existing local Hugging Face model with config.json.",
                           "Bind a Hugging Face safetensors checkpoint folder as base_model.", "base_model")
    shards = sorted(base.glob("*.safetensors"))
    if not shards:
        gguf = any(base.glob("*.gguf"))
        raise TrainerError("GGUF inference files are not trainable base safetensors." if gguf
                           else "No *.safetensors weights were found in the base model folder.",
                           "Acquire the original Hugging Face safetensors checkpoint for this model; GGUF files cannot be trained.",
                           "base_model")
    if out.exists():
        raise TrainerError("Refusing to overwrite any prior trainer output.", "Choose an output_dir that does not exist yet.", "output_dir")
    train_n = _sniff_jsonl(Path(cfg["train_dataset"]), "train_dataset")
    eval_n = _sniff_jsonl(Path(cfg["eval_dataset"]), "eval_dataset")
    return {"shards": shards, "train_examples": train_n, "eval_examples": eval_n}


def inspect_weights(shards: list[Path], mlx_quantization: dict | None = None) -> tuple[int, int, bool, int]:
    """Read logical parameter count and resident weight sizes directly from safetensors headers."""
    total = 0
    tensors = {}
    for shard in shards:
        try:
            with open(shard, "rb") as handle:
                (length,) = struct.unpack("<Q", handle.read(8))
                header = json.loads(handle.read(length))
        except Exception as exc:
            raise TrainerError(f"Cannot read the safetensors header of {shard.name}: {exc}",
                               "Re-copy or re-download the checkpoint; the file looks truncated or corrupt.", "base_model")
        tensors.update((name, meta) for name, meta in header.items() if name != "__metadata__")
    packed_counts = {}
    if mlx_quantization:
        for name, meta in tensors.items():
            if not name.endswith(".weight") or meta["dtype"] != "U32":
                continue
            module = name.removesuffix(".weight")
            scales = tensors.get(f"{module}.scales")
            settings = mlx_quantization.get(module, mlx_quantization)
            group_size = settings.get("group_size") if isinstance(settings, dict) else None
            if scales and isinstance(group_size, int) and group_size > 0:
                packed_counts[name] = math.prod(scales["shape"]) * group_size
    auxiliary = {f"{name.removesuffix('.weight')}.{suffix}"
                 for name in packed_counts for suffix in ("scales", "biases")}
    for name, meta in tensors.items():
        if name not in auxiliary:
            total += packed_counts.get(name, math.prod(meta["shape"]))
    stored_bytes = sum(meta["data_offsets"][1] - meta["data_offsets"][0] for meta in tensors.values())
    float_widths = {"F16": 2, "BF16": 2, "F32": 4, "F64": 8}
    compute_width = max((float_widths[meta["dtype"]] for meta in tensors.values()
                         if meta["dtype"] in float_widths), default=4)
    return total, stored_bytes, bool(packed_counts), compute_width


def vocab_size_of(base: Path) -> int | None:
    try:
        v = json.loads((base / "config.json").read_text(encoding="utf-8")).get("vocab_size")
        return int(v) if isinstance(v, int) and v > 0 else None
    except Exception:
        return None


# --------------------------------------------------------------------------- backend selection

def select_backend(env: dict, cfg: dict) -> tuple[str, list[str]]:
    requested, allow_cpu, notes = cfg["backend"], cfg["allow_cpu"], []
    avail = env["available_backends"]
    wants = {"cuda": "cuda-nvidia", "rocm": "rocm", "mps": "mps", "mlx": "mlx", "cpu": "cpu"}
    hints = "; ".join(p["remedy"] for p in env["problems"] if p["severity"] == "error") or "Run `probe` and follow its remedies."
    if requested == "auto":
        for b in ("cuda-nvidia", "rocm", "mps", "mlx"):
            if b in avail:
                return b, notes
        if allow_cpu and "cpu" in avail:
            notes.append("No GPU backend is available; training on CPU because allow_cpu is true.")
            return "cpu", notes
        raise TrainerError("No GPU training backend is available and CPU training is not allowed.",
                           hints + " To train on CPU anyway (tiny models only) set allow_cpu to true.", "backend")
    target = wants[requested]
    if target == "cpu":
        if not allow_cpu:
            raise TrainerError("backend \"cpu\" requires explicit opt-in.", "Set allow_cpu to true (only tiny models are practical on CPU).", "allow_cpu")
        if "cpu" not in avail:
            raise TrainerError("PyTorch is not importable, so CPU training is unavailable.", hints, "backend")
        notes.append("Training on CPU because allow_cpu is true.")
        return "cpu", notes
    if target not in avail:
        raise TrainerError(f"Requested backend \"{requested}\" is not available on this machine.",
                           hints + " This trainer never falls back to another backend when one is requested explicitly.", "backend")
    return target, notes


def resolve_precision(env: dict, cfg: dict, backend: str) -> str:
    p, det = cfg["precision"], env["detail"][backend]
    if backend == "mlx":
        if p != "auto":
            raise TrainerError("MLX uses the dtype stored in the model weights; a fixed precision cannot be forced.",
                               "Set precision to \"auto\" for the mlx backend (use another dtype of the checkpoint if needed).", "precision")
        return "model-dtype"
    if p == "auto":
        if det["bf16"]:
            return "bf16"
        if backend in ("cuda-nvidia", "rocm", "mps") and det["fp16"]:
            return "fp16"
        return "fp32"
    if p == "bf16" and not det["bf16"]:
        raise TrainerError(f"bf16 is not supported on backend {backend}.", "Use precision \"fp16\", \"fp32\" or \"auto\".", "precision")
    if p == "fp16" and not det["fp16"]:
        raise TrainerError(f"fp16 training is not supported on backend {backend}.", "Use precision \"fp32\" or \"auto\".", "precision")
    return p


# --------------------------------------------------------------------------- training: torch path

def _check_cancel(cfg: dict) -> None:
    cf = cfg.get("cancel_file")
    if cf and os.path.exists(cf):
        raise Cancelled()


def train_torch(cfg: dict, backend: str, precision: str, info: dict) -> dict:
    missing = [m for m in ("transformers", "peft", "trl", "datasets") if importlib.util.find_spec(m) is None]
    if missing:
        raise TrainerError(f"Missing Python package(s): {', '.join(missing)}.", f"pip install {' '.join(missing)} (see README.md).")
    import torch
    from datasets import load_dataset
    from peft import LoraConfig
    from transformers import AutoModelForCausalLM, AutoTokenizer, TrainerCallback
    from trl import SFTConfig, SFTTrainer

    if backend == "mps":
        os.environ.setdefault("PYTORCH_ENABLE_MPS_FALLBACK", "1")
    out = Path(cfg["output_dir"])
    out.mkdir(parents=True)  # raises if it appeared meanwhile
    dtype = {"bf16": torch.bfloat16, "fp16": torch.float16, "fp32": torch.float32}[precision]
    gpu = backend in ("cuda-nvidia", "rocm")

    train = load_dataset("json", data_files=cfg["train_dataset"], split="train")
    evaluation = load_dataset("json", data_files=cfg["eval_dataset"], split="train")
    tokenizer = AutoTokenizer.from_pretrained(cfg["base_model"], local_files_only=True, trust_remote_code=False)
    if tokenizer.pad_token is None:
        tokenizer.pad_token = tokenizer.eos_token

    kwargs: dict[str, Any] = {"local_files_only": True, "trust_remote_code": False}
    four = cfg["quantization"] == "4bit"
    if four:
        from transformers import BitsAndBytesConfig
        kwargs["quantization_config"] = BitsAndBytesConfig(
            load_in_4bit=True, bnb_4bit_quant_type=cfg["bnb_4bit_quant_type"],
            bnb_4bit_use_double_quant=cfg["bnb_4bit_double_quant"], bnb_4bit_compute_dtype=dtype)
        kwargs["device_map"] = {"": 0}
    emit("loading_model", backend=backend, precision=precision, quantization=cfg["quantization"])
    try:
        model = AutoModelForCausalLM.from_pretrained(cfg["base_model"], dtype=dtype, **kwargs)
    except TypeError:  # older transformers call the argument torch_dtype
        model = AutoModelForCausalLM.from_pretrained(cfg["base_model"], torch_dtype=dtype, **kwargs)
    if hasattr(model.config, "use_cache"):
        model.config.use_cache = False
    if four:
        from peft import prepare_model_for_kbit_training
        model = prepare_model_for_kbit_training(model, use_gradient_checkpointing=cfg["gradient_checkpointing"])

    sft = dict(
        output_dir=str(out / "checkpoints"), num_train_epochs=cfg["epochs"], max_steps=cfg["max_steps"],
        per_device_train_batch_size=cfg["batch_size"], per_device_eval_batch_size=cfg["batch_size"],
        gradient_accumulation_steps=cfg["gradient_accumulation_steps"], learning_rate=cfg["learning_rate"],
        max_length=cfg["max_sequence_length"], eval_strategy="epoch", save_strategy="steps",
        save_steps=cfg["save_steps"], logging_steps=cfg["logging_steps"], report_to="none", seed=cfg["seed"],
        completion_only_loss=True, gradient_checkpointing=cfg["gradient_checkpointing"],
        bf16=(precision == "bf16" and gpu), fp16=(precision == "fp16" and gpu), use_cpu=(backend == "cpu"),
    )
    if cfg["gradient_checkpointing"]:
        sft["gradient_checkpointing_kwargs"] = {"use_reentrant": False}
    adapter = LoraConfig(
        r=cfg["lora_rank"], lora_alpha=cfg["lora_alpha"], lora_dropout=cfg["lora_dropout"], bias="none",
        task_type="CAUSAL_LM", target_modules="all-linear" if cfg["target_modules"] == "auto" else cfg["target_modules"])

    class Progress(TrainerCallback):
        def on_log(self, args, state, control, logs=None, **_):
            logs = logs or {}
            if "loss" in logs:
                loss = float(logs["loss"])
                if not math.isfinite(loss):
                    raise TrainerError("Training loss became NaN/inf.",
                                       "Lower learning_rate, or set precision to \"fp32\" (fp16 on MPS is numerically fragile).", "precision")
                emit("progress", step=int(state.global_step), max_steps=int(state.max_steps), loss=loss,
                     learning_rate=logs.get("learning_rate"), epoch=logs.get("epoch"))

        def on_step_end(self, args, state, control, **_):
            _check_cancel(cfg)

        def on_save(self, args, state, control, **_):
            emit("checkpoint", step=int(state.global_step))

    trainer = SFTTrainer(model=model, args=SFTConfig(**sft), train_dataset=train, eval_dataset=evaluation,
                         peft_config=adapter, processing_class=tokenizer, callbacks=[Progress()])
    baseline = float(trainer.evaluate().get("eval_loss", float("nan")))
    if not math.isfinite(baseline):
        raise TrainerError("Held-out baseline loss is missing or not finite.", "Check the eval dataset and the precision setting.")
    emit("baseline_eval", eval_loss=baseline)
    result = trainer.train()
    final = float(trainer.evaluate().get("eval_loss", float("nan")))
    if not math.isfinite(final):
        raise TrainerError("Held-out evaluation loss is missing or not finite.", "Lower learning_rate or use precision \"fp32\".")
    trainer.model.save_pretrained(str(out / "adapter"), safe_serialization=True)
    tokenizer.save_pretrained(str(out / "adapter"))
    merged = False
    if not four:  # a 4-bit base cannot be merged losslessly; reload the 16-bit base to merge elsewhere
        trainer.model.merge_and_unload(safe_merge=True).save_pretrained(str(out / "merged"), safe_serialization=True)
        tokenizer.save_pretrained(str(out / "merged"))
        merged = True
    return {"train_loss": float(result.training_loss), "baseline_eval_loss": baseline, "eval_loss": final,
            "adapter_path": str(out / "adapter"), "merged_path": str(out / "merged") if merged else None}


# --------------------------------------------------------------------------- training: mlx path

_MLX_LOSS_VALUE = r"[+-]?(?:\d+(?:\.\d*)?(?:e[+-]?\d+)?|nan|inf(?:inity)?)"
_MLX_LEGACY_LOSS = re.compile(rf"\bIter\s+(\d+):\s+(Train|Val)\s+loss\s+({_MLX_LOSS_VALUE})(?=\s|,|$)", re.I)
_MLX_VAL_ROW = re.compile(rf"\s*(\d+)\s+val\s+({_MLX_LOSS_VALUE})\s+\d+(?:\.\d+)?s\s*", re.I)
_MLX_TRAIN_ROW = re.compile(rf"\s*(\d+)\s+({_MLX_LOSS_VALUE})\s+[▲▼]\s+[\d,]+\s+\d+(?:\.\d+)?k\s*", re.I)
_MLX_ANSI = re.compile(r"\x1b\[[0-?]*[ -/]*[@-~]")


def parse_mlx_loss(line: str) -> tuple[int, str, float] | None:
    """Read loss from either MLX-LM's legacy messages or its colored progress table."""
    clean = _MLX_ANSI.sub("", line)
    legacy = _MLX_LEGACY_LOSS.search(clean)
    if legacy:
        return int(legacy[1]), legacy[2].lower(), float(legacy[3])
    validation = _MLX_VAL_ROW.fullmatch(clean)
    if validation:
        return int(validation[1]), "val", float(validation[2])
    training = _MLX_TRAIN_ROW.fullmatch(clean)
    if training:
        return int(training[1]), "train", float(training[2])
    return None


def train_mlx(cfg: dict, info: dict) -> dict:
    """Run the MLX-LM LoRA CLI and report the child's observed validation losses."""
    out = Path(cfg["output_dir"])
    out.mkdir(parents=True)
    data = out / "mlx_data"
    data.mkdir()
    shutil.copyfile(cfg["train_dataset"], data / "train.jsonl")
    shutil.copyfile(cfg["eval_dataset"], data / "valid.jsonl")
    adapter_dir = out / "adapter"
    lora_parameters = {"rank": cfg["lora_rank"], "scale": cfg["lora_alpha"] / cfg["lora_rank"], "dropout": cfg["lora_dropout"]}
    lines = ["lora_parameters:"] + [f"  {k}: {v}" for k, v in lora_parameters.items()]
    if cfg["target_modules"] != "auto":
        lines.append("  keys: " + json.dumps(cfg["target_modules"]))
    (out / "mlx_lora_config.yaml").write_text("\n".join(lines) + "\n", encoding="utf-8")
    cmd = [sys.executable, "-m", "mlx_lm.lora", "--model", cfg["base_model"], "--train", "--data", str(data),
           "--fine-tune-type", "lora", "--adapter-path", str(adapter_dir), "-c", str(out / "mlx_lora_config.yaml"),
           "--iters", str(cfg["max_steps"]), "--batch-size", str(cfg["batch_size"]),
           "--learning-rate", repr(float(cfg["learning_rate"])), "--max-seq-length", str(cfg["max_sequence_length"]),
           "--num-layers", str(cfg["mlx_num_layers"]), "--seed", str(cfg["seed"]),
           "--steps-per-report", str(cfg["logging_steps"]), "--save-every", str(cfg["save_steps"]),
           "--steps-per-eval", str(cfg["max_steps"]), "--val-batches", "-1",
           "--grad-accumulation-steps", str(cfg["gradient_accumulation_steps"]), "--mask-prompt"]
    if cfg["gradient_checkpointing"]:
        cmd.append("--grad-checkpoint")
    env = dict(os.environ, HF_HUB_OFFLINE="1", TRANSFORMERS_OFFLINE="1", PYTHONUNBUFFERED="1")
    emit("loading_model", backend="mlx", note="epochs is ignored by mlx_lm; max_steps is used as --iters")
    proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, env=env, bufsize=1)
    vals: list[float] = []
    train_losses: list[float] = []
    tail: list[str] = []
    assert proc.stdout is not None
    try:
        for line in proc.stdout:
            print(line.rstrip(), file=sys.stderr)
            tail = (tail + [line.rstrip()])[-15:]
            report = parse_mlx_loss(line)
            if report is not None:
                step, kind, loss = report
                if not math.isfinite(loss):
                    proc.kill()
                    raise TrainerError("Training loss became NaN/inf.", "Lower learning_rate or use a smaller model.", "learning_rate")
                if kind == "train":
                    train_losses.append(loss)
                    emit("progress", step=step, max_steps=cfg["max_steps"], loss=loss)
                else:
                    vals.append(loss)
                    emit("eval", step=step, eval_loss=loss)
            _check_cancel(cfg)
    except Cancelled:
        proc.kill()
        raise
    if proc.wait() != 0:
        raise TrainerError("mlx_lm.lora failed: " + " | ".join(tail[-4:])[:600],
                           "Check that mlx-lm is installed and recent (pip install -U mlx-lm), that the model architecture is supported, "
                           "and that this trainer's assumed flags match your mlx-lm version (`python -m mlx_lm.lora --help`).")
    if not (adapter_dir / "adapters.safetensors").is_file() or len(vals) < 2:
        raise TrainerError("mlx_lm.lora finished but no adapter or baseline/final validation loss was found.",
                           "Check the stderr log for the adapter save and the initial/final validation passes.")
    return {"train_loss": train_losses[-1] if train_losses else None, "baseline_eval_loss": vals[0], "eval_loss": vals[-1],
            "adapter_path": str(adapter_dir), "merged_path": None}


# --------------------------------------------------------------------------- train command

def cmd_train(args: argparse.Namespace) -> int:
    os.environ["HF_HUB_OFFLINE"] = "1"
    os.environ["HF_DATASETS_OFFLINE"] = "1"
    os.environ["TRANSFORMERS_OFFLINE"] = "1"
    os.environ["WANDB_DISABLED"] = "true"
    cfg = load_config(args.config)
    info = validate_paths(cfg)
    env = detect()
    backend, notes = select_backend(env, cfg)
    for n in notes:
        emit("notice", message=n)
    precision = resolve_precision(env, cfg, backend)
    four = cfg["quantization"] == "4bit"
    if four and not (backend == "cuda-nvidia" and env["four_bit_available"]):
        raise TrainerError("4-bit QLoRA needs an NVIDIA CUDA GPU with bitsandbytes installed.",
                           "Install bitsandbytes on the NVIDIA machine, or set quantization to \"none\" (ROCm, MPS, MLX and CPU do not support it here).",
                           "quantization")
    if four:
        for key in ("bnb_4bit_quant_type", "bnb_4bit_double_quant"):
            if key not in cfg:
                raise _missing(key, "a value (required when quantization is \"4bit\")")
    if backend == "mlx":
        if four:
            raise TrainerError("quantization \"4bit\" is not applied by the mlx backend.",
                               "Use an already MLX-quantized checkpoint as base_model and set quantization to \"none\".", "quantization")
        if not isinstance(cfg.get("mlx_num_layers"), int) or isinstance(cfg.get("mlx_num_layers"), bool):
            raise _missing("mlx_num_layers", "an integer (number of layers to adapt, -1 for all; required for the mlx backend)")
    model_path = Path(cfg["base_model"])
    model_config = json.loads((model_path / "config.json").read_text(encoding="utf-8")) if backend == "mlx" else None
    mlx_quantization = model_config.get("quantization") if isinstance(model_config, dict) else None
    if not isinstance(mlx_quantization, dict):
        mlx_quantization = None
    params, stored_bytes, mlx_packed, compute_width = inspect_weights(info["shards"], mlx_quantization)
    est = estimate_report(
        env, backend, params / 1e9, cfg["lora_rank"],
        tokens_per_step=cfg["batch_size"] * cfg["max_sequence_length"],
        gradient_checkpointing=cfg["gradient_checkpointing"], vocab_size=vocab_size_of(model_path),
        checkpoint_bytes=stored_bytes if backend == "mlx" else None,
        checkpoint_is_quantized=mlx_packed, checkpoint_compute_bytes=compute_width)
    if backend == "mlx":
        chosen = est["checkpoint"]
    elif four:
        chosen = est["qlora_4bit"]
    elif precision == "fp32":
        chosen = est["lora_fp32"]
    else:
        chosen = est["lora_16bit"]
    emit("preflight", backend=backend, device_name=env["detail"][backend]["device_name"], precision=precision,
         quantization=cfg["quantization"], prequantized_mlx=mlx_packed, parameters_billions=round(params / 1e9, 4),
         estimated_need_mib=chosen["need_mib"], available_mib=est["available_mib"], verdict=chosen["verdict"],
         train_examples=info["train_examples"], eval_examples=info["eval_examples"])
    if chosen["verdict"] == "does-not-fit" and not cfg.get("force", False):
        raise TrainerError(
            f"The model is estimated to need {chosen['need_mib']} MiB but only {est['available_mib']} MiB is available on {backend}.",
            "Use a smaller model, a smaller lora_rank/batch_size/max_sequence_length, enable gradient_checkpointing"
            + ("" if four else ", or quantization \"4bit\" on an NVIDIA GPU") + ". Set \"force\": true to try anyway.", "force")
    if chosen["verdict"] == "unknown":
        emit("notice", message="Available memory could not be determined; the memory pre-flight was skipped.")
    if chosen["verdict"] == "tight":
        emit("notice", message="Memory is tight; out-of-memory failures are possible.")
    if args.check_only:
        emit("check_ok", backend=backend, precision=precision)
        return 0

    sys.stdout = sys.stderr
    emit("start", backend=backend, max_steps=cfg["max_steps"])
    try:
        res = train_mlx(cfg, info) if backend == "mlx" else train_torch(cfg, backend, precision, info)
    except Cancelled:
        emit("cancelled")
        return 3
    metrics = {**res, "train_examples": info["train_examples"], "eval_examples": info["eval_examples"],
               "epochs_requested": cfg["epochs"], "steps_requested": cfg["max_steps"], "backend": backend,
               "precision": precision, "quantization": cfg["quantization"], "adapter_type": "LoRA",
               "requires_independent_benchmark": True}
    (Path(cfg["output_dir"]) / "metrics.json").write_text(json.dumps(metrics, indent=2), encoding="utf-8")
    emit("result", status="adapter_ready", **metrics)
    return 0


# --------------------------------------------------------------------------- entry point

def main(argv: list[str] | None = None) -> int:
    global _OUT
    parser = argparse.ArgumentParser(description="LocalGPT hardware-portable offline LoRA trainer")
    sub = parser.add_subparsers(dest="command", required=True)
    p = sub.add_parser("probe", help="describe this machine as one JSON object")
    p.add_argument("--backend", choices=["cuda-nvidia", "rocm", "mps", "mlx", "cpu"])
    p.add_argument("--parameters-billions", type=float)
    p.add_argument("--lora-rank", type=int)
    p.add_argument("--tokens-per-step", type=int)
    p.add_argument("--no-gradient-checkpointing", action="store_true")
    t = sub.add_parser("train", help="train from one JSON config file")
    t.add_argument("config")
    t.add_argument("--check-only", action="store_true", help="validate and run the memory pre-flight, then stop")
    try:
        args = parser.parse_args(argv)
    except SystemExit as exc:
        if exc.code not in (0, None):
            emit_error(TrainerError("Invalid command line.", "Run with --help.", code=1))
            return 1
        return 0
    _OUT = sys.stdout
    sys.stdout = sys.stderr  # keep stdout JSON-only
    try:
        return cmd_probe(args) if args.command == "probe" else cmd_train(args)
    except TrainerError as err:
        emit_error(err)
        return err.code
    except Cancelled:
        emit("cancelled")
        return 3
    except Exception as exc:  # unexpected: still a single JSON error object
        emit_error(TrainerError(f"{type(exc).__name__}: {exc}"[:600],
                                "See the stderr log for the traceback; run `probe` to check the environment.", code=2))
        import traceback
        traceback.print_exc(file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
