#!/usr/bin/env bash
set -euo pipefail

product=''
source_root=''
output_root=''
docs_root=''
version=''
configuration='Release'
runtime='all'
release_packaging_package=''
use_container_packaging=0
require_optional_native_packages=0
keep_build_tree=0

while (($#)); do
  case "$1" in
    --product) product="$2"; shift 2 ;;
    --source) source_root="$2"; shift 2 ;;
    --output) output_root="$2"; shift 2 ;;
    --docs) docs_root="$2"; shift 2 ;;
    --version) version="$2"; shift 2 ;;
    --configuration) configuration="$2"; shift 2 ;;
    --runtime) runtime="$2"; shift 2 ;;
    --release-packaging-package) release_packaging_package="$2"; shift 2 ;;
    --use-container-packaging) use_container_packaging=1; shift ;;
    --require-optional-native-packages) require_optional_native_packages=1; shift ;;
    --keep-build-tree) keep_build_tree=1; shift ;;
    *) printf 'Unknown WSL release helper argument: %s\n' "$1" >&2; exit 2 ;;
  esac
done

for value_name in product source_root output_root docs_root version; do
  if [[ -z "${!value_name}" ]]; then
    printf 'Missing required WSL release helper value: %s\n' "$value_name" >&2
    exit 2
  fi
done

if [[ "$(uname -s)" != 'Linux' ]]; then
  printf 'The WSL release child must run under Linux.\n' >&2
  exit 3
fi
if ! command -v pwsh >/dev/null 2>&1; then
  printf 'PowerShell (pwsh) is missing inside WSL. Run Setup-WslLinuxBuild.ps1 -Provision from Windows.\n' >&2
  exit 4
fi
if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks 2>/dev/null | grep -q '^10[.]'; then
  printf '.NET 10 SDK is missing inside WSL. Run Setup-WslLinuxBuild.ps1 -Provision from Windows.\n' >&2
  exit 5
fi
if ! command -v python3 >/dev/null 2>&1; then
  printf 'Python 3 is missing inside WSL. Run Setup-WslLinuxBuild.ps1 -Provision from Windows.\n' >&2
  exit 6
fi

case "$product" in
  LocalGPT) product_slug='localgpt' ;;
  PublisherStudio) product_slug='publisherstudio' ;;
  *) printf 'Unsupported product: %s\n' "$product" >&2; exit 2 ;;
esac

cache_parent="${XDG_CACHE_HOME:-$HOME/.cache}/$product_slug"
mkdir -p "$cache_parent"
build_root="$(mktemp -d "$cache_parent/wsl-release-XXXXXXXX")"
repo="$build_root/repository"
docs="$build_root/documentation"
mkdir -p "$repo" "$docs" "$output_root"

cleanup() {
  if [[ "$keep_build_tree" -eq 1 ]]; then
    printf 'WSL build tree retained at %s\n' "$build_root"
  else
    rm -rf "$build_root"
  fi
}
trap cleanup EXIT

printf 'Mirroring %s source into the WSL Linux filesystem for native Linux build I/O...\n' "$product"
(
  cd "$source_root"
  tar \
    --exclude='./.git' \
    --exclude='./artifacts' \
    --exclude='./**/bin' \
    --exclude='./**/bin/**' \
    --exclude='./**/obj' \
    --exclude='./**/obj/**' \
    --exclude='./**/node_modules' \
    --exclude='./**/node_modules/**' \
    -cf - .
) | (cd "$repo" && tar -xf -)

cp -a "$docs_root/." "$docs/"
if [[ -n "$release_packaging_package" ]]; then
  mkdir -p "$repo/packages"
  cp -f "$release_packaging_package" "$repo/packages/"
fi

export PATH="$HOME/.local/bin:$PATH"
# WSL commonly has no FUSE device. AppImage runtimes support extract-and-run, which lets
# appimagetool execute without requiring a mounted AppImage filesystem.
export APPIMAGE_EXTRACT_AND_RUN=1

build_args=(
  -NoLogo -NoProfile -File "$repo/Build-Release.ps1"
  -Runtime "$runtime"
  -Configuration "$configuration"
  -WslChildBuild
  -SkipReleaseBundle
  -PreparedDocumentationRoot "$docs"
)
if [[ "$product" == 'PublisherStudio' ]]; then
  build_args+=( -UsePreparedClientAssets )
fi
if [[ "$use_container_packaging" -eq 1 ]]; then
  build_args+=( -UseContainerPackaging )
fi
if [[ "$require_optional_native_packages" -eq 1 ]]; then
  build_args+=( -RequireOptionalNativePackages )
fi

printf 'Running %s Linux release inside WSL from %s...\n' "$product" "$repo"
pwsh "${build_args[@]}"

artifact_root="$repo/artifacts/release"
if [[ ! -d "$artifact_root" ]]; then
  printf 'WSL child build created no release artifact directory: %s\n' "$artifact_root" >&2
  exit 7
fi

copied=0
while IFS= read -r -d '' artifact; do
  cp -f "$artifact" "$output_root/"
  printf 'WSL artifact: %s\n' "$(basename "$artifact")"
  copied=$((copied + 1))
done < <(find "$artifact_root" -maxdepth 1 -type f -name "$product-$version-linux-*" -print0 | sort -z)

if [[ "$copied" -eq 0 ]]; then
  printf 'WSL child build returned no Linux artifacts for %s %s.\n' "$product" "$version" >&2
  exit 8
fi
printf 'WSL Linux release child returned %d artifact(s).\n' "$copied"
