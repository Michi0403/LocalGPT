# LocalGPT 7.9.1 — Unruffled Uakari

- Seed 71 NUTS benchmark cases covering all 33 enabled default Council teams. Edit and validate the shared scenario in the benchmark panel, or customize it for an individual run.
- Keep Council model selection open. NUTS role recommendations go through approval and add eligible models while preserving existing role assignments.
- Configure LoRA training defaults, initial backend preferences, package profiles, and Python worker settings in `/install`. Store each setting in the system catalog and preserve overrides through startup seeding.
- Use the same device selection for preparation and training. Probe the installed PyTorch or MLX runtime to determine which accelerators are available and preserve the existing PyTorch build when installing training dependencies.
- Verify that the exported adapter reloads before marking a training job ready. Keep independent benchmark and approval requirements for assigning the published revision to a Council role.
