# LocalGPT 2.3.12 source validation

- Keeps the 2.3.11 automatic, version-matched GitHub Pages snapshot seeding design.
- Fixes the MSBuild snapshot output path so `.github/pages/localgpt-kawaii-docs.zip` is rooted below the repository instead of being concatenated as `src.github/...`.
- Keeps the successful HumanCollaborationInbox render-mode and LocalPathExplorer text-service ownership fixes from 2.3.10.
- Uses the existing `_logger` field in `DevExpressChatService` resilience catches, avoiding the primary-constructor capture warning without changing behavior.
- The tracked older Kawaii snapshot is intentionally not relabeled; the first successful 2.3.12 Debug/Release build will generate and seed the real versioned snapshot.
- Source package contains no build output directories or compiled DLL/EXE/PDB artifacts.
