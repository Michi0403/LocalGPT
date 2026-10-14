#!/usr/bin/env python3
"""Static integration gate for LocalGPT 4.2.5 ASCII chat, DXFunction, seed, game and controller wiring."""
from __future__ import annotations
import argparse
from pathlib import Path


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    root = parser.parse_args().root.resolve()
    failures: list[str] = []

    def read(relative: str) -> str:
        path = root / relative
        if not path.is_file():
            failures.append(f"missing file: {relative}")
            return ""
        return path.read_text(encoding="utf-8")

    chat = read("src/LocalGPT/Components/Pages/Chat.razor")
    chat_code = read("src/LocalGPT/Components/Pages/Chat.razor.cs")
    console = read("src/LocalGPT/Components/Shared/ChatGameConsole.razor")
    ascii_text = read("src/LocalGPT/Services/AsciiChatTextService.cs")
    js = read("src/LocalGPT/wwwroot/js/localgpt-game-console.js")
    state = read("src/LocalGPT/Services/ChatAsciiExperienceState.cs")
    ascii_dx = read("src/LocalGPT/Services/ChatAsciiDxAiFunctions.cs")
    game_dx = read("src/LocalGPT/Services/CouncilGameDxAiFunctions.cs")
    seeds = read("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs")
    development_seeds = read("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.DevelopmentTemplates.cs")
    registration = read("src/LocalGPT/Program.ServiceRegistration.cs")
    catalog = read("src/LocalGPT/Services/DxAiFunctionCatalogService.QueriesAndDiscovery.cs")

    required_chat = (
        'ASCII fun mode (contextual)',
        'ConversationMessages="@AsciiConversationMessages"',
        'CouncilParticipantActivities="@AsciiCouncilParticipantActivities"',
    )
    for token in required_chat:
        if token not in chat:
            failures.append(f"chat presentation missing {token!r}")

    for token in (
        "AsciiExperience.Update(ActiveConversationId, showGameConsole, asciiFunModeEnabled)",
        "CouncilLiveSessions.GetParticipantActivitiesForDisplay(activeRunId)",
        "Task.Delay(700, cancellationToken)",
    ):
        if token not in chat_code:
            failures.append(f"canonical replay/state wiring missing {token!r}")

    for token in (
        "BuildConversationTranscript",
        "BuildCouncilParticipantBody",
        "[THINK]",
        "[CALL ",
        "[RESULT ",
        "[SAY]",
        ".Take(12)",
        "Math.Clamp(frames, 0, 12)",
        "ResolveRoleNickname(ChatMessageRole role",
    ):
        if token not in ascii_text:
            failures.append(f"ASCII transcript/sequence text-service contract missing {token!r}")

    for token in (
        "@inject AsciiChatTextService AsciiText",
        "AsciiText.BuildConversationTranscript(ConversationMessages, CouncilParticipantActivities, AssistantDisplayName)",
        "AsciiText.ExtractSequenceFrames(ConversationMessages)",
        "AsciiText.BuildSequenceSignature(asciiSequenceFrames)",
    ):
        if token not in console:
            failures.append(f"ASCII component service ownership missing {token!r}")
    if "AsciiText.BuildParticipantSignature(participantActivities)" not in chat_code:
        failures.append("Chat Council mirror signature is not service-owned")
    if "AddSingleton<AsciiChatTextService>()" not in registration:
        failures.append("ASCII text service is not registered in DI")

    if ".slice(0, 12)" not in js:
        failures.append("browser sequence player is not bounded to 12 frames")
    if "requestAnimationFrame(() => renderSequenceFrame" in js:
        failures.append("ASCII sequence playback uses a display-frame loop")
    for token in ("scheduleGamepadFrame", "cancelGamepadFrame", "gamepadconnected", "gamepaddisconnected", "document.hidden", "windowFocused"):
        if token not in js:
            failures.append(f"controller lifecycle wiring missing {token!r}")

    for token in (
        "terminal={(open ? \"OPEN\" : \"CLOSED\")}",
        "contextual ASCII fun={(fun ? \"ENABLED\" : \"DISABLED\")}",
        "```ascii-sequence",
        "--- frame ---",
        "2-12 frames",
        "existing Council game/runtime functions",
    ):
        if token not in state:
            failures.append(f"provider ASCII guidance missing {token!r}")

    for token in (
        '"localgpt.ascii.surface.get"',
        "IChatAsciiExperienceState asciiExperience",
        "SupportsAutomaticInvocation: true",
        "supportsAsciiArt = true",
        "supportsAsciiSequence = true",
        "supportsCouncilGameRuntime = true",
        "maximumSequenceFrames = 12",
        "recommendedMaximumColumns = 80",
        "recommendedMaximumRowsPerFrame = 28",
    ):
        if token not in ascii_dx:
            failures.append(f"ASCII DXFunction capability query missing {token!r}")

    if "typeof(IDxAiFunctionHandler).IsAssignableFrom(type.AsType())" not in registration:
        failures.append("DXFunction handlers are not discovered through the normal DI reflection path")
    if "registry.GetFunctions().Select(CreateDxEntry)" not in catalog or "IsSystemSeed = !string.Equals(function.Source, \"UserDxFunction\"" not in catalog:
        failures.append("DXFunction catalog does not auto-synchronize DI functions as system seed entries")

    game_names = (
        "localgpt.game.session.start",
        "localgpt.game.session.get",
        "localgpt.game.control.preview",
        "localgpt.game.control",
        "localgpt.game.frame.submit",
        "localgpt.game.control-mode.set",
        "localgpt.game.input-gate.set",
    )
    for name in game_names:
        if name not in game_dx:
            failures.append(f"existing ASCII game DXFunction regressed: {name}")

    if seeds.count('\"localgpt.ascii.surface.get\"') < 2 or '"localgpt.ascii.surface.get"' not in development_seeds:
        failures.append("shipped ASCII/game Council blueprints do not seed the ASCII surface capability")
    for team in ("ascii-doom-council-adventure", "green-dragon-runtime-story"):
        if team not in seeds:
            failures.append(f"shipped ASCII-capable Council seed missing {team}")

    if failures:
        print("LocalGPT 4.2.5 ASCII experience audit FAILED:")
        for failure in failures:
            print(f" - {failure}")
        return 1
    print("LocalGPT 4.2.5 ASCII experience audit passed: canonical replay, Council lanes, DXFunction discovery/system seeding, shipped game capabilities, bounded animations, and demand-driven controller support are intact.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
