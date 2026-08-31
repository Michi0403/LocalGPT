#!/usr/bin/env python3
"""Source-only release audit for LocalGPT 3.3.0 theme-contrast/navigation repair."""
from __future__ import annotations

from pathlib import Path
import math
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
failures: list[str] = []
checks: list[str] = []


def read(relative_path: str) -> str:
    return (ROOT / relative_path).read_text(encoding="utf-8-sig", errors="strict")


def require(relative_path: str, needle: str, label: str) -> None:
    if needle not in read(relative_path):
        failures.append(f"{relative_path} missing {label}: {needle}")
    else:
        checks.append(label)


def forbid(relative_path: str, needle: str, label: str) -> None:
    if needle in read(relative_path):
        failures.append(f"{relative_path} retains forbidden {label}: {needle}")
    else:
        checks.append(label)


def parse_hex(value: str) -> tuple[int, int, int]:
    value = value.strip()
    if not value.startswith("#"):
        raise ValueError(value)
    raw = value[1:]
    if len(raw) == 3:
        raw = "".join(ch * 2 for ch in raw)
    if len(raw) != 6:
        raise ValueError(value)
    return tuple(int(raw[i:i+2], 16) for i in (0, 2, 4))


def luminance(rgb: tuple[int, int, int]) -> float:
    channels: list[float] = []
    for component in rgb:
        value = component / 255.0
        channels.append(value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4)
    return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2]


