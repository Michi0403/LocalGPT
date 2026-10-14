"""Source-only guard for the LocalGPT 5.6.9 release-packaging PKCS servicing floor.

Does not restore, build, or attempt to resolve packages over the network.
"""
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PACKAGING = ROOT / "src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj"


def require(ok: bool, message: str) -> None:
    if not ok:
        raise SystemExit(f"Release packaging security contract failed: {message}")


packaging = ET.parse(PACKAGING).getroot()
props = packaging.find("PropertyGroup")
require(props is not None, "project has no properties")
require(props.findtext("TargetFramework") == "net10.0", "packaging target framework drifted")
require(props.findtext("Version") == "1.0.5", "packaging source version drifted")
refs = {r.attrib.get("Include"): r.attrib.get("Version")
        for group in packaging.findall("ItemGroup") for r in group.findall("PackageReference")}
require(refs.get("PDFsharp") == "6.2.4", "PDFsharp dependency unexpectedly changed")
require(refs.get("System.Security.Cryptography.Pkcs") == "10.0.13",
        "PDFsharp's transitive System.Security.Cryptography.Pkcs minimum is not overridden to secure 10.0.13")

release = (ROOT / "Build-Release.ps1").read_text(encoding="utf-8")
require("$releasePackagingVersion = '1.0.5'" in release,
        "release workflow may reuse the stale 1.0.4 package")
for rel in ("build/Ensure-ReleasePackagingPackage.ps1", "build/Publish-ReleasePackagingPackage.ps1"):
    script = (ROOT / rel).read_text(encoding="utf-8")
    require('[string]$Version = "1.0.5"' in script, rel + " defaults to a stale package")
    require("$declaredPackageVersion" in script and "[string]::Equals($Version, $declaredPackageVersion" in script,
            rel + " no longer verifies the requested helper version")
require("pdf-merge-input-list" in (ROOT / "build/Ensure-ReleasePackagingPackage.ps1").read_text(encoding="utf-8"),
        "release helper capability validation missing")
root_props = ET.parse(ROOT / "Directory.Build.props").getroot()
raw_props = (ROOT / "Directory.Build.props").read_text(encoding="utf-8")
require("<NuGetAudit>true</NuGetAudit>" in raw_props and "NU1903;NU1904" in raw_props,
        "build-blocking NuGet audit disabled")
application_version = ET.parse(ROOT / "src/LocalGPT/LocalGPT.csproj").getroot().findtext(".//Version", default="unknown")
print(f"LocalGPT {application_version} release packaging dependency, helper cache version, capability and NuGet audit guards passed.")
