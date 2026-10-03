from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
failures: list[str] = []


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        failures.append(f"missing {rel}")
        return ""
    return path.read_text(encoding="utf-8-sig")


helper = read("build/RepositoryBuildStorage.Common.ps1")
if "Join-Path $repository 'artifacts/.build-storage'" not in helper:
    failures.append("repository build storage lacks zero-configuration <repository>/artifacts/.build-storage default")

gitignore = read(".gitignore")
if "artifacts/" not in gitignore and "artifacts/.build-storage/" not in gitignore:
    failures.append(".gitignore does not ignore repository-local artifacts/.build-storage")

compat = read("build/Assert-PowerShellCompatibility.ps1")
if 'IndexOf("Join-Path $documentationToolCacheRoot' in compat:
    failures.append("PowerShell compatibility guard interpolates $documentationToolCacheRoot in a double-quoted source-search literal; StrictMode would treat it as an unset runtime variable")
for token in (
    "FUTURE2_BUILD_STORAGE_ROOT",
    "FUTURE2_DOCUMENTATION_CACHE_ROOT",
    "DOTNET_CLI_HOME",
    "NUGET_PACKAGES",
    "NUGET_HTTP_CACHE_PATH",
    "NUGET_PLUGINS_CACHE_PATH",
    "NUGET_SCRATCH",
    "NPM_CONFIG_CACHE",
    "XDG_CACHE_HOME",
    "TMPDIR",
    "$env:TMP =",
    "$env:TEMP =",
):
    if token not in helper:
        failures.append(f"repository build storage no longer redirects {token}")

release = read("Build-Release.ps1")
init = release.find("Initialize-Future2RepositoryBuildStorage")
first_work = release.find("if ($CompileOnly)")
if init < 0 or (first_work >= 0 and init > first_work):
    failures.append("Build-Release.ps1 does not initialize repository build storage before build work")

docs = read("build/Build-Documentation.ps1")
init = docs.find("Initialize-Future2RepositoryBuildStorage")
first_temp = docs.find("GetTempPath()")
if init < 0:
    failures.append("Build-Documentation.ps1 does not initialize repository build storage")
elif first_temp >= 0 and init > first_temp:
    failures.append("Build-Documentation.ps1 accesses OS temp before repository storage initialization")
if "Join-Path $documentationToolCacheRoot 'browser-profiles'" not in docs:
    failures.append("Build-Documentation.ps1 does not keep browser profiles inside the documentation cache root")
if "DocumentationBrowserProfiles" in docs:
    failures.append("Build-Documentation.ps1 still contains the legacy OS-temp browser-profile root")

node = read("build/NodeRuntime.Common.ps1")
fallback = node.find("if (-not [string]::IsNullOrWhiteSpace($FallbackRoot))")
local_data = node.find("$localApplicationData = [Environment]::GetFolderPath")
if fallback < 0 or local_data < 0 or fallback > local_data:
    failures.append("NodeRuntime.Common.ps1 does not prefer the repository fallback before LocalApplicationData")

agents = read("AGENTS.md")
if "## Repository-local release-build storage" not in agents:
    failures.append("repository-local release-build storage architecture rule is missing from AGENTS.md")

if failures:
    for failure in failures:
        print(f"FAIL: {failure}", file=sys.stderr)
    raise SystemExit(1)

print("Repository-local release-build storage audit passed: release/documentation entrypoints redirect heavy caches and temp state to repository-owned storage, and per-user caches remain fallback-only.")
