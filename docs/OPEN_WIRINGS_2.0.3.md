# Open wiring register

The former `localgpt.models.benchmark.autotune` placeholder was completed in LocalGPT 2.1.10.
It now runs only after fresh human confirmation, only against models already installed in a loopback Ollama runtime, and only saves a new preset when requested. It does not pull models or overwrite an existing preset.

No deliberately registered `NotImplementedException` wiring remains from the 2.0.3 game/runtime-class work.
