#!/usr/bin/env python3
"""Static release gate for the configurable ASCII DOOM campaign and runtime ownership contract."""
from __future__ import annotations

import argparse
import re
from pathlib import Path


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path("."))
    args = parser.parse_args()
    root = args.root.resolve()
    checks: list[tuple[str, bool]] = []

    def read(relative: str) -> str:
        path = root / relative
        checks.append((f"file exists: {relative}", path.is_file()))
        return path.read_text(encoding="utf-8") if path.is_file() else ""

    models = read("src/LocalGPT/BusinessObjects/CouncilGameModels.cs")
    runtime_classes = read("src/LocalGPT/Services/CouncilRuntimeClassService.cs")
    campaigns = read("src/LocalGPT/Services/CouncilGameSessionService.Campaign.cs")
    sessions = read("src/LocalGPT/Services/CouncilGameSessionService.cs")
    combat = read("src/LocalGPT/Services/CouncilGameSessionService.Combat.cs")
    rendering = read("src/LocalGPT/Services/CouncilGameSessionService.Rendering.cs")
    snapshots = read("src/LocalGPT/Services/CouncilGameSessionService.Snapshots.cs")
    functions = read("src/LocalGPT/Services/CouncilGameDxAiFunctions.cs")
    teams = read("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs")

    starter_method = runtime_classes.split("CreateAsciiDoomStarterLevels", 1)[-1]
    level_numbers = [int(value) for value in re.findall(r"new\(\) \{ Level = (\d+), Name = \"Corridor", starter_method)]

    checks.extend([
        ("campaign settings are serializable business data", "public sealed class CouncilGameLevelProfile" in models),
        ("start contract supports team-class campaign override", 'public string CampaignRuntimeClassKey { get; set; } = string.Empty;' in models),
        ("starter campaign is visible runtime-class seed", 'BuildDefinition("games.ascii.doom.campaign"' in runtime_classes),
        ("runtime-class seed version advanced", "private const int CurrentSeedVersion = 8;" in runtime_classes),
        ("starter contains exactly ten ordered levels", level_numbers[:10] == list(range(1, 11)) and len(level_numbers) >= 10),
        ("starter difficulty is explicit 1 through 10", all(f"Difficulty = {level}" in starter_method for level in range(1, 11))),
        ("starter map sizes progress", "MapWidth = 28" in starter_method and "MapWidth = 64" in starter_method and "MapHeight = 18" in starter_method and "MapHeight = 32" in starter_method),
        ("starter density is explicit and progressive", "EnemyDensity = .55d" in starter_method and "EnemyDensity = 1.30d" in starter_method),
        ("engine has no second hidden starter-default class", "CouncilGameCampaignDefaults" not in models + sessions + campaigns + combat),
        ("singleton game service resolves scoped configuration through a scope factory", "private readonly IServiceScopeFactory scopeFactory;" in sessions and "scopeFactory.CreateScope()" in campaigns),
        ("team-assigned campaign class is resolved", "ICouncilTeamConfigurationService" in campaigns and "team.Roles" in campaigns and "RuntimeClassKeys" in campaigns),
        ("copied campaign beats system seed when both are assigned", ".OrderBy(assigned => assigned.IsSystemSeed ? 1 : 0)" in campaigns),
        ("malformed campaign fails instead of silently inventing difficulty", "must contain at least one level profile" in campaigns and "invalid levelProfilesJson" in campaigns),
        ("map geometry consumes configured dimensions and room count", "var width = level.MapWidth;" in combat and "var height = level.MapHeight;" in combat and "index < level.RoomCount" in combat),
        ("enemy density and health/damage are configured", "level.RoomCount * level.EnemyDensity" in combat and "level.EnemyHealthMultiplier" in combat and "level.EnemyDamageMultiplier" in combat),
        ("campaign automatically advances configured levels", "session.AutoAdvanceLevels" in combat and "session.CurrentLevelIndex++" in combat and "InitializeDoomWorld(session);" in combat),
        ("HUD exposes authoritative level and difficulty", "LEVEL {levelProfile.Level:00}" in rendering and "DIFF {levelProfile.Difficulty}/10" in rendering),
        ("snapshots expose resolved campaign state", "CampaignRuntimeClassKey = session.CampaignRuntimeClassKey" in snapshots and "CurrentLevelProfile" in snapshots),
        ("DX start schema exposes campaign controls", '"campaignRuntimeClassKey"' in functions and '"startingLevel"' in functions and '"autoAdvanceLevels"' in functions),
        ("Doom Council roles keep human participation optional", teams.count("HumanParticipationMode = HumanParticipationMode.Optional") >= 3),
        ("Doom team assigns campaign runtime class", teams.count('"games.ascii.doom.campaign"') >= 3),
        ("preset explicitly separates Council human role from game controls", 'never interpret an absent Council-role response as "no human game input"' in teams),
        ("preset forbids model-invented difficulty", "Do not invent or reinterpret difficulty" in teams),
    ])

    failed = [name for name, ok in checks if not ok]
    if failed:
        for name in failed:
            print(f"FAIL: {name}")
        print(f"ASCII DOOM campaign audit failed: {len(failed)}/{len(checks)} checks failed.")
        return 1
    print(f"ASCII DOOM campaign audit passed: {len(checks)} checks.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
