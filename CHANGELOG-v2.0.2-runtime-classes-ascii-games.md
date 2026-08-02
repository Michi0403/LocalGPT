# LocalGPT 2.0.2 — runtime classes and ASCII Council games

- Added database-backed Council runtime-class definitions, EF mapping, migration, service, controller, and AI-readable DXFunctions.
- Added role-to-runtime-class assignment with full field ownership and input-binding previews in the Council Team editor.
- Added categorized best-use DXFunction selection to team configuration.
- Added the `ASCII DOOM Council Adventure` preset: one meaningful world step per turn and exactly one AI-owned 80x25 ASCII frame.
- Added the `Green Dragon Runtime Story` preset: locations, houses, NPCs, events, player, world state, and frames are bounded runtime-class instances orchestrated by directors.
- Documented individual opt-in installer imports for `id-Software/DOOM`, `lotgd/lotgd`, official PHP documentation, and LLVM/Clang source; none is added to the bulk recommended download list.
- Added ASCII frame marker rendering and theme-compatible terminal styling.
- Added card grouping and a mass-edit DevExpress Grid view to the DXFunction catalog.
- Changed automatic structured-JSON rendering to skip large or active Council streams, preserving UI and Stop-button responsiveness.
- Kept the reviewed-team Save button clickable while idle; missing confirmation now produces an explicit review message instead of a permanently grey action.
- Bumped the LocalGPT project and runtime-advertised version to 2.0.2.

## Validation limitation

No .NET SDK is installed in the preparation environment. The source received static consistency checks and archive integrity checks, but the user must run the normal restore/build/migration and functional test cycle locally.
