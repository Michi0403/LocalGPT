from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"


def test_council_text_service_owns_its_whitespace_regex():
    text = (APP / "Services" / "CouncilTextService.cs").read_text(encoding="utf-8")
    assert "LocalGptCatalogService._whitespacePattern" not in text
    assert text.count("_whitespacePattern.Replace") >= 4


def test_program_options_using_is_not_duplicated():
    text = (APP / "Program.cs").read_text(encoding="utf-8")
    assert text.count("using Microsoft.Extensions.Options;") == 1
