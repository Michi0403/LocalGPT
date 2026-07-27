from pathlib import Path
import json
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]

class LocalGptBuildWireLoggingContracts(unittest.TestCase):
    def test_wire_project_is_rid_neutral_and_solution_maps_it_to_any_cpu(self):
        wire = ROOT / "LocalGPTWebviewWrapper/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj"
        tree = ET.parse(wire)
        text = wire.read_text(encoding="utf-8")
        self.assertIn("<Platforms>AnyCPU</Platforms>", text)
        self.assertIn("<PlatformTarget>AnyCPU</PlatformTarget>", text)
        self.assertIn("<AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>", text)
        solution = (ROOT / "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln").read_text(encoding="utf-8")
        self.assertIn("Debug|x64.ActiveCfg = Debug|Any CPU", solution)
        self.assertIn("Release|arm64.ActiveCfg = Release|Any CPU", solution)

    def test_application_supports_project_and_package_modes(self):
        project = (ROOT / "LocalGPTWebviewWrapper/LocalGPT/LocalGPT.csproj").read_text(encoding="utf-8")
        self.assertIn("Condition=\"'$(UseLocalWireProtocolProject)' == 'true'\"", project)
        self.assertIn("PackageReference Include=\"LocalGPT.WireProtocolVersion\"", project)
        self.assertNotIn("SetPlatform=\"AnyCPU\"", project)
        self.assertNotIn("AdditionalProperties=\"Platform=AnyCPU\"", project)
        self.assertIn("GlobalPropertiesToRemove=\"Platform;PlatformTarget;RuntimeIdentifier", project)
        self.assertNotIn("MSBuild Projects=\"..\\LocalGPT.WireProtocolVersion", project)

    def test_release_matrix_restores_each_rid_without_publishing_the_solution(self):
        script = (ROOT / "Build-Release.ps1").read_text(encoding="utf-8")
        for rid in ("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"):
            self.assertIn(f'"{rid}"', script)
        self.assertIn('"restore", $appProject, "-r", $Rid', script)
        self.assertIn('"publish", $appProject', script)
        self.assertIn('UseLocalWireProtocolProject=false', script)
        self.assertIn('--disable-parallel', script)
        self.assertIn('Updated shared LocalGPT protocol package cache', script)
        self.assertIn('IncludeWireProtocolPackageInPublish=true', script)
        self.assertNotIn('publish", $solution', script)
        local_build = (ROOT / "Build-LocalDevelopment.ps1").read_text(encoding="utf-8")
        self.assertIn('Restoring the authoritative RID-neutral protocol project first', local_build)
        self.assertIn('BuildProjectReferences=false', local_build)
        self.assertIn('--disable-parallel', local_build)
        self.assertLess(local_build.index('restore", $wireProject'), local_build.index('restore", $appProject'))

    def test_optional_wiring_and_package_location_are_documented(self):
        readme = (ROOT / "README.md").read_text(encoding="utf-8")
        self.assertIn("Optional organic 1-Wire integration and protocol package", readme)
        self.assertIn("packages\\LocalGPT.WireProtocolVersion.2.1.0.nupkg", readme)
        self.assertIn("organic adaptation model", readme)
        self.assertIn("Build-Release.ps1 -Runtime all", readme)

    def test_logging_guard_is_monotonic(self):
        baseline = json.loads((ROOT / "build/logging-baseline.json").read_text(encoding="utf-8"))
        self.assertGreater(len(baseline["files"]), 50)
        guard = (ROOT / "build/Assert-LoggingIntegrity.ps1").read_text(encoding="utf-8")
        self.assertIn("Logging regression", guard)
        self.assertIn("ALLOW_LOGGING_BASELINE_REFRESH", guard)
        self.assertIn("catchBlocks", guard)
        self.assertIn("Windows PowerShell 5.1", guard)
        self.assertNotIn("[System.IO.Path]::GetRelativePath", guard)
        self.assertIn(".Replace('\\', '/')", guard)
        self.assertTrue((ROOT / ".github/workflows/logging-integrity.yml").is_file())
        targets = (ROOT / "Directory.Build.targets").read_text(encoding="utf-8")
        self.assertIn("AssertLocalGptLoggingIntegrity", targets)
        self.assertIn("SkipLoggingIntegrityGuard", targets)
        self.assertIn("ConsoleToMSBuild=\"true\"", targets)
        self.assertNotIn("-RepositoryRoot", targets)
        self.assertIn("WorkingDirectory=\"$(MSBuildThisFileDirectory)\"", targets)
        self.assertIn("Split-Path -Parent $PSScriptRoot", guard)
        self.assertIn("Assert-LoggingIntegrity.ps1", (ROOT / "Build-Release.ps1").read_text(encoding="utf-8"))
        self.assertIn("Assert-LoggingIntegrity.ps1", (ROOT / "Build-LocalDevelopment.ps1").read_text(encoding="utf-8"))
        policy = (ROOT / "docs/LOGGING_INTEGRITY.md").read_text(encoding="utf-8")
        self.assertIn("Logging removal is not cleanup", policy)

if __name__ == "__main__":
    unittest.main()