def contrast(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    left, right = luminance(a), luminance(b)
    return (max(left, right) + 0.05) / (min(left, right) + 0.05)


def mix(a: tuple[int, int, int], b: tuple[int, int, int], weight_a: float) -> tuple[int, int, int]:
    return tuple(round(a[i] * weight_a + b[i] * (1.0 - weight_a)) for i in range(3))


for relative_path in [
    "src/LocalGPT/LocalGPT.csproj",
    "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
]:
    require(relative_path, "<Version>3.3.0</Version>", f"3.3.0 version in {relative_path}")

major, minor, patch = map(int, "3.3.0".split("."))
if minor >= 10 or patch >= 10:
    failures.append("release version violates single-digit minor/patch policy")
else:
    checks.append("single-digit minor/patch release policy")

require("src/LocalGPT/LocalGPT.csproj", "<DevExpressVersion>25.2.9</DevExpressVersion>", "DevExpress 25.2.9 retention")
require("src/LocalGPT/Components/App.razor", "js/localgpt-chat-ui.js?v=3.3.0", "browser cache version marker")
require("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/3.3.0", "outbound product version")

# Main application chrome must not let component-theme icon variables leak into the navigation tree.
require("src/LocalGPT/Components/Layout/MainLayout.razor", "localgpt-shell-toolbar-button", "theme-safe shell toolbar class")
require("src/LocalGPT/Components/Layout/MainLayout.razor", "localgpt-back-home-button", "theme-safe Back to Home class")
require("src/LocalGPT/Components/Layout/MainLayout.razor", "localgpt-navigation-button", "theme-safe drawer button class")
forbid("src/LocalGPT/Components/Layout/MainLayout.razor.css", "::deep .icon {", "generic descendant icon foreground rule")
require("src/LocalGPT/Components/Layout/MainLayout.razor.css", ".localgpt-shell-toolbar-button .icon", "scoped toolbar icon rule")
require("src/LocalGPT/Components/Layout/MainLayout.razor.css", ".localgpt-navigation-button .icon", "scoped navigation button icon rule")
require("src/LocalGPT/Components/Layout/MainLayout.razor.css", ".localgpt-shell-toolbar-button *", "shell button descendant foreground inheritance")
require("src/LocalGPT/Components/Layout/MainLayout.razor.css", 'mask-image: url("images/menu.svg")', "masked menu icon")

# Stable drawer palette and navigation masks.
contract_path = "src/LocalGPT/wwwroot/css/localgpt-theme-contract.css"
for token in [
    "--localgpt-navigation-bg: #151d29;",
    "--localgpt-navigation-color: #f8fafc;",
    "--localgpt-navigation-border:",
    "--localgpt-navigation-hover-bg:",
]:
    require(contract_path, token, f"navigation theme token {token.split(':')[0]}")
require("src/LocalGPT/Components/Layout/Drawer.razor.css", "var(--localgpt-navigation-bg)", "drawer stable navigation background")
require("src/LocalGPT/Components/Layout/NavMenu.razor.css", "background: currentColor !important;", "mask-based navigation icon foreground")
require("src/LocalGPT/Components/Layout/NavMenu.razor.css", 'mask-image: url("images/projects.svg")', "Projects navigation SVG mask")
require("src/LocalGPT/Components/Layout/NavMenu.razor.css", 'mask-image: url("images/project-maintenance.svg")', "Project Maintenance navigation SVG mask")
require("src/LocalGPT/Components/Layout/NavMenu.razor", 'IconCssClass="icon project-maintenance-icon"', "distinct Project Maintenance icon class")

for relative_path in [
    "src/LocalGPT/wwwroot/images/projects.svg",
    "src/LocalGPT/wwwroot/images/project-maintenance.svg",
    "src/LocalGPT/wwwroot/images/menu.svg",
]:
    try:
        ET.parse(ROOT / relative_path)
    except ET.ParseError as exc:
        failures.append(f"{relative_path} is not valid SVG XML: {exc}")
    else:
        checks.append(f"valid SVG XML {relative_path}")

require("src/LocalGPT/LocalGPT.csproj", 'Content Update="wwwroot\\images\\projects.svg"', "Projects SVG static-output registration")
require("src/LocalGPT/LocalGPT.csproj", 'Content Update="wwwroot\\images\\project-maintenance.svg"', "Project Maintenance SVG static-output registration")
require("src/LocalGPT/Components/Pages/Projects.razor", "projects-heading-icon", "Projects page heading icon")

# Chat controls use app-owned semantic classes rather than assuming DevExpress theme luminance.
chat_razor = "src/LocalGPT/Components/Pages/Chat.razor"
for class_name in [
    "localgpt-action-primary",
    "localgpt-action-neutral",
    "localgpt-action-success",
    "localgpt-action-danger",
]:
    require(chat_razor, class_name, f"Chat semantic class {class_name}")
chat_css = "src/LocalGPT/Components/Pages/Chat.razor.css"
require(chat_css, ".localgpt-action-primary", "Chat primary contrast rule")
require(chat_css, ".localgpt-action-neutral", "Chat neutral contrast rule")
require(chat_css, ".localgpt-action-success", "Chat success contrast rule")
require(chat_css, ".localgpt-action-danger", "Chat danger contrast rule")
require(chat_css, ".localgpt-send-button", "runtime send-button contrast rule")
require(chat_css, ".localgpt-upload-button", "runtime upload-button contrast rule")
require(chat_css, ".localgpt-rounded-action *", "Chat button descendant foreground inheritance")
require(chat_css, ".btn-outline-danger", "Chat danger Bootstrap contrast rule")
require(chat_css, "var(--localgpt-primary) 55%, #111827 45%", "AA-oriented primary darkening mix")

# The maintained shell palettes must keep neutral text and darkened-primary action text readable.
contract = read(contract_path)
blocks = re.findall(r'html\[data-localgpt-shell-theme="([^"]+)"\]\s*\{([^}]*)\}', contract, re.DOTALL)
if not blocks:
    failures.append("no maintained shell-theme blocks found for contrast audit")
else:
    dark_anchor = parse_hex("#111827")
    white = (255, 255, 255)
    audited = 0
    minimum_neutral = (math.inf, "")
    minimum_primary = (math.inf, "")
    for theme_name, body in blocks:
        values = dict(re.findall(r'(--[\w-]+)\s*:\s*([^;]+);', body))
        try:
            body_color = parse_hex(values["--bs-body-color"])
            raised = parse_hex(values.get("--bs-secondary-bg", values["--bs-body-bg"]))
            primary = parse_hex(values["--bs-primary"])
        except (KeyError, ValueError):
            continue
        neutral_ratio = contrast(body_color, raised)
        primary_ratio = contrast(white, mix(primary, dark_anchor, 0.55))
        minimum_neutral = min(minimum_neutral, (neutral_ratio, theme_name))
        minimum_primary = min(minimum_primary, (primary_ratio, theme_name))
        if neutral_ratio < 4.5:
            failures.append(f"{theme_name} neutral shell control contrast is {neutral_ratio:.2f}:1 (< 4.5:1)")
        if primary_ratio < 4.5:
            failures.append(f"{theme_name} primary Chat action contrast is {primary_ratio:.2f}:1 (< 4.5:1)")
        audited += 1
    if audited:
        checks.append(f"contrast audit across {audited} maintained shell palettes (neutral min {minimum_neutral[0]:.2f}:1; primary min {minimum_primary[0]:.2f}:1)")
    else:
        failures.append("shell-theme contrast audit could not parse any complete palette")

nav_ratio = contrast(parse_hex("#f8fafc"), parse_hex("#151d29"))
if nav_ratio < 7.0:
    failures.append(f"navigation foreground/background contrast is only {nav_ratio:.2f}:1")
else:
    checks.append(f"navigation foreground/background contrast {nav_ratio:.2f}:1")

# Preserve render and feature boundaries touched by the surrounding files.
require("src/LocalGPT/Components/Layout/NavMenu.razor", "@rendermode InteractiveServer", "NavMenu InteractiveServer boundary")
require("src/LocalGPT/Components/Pages/Chat.razor", "@rendermode InteractiveServer", "Chat InteractiveServer boundary")
require("src/LocalGPT/Components/Pages/Projects.razor", "@rendermode InteractiveServer", "Projects InteractiveServer boundary")
require("CHANGELOG-v3.3.0-THEME-CONTRAST-NAVIGATION-ICON-REPAIR.md", "PublisherStudio remains 2.9.7", "unchanged PublisherStudio disclosure")
require("RELEASE.md", "PublisherStudio remains **2.9.7**", "release PublisherStudio disclosure")
require("VALIDATION-v3.3.0-source.md", "source-only and not compiled", "source-only validation disclosure")
require("CHANGELOG-v3.3.0-THEME-CONTRAST-NAVIGATION-ICON-REPAIR.md", "3.2.10", "version-rollover explanation")

if failures:
    print("LocalGPT 3.3.0 source release audit failed:")
    for failure in failures:
        print("  -", failure)
    raise SystemExit(1)

print(f"LocalGPT 3.3.0 source release audit passed: {len(checks)} checks.")
for check in checks:
    print("  +", check)
