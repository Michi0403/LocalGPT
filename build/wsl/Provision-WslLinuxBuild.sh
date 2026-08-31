#!/usr/bin/env bash
set -euo pipefail

install_appimagetool=1
while (($#)); do
  case "$1" in
    --skip-appimagetool) install_appimagetool=0; shift ;;
    *) printf 'Unknown WSL provisioning argument: %s\n' "$1" >&2; exit 2 ;;
  esac
done

if [[ "$(uname -s)" != 'Linux' ]]; then
  printf 'This provisioning helper must run inside a Linux WSL distribution.\n' >&2
  exit 2
fi
if [[ ! -r /etc/os-release ]]; then
  printf 'Cannot identify the WSL Linux distribution because /etc/os-release is missing.\n' >&2
  exit 2
fi
# shellcheck disable=SC1091
. /etc/os-release
case "${ID:-}" in
  ubuntu|debian) ;;
  *)
    printf 'Automatic provisioning currently supports Ubuntu/Debian WSL. Detected ID=%s. Install pwsh, .NET 10 SDK, python3, rpm/rpmbuild and appimagetool manually, then rerun the status helper.\n' "${ID:-unknown}" >&2
    exit 3
    ;;
esac
if ! command -v sudo >/dev/null 2>&1; then
  printf 'sudo is required for one-time WSL build-host provisioning.\n' >&2
  exit 4
fi

printf 'Installing Linux release prerequisites in %s %s...\n' "${PRETTY_NAME:-$ID}" "${VERSION_ID:-}"
sudo apt-get update
sudo DEBIAN_FRONTEND=noninteractive apt-get install -y \
  ca-certificates curl wget apt-transport-https software-properties-common \
  python3 rpm file tar gzip xz-utils coreutils findutils

if ! command -v pwsh >/dev/null 2>&1; then
  tmp_dir="$(mktemp -d)"
  trap 'rm -rf "$tmp_dir"' EXIT
  case "$ID" in
    ubuntu) repo_url="https://packages.microsoft.com/config/ubuntu/${VERSION_ID}/packages-microsoft-prod.deb" ;;
    debian) repo_url="https://packages.microsoft.com/config/debian/${VERSION_ID}/packages-microsoft-prod.deb" ;;
  esac
  printf 'Registering the Microsoft package repository for PowerShell...\n'
  curl -fL "$repo_url" -o "$tmp_dir/packages-microsoft-prod.deb"
  sudo dpkg -i "$tmp_dir/packages-microsoft-prod.deb"
  sudo apt-get update
  sudo DEBIAN_FRONTEND=noninteractive apt-get install -y powershell
fi

if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks 2>/dev/null | grep -q '^10[.]'; then
  printf 'Installing .NET 10 SDK...\n'
  if ! sudo DEBIAN_FRONTEND=noninteractive apt-get install -y dotnet-sdk-10.0; then
    printf 'dotnet-sdk-10.0 was not available from the configured APT feeds. Follow Microsoft .NET 10 instructions for this distro/architecture, then rerun Setup-WslLinuxBuild.ps1.\n' >&2
    exit 5
  fi
fi

if [[ "$install_appimagetool" -eq 1 ]] && ! command -v appimagetool >/dev/null 2>&1; then
  mkdir -p "$HOME/.local/bin"
  case "$(uname -m)" in
    x86_64|amd64) appimage_arch='x86_64' ;;
    aarch64|arm64) appimage_arch='aarch64' ;;
    *) appimage_arch='' ;;
  esac
  if [[ -n "$appimage_arch" ]]; then
    printf 'Installing official appimagetool for host architecture %s into ~/.local/bin...\n' "$appimage_arch"
    curl -fL "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-${appimage_arch}.AppImage" -o "$HOME/.local/bin/appimagetool"
    chmod 0755 "$HOME/.local/bin/appimagetool"
  else
    printf 'No prebuilt appimagetool mapping is maintained for architecture %s; AppImage remains optional.\n' "$(uname -m)" >&2
  fi
fi

printf '\nWSL Linux release tool status:\n'
printf '  PowerShell:  '; command -v pwsh || true
printf '  .NET SDKs:\n'; dotnet --list-sdks 2>/dev/null | sed 's/^/    /' || true
printf '  Python:      '; command -v python3 || true
printf '  rpmbuild:    '; command -v rpmbuild || true
printf '  appimagetool:'; command -v appimagetool || printf ' optional tool not found\n'
printf '\nProvisioning complete. The Windows release helper can now use this distro headlessly.\n'
