#!/usr/bin/env python3
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1]
def read(rel):
 p=ROOT/rel
 if not p.is_file(): raise SystemExit(f'ASCII color audit failed: missing {rel}')
 return p.read_text(encoding='utf-8')
def main():
 models=read('src/LocalGPT/BusinessObjects/CouncilAsciiColorModels.cs')
 game_models=read('src/LocalGPT/BusinessObjects/CouncilGameModels.cs')
 display_models=read('src/LocalGPT/BusinessObjects/CouncilGameDisplayModels.cs')
 project_models=read('src/LocalGPT/BusinessObjects/GameProjectModels.cs')
 color=read('src/LocalGPT/Services/CouncilGameSessionService.Color.cs')
 sessions=read('src/LocalGPT/Services/CouncilGameSessionService.cs')
 snapshots=read('src/LocalGPT/Services/CouncilGameSessionService.Snapshots.cs')
 display=read('src/LocalGPT/Services/CouncilGameSessionService.Display.cs')
 funcs=read('src/LocalGPT/Services/CouncilGameDisplayDxAiFunctions.cs')
 game_funcs=read('src/LocalGPT/Services/CouncilGameDxAiFunctions.cs')
 js=read('src/LocalGPT/wwwroot/js/localgpt-game-console.js')
 razor=read('src/LocalGPT/Components/Shared/ChatGameConsole.razor')
 projects=read('src/LocalGPT/Components/Pages/Projects.razor')
 project_service=read('src/LocalGPT/Services/GameProjectService.cs')
 migration=read('src/LocalGPT/Migrations/20260919133000_AddGameProjectAsciiColor.cs')
 snapshot=read('src/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs')
 surface=read('src/LocalGPT/Services/ChatAsciiDxAiFunctions.cs')
 knowledge=read('docs/reference/ascii-game-authoring.md')
 catalog=read('src/LocalGPT/Services/Persistence/InitialDataCatalog.cs')
 csproj=read('src/LocalGPT/LocalGPT.csproj')
 teams=read('src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.GameProjectTemplates.cs')
 hotseat=read('src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.AsciiHotSeatTemplate.cs')
 seed=read('src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs')
 tournament=read('src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs')
 checks=[
 ('stable enum zero', 'TerminalDefault = 0' in models), ('stable enum one', 'Ansi16 = 1' in models), ('stable enum two', 'Indexed256 = 2' in models),
 ('style fg', 'ForegroundColor' in models), ('style bg', 'BackgroundColor' in models), ('style emphasis', all(x in models for x in ['Bold','Dim','Invert'])),
 ('bounded run model', all(x in models for x in ['class CouncilAsciiStyleRun','public int Y','public int X','public int Length'])),
 ('palette contract', 'class CouncilAsciiPaletteSnapshot' in models), ('run cap', 'MaximumAsciiStyleRuns = 4096' in color),
 ('terminal compatibility', "LocalGPT's existing black/green terminal appearance" in color), ('ansi default', 'DefaultForegroundColor = 10' in color), ('indexed default', 'DefaultForegroundColor = 46' in color),
 ('style clipping', 'NormalizeAsciiStyleRuns' in color and 'Math.Min(frameWidth' in color), ('semantic styles', 'BuildSemanticAsciiStyleRuns' in color),
 ('doom semantics', 'MZE' in color or 'MZ' in color), ('story semantics', 'DRAGON' in color), ('tournament semantics', 'champion' in color.lower()),
 ('session request palette', 'CouncilAsciiColorMode AsciiColorMode' in game_models and 'DefaultForegroundColor' in game_models),
 ('snapshot palette', 'FrameStyleRuns' in game_models and 'AnimationFrameStyleRuns' in game_models),
 ('display style request', display_models.count('CouncilAsciiTextStyle? Style') >= 4), ('animation styles', 'FrameStyleRuns' in display_models and 'SubtitleStyle' in display_models),
 ('project profile palette', project_models.count('CouncilAsciiColorMode AsciiColorMode') >= 3),
 ('project validation', 'Enum.IsDefined(typeof(CouncilAsciiColorMode)' in project_service), ('project build copy', 'AsciiColorMode = profile.AsciiColorMode' in project_service),
 ('session consumes definition', 'definition?.AsciiColorMode ?? request.AsciiColorMode' in sessions), ('snapshot resolves runs', 'ResolveFrameStyleRuns' in snapshots),
 ('display readback', 'AsciiColorMode = session.AsciiColorMode' in display and 'StyleRuns = CropAsciiStyleRuns' in display),
 ('style-aware text write', 'ApplyAsciiStyleRange' in display), ('animation accepts runs', 'request.FrameStyleRuns' in display),
 ('palette dxfunction', 'localgpt.game.display.palette.get' in funcs), ('write style schema', '"style":{"$ref":"#/$defs/style"}' in funcs),
 ('frame style schema', '"styleRuns"' in game_funcs), ('frame palette schema', '"asciiColorMode"' in game_funcs), ('start palette schema', game_funcs.count('"asciiColorMode"') >= 2),
 ('surface advertises color', 'supportsAsciiColor = true' in surface and 'paletteFunction = "localgpt.game.display.palette.get"' in surface),
 ('safe js ansi palette', 'ansi16Palette' in js), ('safe js xterm', 'indexedColor(index)' in js), ('safe dom spans', "document.createElement('span')" in js and 'document.createTextNode' in js),
 ('no html injection renderer', '.innerHTML' not in js), ('static frame js', 'setFramePresentation' in js and 'setFramePresentation' in razor),
 ('animation style js', 'sequenceFrameStyleRuns' in js and 'AnimationFrameStyleRuns' in razor), ('subtitle style js', 'sequenceSubtitleStyle' in js and 'AnimationSubtitleStyle' in razor),
 ('projects ui mode', 'Terminal default (LocalGPT black/green)' in projects), ('projects ui index bounds', 'GameProfileEdit.AsciiColorMode == CouncilAsciiColorMode.Ansi16 ? 15 : 255' in projects),
 ('migration fields', all(x in migration for x in ['AsciiColorMode','DefaultForegroundColor','DefaultBackgroundColor'])), ('snapshot fields', all(x in snapshot for x in ['AsciiColorMode','DefaultForegroundColor','DefaultBackgroundColor'])),
 ('knowledge source', '0.8B, 1B and 2B' in knowledge), ('knowledge evidence order', 'Project' in knowledge and 'localgpt.game.display.palette.get' in knowledge and 'localgpt.regex.test' in knowledge),
 ('knowledge approved', 'docs/reference/ascii-game-authoring.md' in catalog), ('knowledge embedded', 'LocalGPT.Knowledge.ascii-game-authoring.md' in catalog and 'LocalGPT.Knowledge.ascii-game-authoring.md' in csproj),
 ('small model discovery', '0.8B-2B' in teams and 'game-discovery-context' in teams), ('small model development', 'game-development-baseline' in teams and 'localgpt.game.display.palette.get' in teams),
 ('small model engine extension', 'engine-extension-boundary' in teams and 'localgpt.knowledge.list' in teams), ('small model playtest', 'game-playtest-baseline' in teams and 'localgpt.regex.get' in teams),
 ('hotseat palette', 'localgpt.game.display.palette.get' in hotseat), ('doom/dragon palette', seed.count('localgpt.game.display.palette.get') >= 4),
 ('tournament movie styling', 'AnimationFrameStyleRuns' in tournament and 'AnimationSubtitleStyle' in tournament),
 ('plain canonical guidance', 'Never emit raw ANSI escape sequences or HTML into frame text.' in color),
 ]
 failed=[name for name,ok in checks if not ok]
 if failed:
  for name in failed: print('FAIL:',name)
  raise SystemExit(f'ASCII color/game-authoring audit failed: {len(failed)}/{len(checks)} checks failed.')
 print(f'ASCII color/game-authoring audit passed: {len(checks)} checks.')
 return 0
if __name__=='__main__': sys.exit(main())
