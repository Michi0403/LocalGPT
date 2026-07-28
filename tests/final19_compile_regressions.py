from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"


def test_council_text_service_gets_whitespace_regex_from_its_data_service():
    text = (APP / "Services" / "CouncilTextService.cs").read_text(encoding="utf-8")
    contract = (APP / "Interfaces" / "ICouncilTextPatternDataService.cs").read_text(encoding="utf-8")
    data_service = (APP / "Services" / "Persistence" / "CouncilTextPatternDataService.cs").read_text(encoding="utf-8")
    assert "LocalGptCatalogService._whitespacePattern" not in text
    assert "_whitespacePattern" not in text
    assert text.count("_patterns.WhitespacePattern.Replace") >= 4
    assert "Regex WhitespacePattern { get; }" in contract
    assert 'GetRequired("builtin.whitespace-pattern")' in data_service


def test_program_options_using_is_not_duplicated():
    text = (APP / "Program.cs").read_text(encoding="utf-8")
    assert text.count("using Microsoft.Extensions.Options;") == 1
