# LocalGPT 7.9.2 — Unruffled Uakari

The uakari tidied up the toolbox.

- Restore through the configured NuGet feeds on macOS. Development and release builds can use a fresh package cache when an existing cache is damaged.
- Limit prebuild cleanup to `bin` and `obj` directories belonging to actual projects. Skip linked directories during the sweep.
- Validate the application and installer on macOS without including the Windows wrapper. Run Windows solution validation serially to prevent shared projects from writing to the same build outputs at once.

## Since 7.8.2

- Keep new Chat messages and active Council sessions separate. Let Council presets select from the models available on each installation.
- Register discovered SDKs through `/install` and show progress for local model setup. Deliver PublisherStudio artifacts back to the originating Chat through approved 1-Wire operations.
- Seed 71 NUTS benchmark cases across 33 default Council teams. Review suggested role assignments through approvals and keep the current team members when adding a model.
- Configure LoRA backends, package profiles, and worker settings in `/install`. Check that a trained adapter reloads before it is eligible for a separate benchmark and Council role approval.
