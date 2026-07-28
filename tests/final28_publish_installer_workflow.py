from pathlib import Path
import json
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def text(relative):
    return (ROOT / relative).read_text(encoding="utf-8-sig")


def test_default_installer_is_preservation_first():
    program = text("LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Program.cs")
    parse = program[program.index("public static CliOptions Parse(string[] args)"):]
    block = re.search(r"if \(argsList\.Count == 0\).*?return options;", parse, re.S).group(0)
    for marker in [
        "InstallLocalGptWin = true",
        "StartLocalGpt = true",
        "InstallOllama = true",
        "PullOllamaModels = true",
        "Range = ModelRange.Slim",
        "DesktopShortcuts = true",
        "StartMenuShortcuts = true",
    ]:
        assert marker in block
    assert "ForceDelete" not in block
    assert "ShowHelp" not in block
    installer_readme = text("LocalGPTWebviewWrapper/LocalGPTInstallerConsole/README.md")
    root_readme = text("README.md")
    for documentation in [installer_readme, root_readme]:
        assert "preservation-first" in documentation
        assert "performs no installation" not in documentation
    assert "Slim minimal model set" in installer_readme


def test_launchers_and_visual_studio_profiles_stay_synchronized():
    launcher_root = ROOT / "LocalGPTWebviewWrapper/LocalGPTInstallerConsole"
    expected = {
        "Default.cmd", "Install.cmd", "Update.cmd", "Start.cmd", "Start-NoBrowser.cmd",
        "Install-Ollama.cmd", "Pull-Models-Slim.cmd", "Pull-Models-RTX3060.cmd",
        "Pull-Models-Full.cmd", "Setup-Learning-Base.cmd", "Import-Recommended.cmd", "Uninstall.cmd",
    }
    assert {path.name for path in launcher_root.glob("*.cmd")} == expected
    for name in expected - {"Uninstall.cmd"}:
        assert "--force-delete" not in (launcher_root / name).read_text()
    profiles = json.loads((launcher_root / "Properties/launchSettings.json").read_text())["profiles"]
    assert "LocalGPT Default Install and Update" in profiles
    assert len(profiles) >= len(expected)


def test_publish_profiles_are_self_contained_multi_file_and_typo_free():
    roots = [
        ROOT / "LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles",
        ROOT / "LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Properties/PublishProfiles",
        ROOT / "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/Properties/PublishProfiles",
    ]
    assert not any("maxos" in path.name.lower() for directory in roots for path in directory.glob("*.pubxml"))
    for profile in [path for directory in roots for path in directory.glob("*.pubxml")]:
        group = ET.parse(profile).getroot().find("PropertyGroup")
        values = {child.tag: child.text for child in group}
        assert values["SelfContained"] == "true"
        assert values["PublishSingleFile"] == "false"
        assert values["PublishTrimmed"] == "false"
        assert values["PublishReadyToRun"] == "false"
        assert values["DeleteExistingFiles"] == "true"


def test_release_and_direct_builds_enforce_both_contracts():
    release = text("Build-Release.ps1")
    targets = text("Directory.Build.targets")
    development = text("Build-LocalDevelopment.ps1")
    assert release.count("+ $multiFileSelfContainedProperties") == 3
    assert "PublishSingleFile=true" not in release
    assert "IncludeNativeLibrariesForSelfExtract=true" not in release
    installer_readme = text("LocalGPTWebviewWrapper/LocalGPTInstallerConsole/README.md")
    assert "--self-contained true" in installer_readme
    assert "PublishSingleFile=false" in installer_readme
    assert "--self-contained false" not in installer_readme
    assert "PublishSingleFile=true" not in installer_readme
    for script in ["Assert-PublishConfiguration.ps1", "Assert-InstallerWorkflow.ps1"]:
        assert script in release
        assert script in development
        assert script in targets
