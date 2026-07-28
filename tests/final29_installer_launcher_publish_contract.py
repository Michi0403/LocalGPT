from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "LocalGPTWebviewWrapper" / "LocalGPTInstallerConsole" / "LocalGPTInstallerConsole.csproj"
LAUNCHERS = {
    "Default.cmd",
    "Install.cmd",
    "Update.cmd",
    "Start.cmd",
    "Start-NoBrowser.cmd",
    "Install-Ollama.cmd",
    "Pull-Models-Slim.cmd",
    "Pull-Models-RTX3060.cmd",
    "Pull-Models-Full.cmd",
    "Setup-Learning-Base.cmd",
    "Import-Recommended.cmd",
    "Uninstall.cmd",
}


def test_all_reviewed_launchers_are_explicitly_and_generically_deployed():
    root = ET.parse(PROJECT).getroot()
    none_items = root.findall(".//None")
    by_update = {item.attrib.get("Update"): item for item in none_items}

    wildcard = by_update.get("*.cmd")
    assert wildcard is not None
    assert wildcard.attrib.get("CopyToOutputDirectory") == "Always"
    assert wildcard.attrib.get("CopyToPublishDirectory") == "Always"

    for launcher in LAUNCHERS:
        item = by_update.get(launcher)
        assert item is not None, f"Missing explicit deployment item for {launcher}"
        assert item.findtext("CopyToOutputDirectory") == "Always"
        assert item.findtext("CopyToPublishDirectory") == "Always"
