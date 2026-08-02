# LocalGPT 2.0.3 — in-chat runtime games, remote knowledge and bounded diagnostics

- Added a persistent `/Chat` ASCII game surface with fullscreen, responsive handheld layout, keyboard, touch and gamepad controls.
- Human, shared and AI autoplay modes use the same `localgpt.game.control` service/DXFunction contract. AI step delay is configurable; one renderer owns each complete turn frame.
- Reworked the ASCII corridor preset to start from a deterministic preseeded room graph instead of waiting for a large Map Architect model; critical opening/director/actor routes prefer small models and cap default active actor counts while remaining editable.
- Added case-, spacing- and punctuation-tolerant runtime-class resolution, aliases, a resolver DXFunction and default regex hints.
- Added reviewed GitHub/public-web imports with exact returned-file preview, configurable source-file regex, bounded archive/web handling, role/topic tags, existing Learn-Base extraction and Council DXFunctions.
- Added remote-source presets for the optional DOOM, LOTGD, PHP documentation and LLVM repositories; none are pulled automatically.
- Preserved the native browser context menu by default and added an explicit WebView developer-tools action.
- Compressed long SQLite cell values in the grid only; full values remain available in the row editor.
- Batched hot-path successful service diagnostics while retaining exact Trace events and immediate failure/cancellation logs.
- Isolated database seed stages in fresh DbContexts and added one concurrency retry.
- Fixed live transcript block/newline boundaries and reduced Council UI notification pressure.
- Bumped LocalGPT to 2.0.3, WebView wrapper to 2.0.2 and installer console to 2.0.1.
- Documented DMZ, firewall and least-privilege recommendations and game-source non-affiliation.

The empirical cross-hardware Ollama autotuner remains an explicit `NotImplementedException` boundary until it can be verified on real target hardware. The preseeded Reactive ASCII Gameplay preset is the supported low-latency default.
