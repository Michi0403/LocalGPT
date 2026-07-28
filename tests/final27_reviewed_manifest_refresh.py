import hashlib
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]


def normalized_hash(relative: str) -> str:
    text = (ROOT / relative).read_text(encoding="utf-8-sig").replace("\r\n", "\n").replace("\r", "\n")
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


class Final27ReviewedManifestRefresh(unittest.TestCase):
    def test_optional_provider_timeouts_are_expected_discovery_outcomes(self):
        source = (ROOT / "LocalGPTWebviewWrapper/LocalGPT/Services/AiDiscoveryService.cs").read_text(encoding="utf-8")
        self.assertGreaterEqual(source.count("catch (OperationCanceledException) when (ct.IsCancellationRequested)"), 3)
        self.assertIn("Optional local AI provider {{Provider}} at host {{EndpointHost}} did not answer", source)
        self.assertIn("Optional local AI provider {{Provider}} at host {{EndpointHost}} is not currently reachable", source)
        self.assertNotIn("Error in ProbeOpenAICompatibleAsync", source)
        self.assertIn("Unexpected error while probing provider {{Provider}} at endpoint {{Endpoint}}", source)

    def test_reviewed_manifest_refresher_preserves_security_boundary(self):
        script = (ROOT / "build/Update-ReviewedProtectionManifest.ps1").read_text(encoding="utf-8")
        for token in ("SupportsShouldProcess", "ConfirmImpact", "ReviewCurrentChanges", "ReviewedFiles", "Assert-SecurityRulePreservation.ps1", "security-rules-final19.sha256", "Security or 1-Wire preservation file cannot be refreshed", "WriteAllBytes"):
            self.assertIn(token, script)
        self.assertIn("Invoke-RequiredSafeguard 'build/Assert-JavaScriptDiagnostics.ps1'", script)
        self.assertIn("Invoke-RequiredSafeguard 'build/Assert-ProtectedRepositoryFiles.ps1'", script)

    def test_refresher_documentation_and_test_are_protected(self):
        guard = (ROOT / "build/Assert-ProtectedRepositoryFiles.ps1").read_text(encoding="utf-8")
        manifest = (ROOT / "build/protected-files.sha256").read_text(encoding="utf-8")
        for relative in ("build/Update-ReviewedProtectionManifest.ps1", "docs/REVIEWED_MANIFEST_REFRESH.md", "tests/final27_reviewed_manifest_refresh.py"):
            self.assertIn(f"'{relative}'", guard)
            self.assertIn(f"{normalized_hash(relative)}  {relative}", manifest)


if __name__ == "__main__":
    unittest.main()
