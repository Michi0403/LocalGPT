from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REMEDIATION = (
    "Repair: keep transient vendor/browser interaction state client-owned. Initialize complex editors one-way; "
    "observe selection/document/markup changes without causing the event callback's automatic Blazor rerender; "
    "reset transient state when the editor generation changes; and commit durable model state only at an explicit "
    "boundary such as Apply, Save, change, pointer-up, or RangeSelectorValueChangeMode.OnHandleRelease. "
    "If continuous preview is essential, update the preview in browser/vendor-owned state and coalesce or commit "
    "server updates instead of feeding every intermediate value back through InteractiveServer."
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Audit InteractiveServer transient UI-state ownership.")
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--product", choices=("localgpt", "publisherstudio"), required=True)
    return parser.parse_args()


def line_col(text: str, index: int) -> tuple[int, int]:
    line = text.count("\n", 0, index) + 1
    last = text.rfind("\n", 0, index)
    col = index + 1 if last < 0 else index - last
    return line, col


def fail(failures: list[str], path: Path, root: Path, text: str, index: int, message: str) -> None:
    line, col = line_col(text, index)
    failures.append(f"{path.relative_to(root).as_posix()}:{line}:{col}: {message}")


def tag_blocks(text: str, tag: str):
    pattern = re.compile(rf"<{re.escape(tag)}\b[\s\S]*?/?>", re.MULTILINE)
    yield from pattern.finditer(text)


def has_render_suppression(text: str, handler: str) -> bool:
    method = re.search(
        rf"\b(?:private|protected|internal|public)\s+(?:async\s+)?(?:void|Task|ValueTask)\s+{re.escape(handler)}\s*\([^)]*\)\s*\{{(?P<body>[\s\S]{{0,2200}}?)\n\s*\}}",
        text,
    )
    if not method:
        return False
    assignment = re.search(r"(?P<flag>_[A-Za-z0-9_]*suppress[A-Za-z0-9_]*render[A-Za-z0-9_]*)\s*=\s*true\s*;", method.group("body"), re.IGNORECASE)
    if not assignment:
        return False
    flag = assignment.group("flag")
    should_render = re.search(r"protected\s+override\s+bool\s+ShouldRender\s*\(\s*\)\s*\{(?P<body>[\s\S]{0,2200}?)\n\s*\}", text)
    if not should_render:
        return False
    body = should_render.group("body")
    return flag in body and re.search(rf"if\s*\(\s*{re.escape(flag)}\s*\)[\s\S]{{0,420}}return\s+false\s*;", body) is not None


def audit_complex_editors(path: Path, root: Path, text: str, failures: list[str]) -> None:
    support_parts = [text]
    for companion in sorted(path.parent.glob(f"{path.stem}*.cs")):
        support_parts.append(companion.read_text(encoding="utf-8", errors="replace"))
    component_text = "\n".join(support_parts)

    for match in tag_blocks(text, "DxRichEdit"):
        tag = match.group(0)
        for attr in ("DocumentContent", "Selection"):
            bad = re.search(rf"@bind-{attr}\s*=", tag)
            if bad:
                fail(failures, path, root, text, match.start() + bad.start(), f"DxRichEdit must not two-way bind transient {attr} through InteractiveServer. {REMEDIATION}")
        callback = re.search(r"SelectionChanged\s*=\s*\"(?P<handler>[A-Za-z_]\w*)\"", tag)
        if callback and not has_render_suppression(component_text, callback.group("handler")):
            fail(
                failures,
                path,
                root,
                text,
                match.start() + callback.start(),
                f"DxRichEdit SelectionChanged handler '{callback.group('handler')}' must observe selection without allowing the callback's automatic component rerender. {REMEDIATION}",
            )

    for match in tag_blocks(text, "DxHtmlEditor"):
        tag = match.group(0)
        for attr in ("Markup", "Selection"):
            bad = re.search(rf"@bind-{attr}\s*=", tag)
            if bad:
                fail(failures, path, root, text, match.start() + bad.start(), f"DxHtmlEditor must not two-way bind live {attr} through InteractiveServer. {REMEDIATION}")
        callback = re.search(r"MarkupChanged\s*=\s*\"(?P<handler>[A-Za-z_]\w*)\"", tag)
        editable = re.search(r"\bReadOnly\s*=\s*\"true\"", tag, re.IGNORECASE) is None
        if callback and editable and not has_render_suppression(component_text, callback.group("handler")):
            fail(
                failures,
                path,
                root,
                text,
                match.start() + callback.start(),
                f"Editable DxHtmlEditor MarkupChanged handler '{callback.group('handler')}' must update backing state without automatically rerendering the live editor. {REMEDIATION}",
            )

    transient_bind = re.compile(
        r"@bind-(?:SelectedDataItems|FocusedRowIndex|FocusedColumnIndex|Selection|CaretPosition|CursorPosition|ScrollPosition|PlaybackPosition|CurrentPageIndex)\s*=",
        re.IGNORECASE,
    )
    for match in re.finditer(r"<Dx[A-Za-z0-9_]+\b[\s\S]*?/?>", text):
        tag = match.group(0)
        bad = transient_bind.search(tag)
        if bad:
            fail(
                failures,
                path,
                root,
                text,
                match.start() + bad.start(),
                f"Transient DevExpress interaction state is two-way bound through InteractiveServer ({bad.group(0).split('=')[0].strip()}). {REMEDIATION}",
            )


def audit_range_selectors(path: Path, root: Path, text: str, failures: list[str]) -> None:
    for match in tag_blocks(text, "DxRangeSelector"):
        tag = match.group(0)
        live = re.search(r"ValueChangeMode\s*=\s*\"RangeSelectorValueChangeMode\.OnHandleMove\"", tag)
        if live:
            fail(
                failures,
                path,
                root,
                text,
                match.start() + live.start(),
                f"DxRangeSelector may not round-trip every handle move through InteractiveServer. Use OnHandleRelease for durable state, or a browser-owned preview path. {REMEDIATION}",
            )


def native_live_ranges(path: Path, root: Path, text: str) -> list[tuple[int, str]]:
    found: list[tuple[int, str]] = []
    for match in re.finditer(r"<input\b[\s\S]*?>", text, re.IGNORECASE):
        tag = match.group(0)
        if re.search(r"\btype\s*=\s*\"range\"", tag, re.IGNORECASE) and "@oninput" in tag:
            found.append((match.start(), tag))
    return found


def main() -> int:
    args = parse_args()
    root = args.root.resolve()
    source = root / ("src/LocalGPT" if args.product == "localgpt" else "src/PublisherStudio.Web")
    if not source.is_dir():
        print(f"FAIL: source root not found: {source}", file=sys.stderr)
        return 1

    failures: list[str] = []
    live_native_ranges: list[tuple[Path, int, str, str]] = []
    razor_files = [p for p in source.rglob("*.razor") if not {"bin", "obj"}.intersection(p.parts)]
    for path in razor_files:
        text = path.read_text(encoding="utf-8", errors="replace")
        audit_complex_editors(path, root, text, failures)
        audit_range_selectors(path, root, text, failures)
        for index, tag in native_live_ranges(path, root, text):
            if args.product == "localgpt":
                fail(
                    failures,
                    path,
                    root,
                    text,
                    index,
                    "Native range input uses @oninput, continuously feeding drag state through InteractiveServer. LocalGPT's maintained slider contract is commit-on-change. " + REMEDIATION,
                )
            else:
                line, col = line_col(text, index)
                live_native_ranges.append((path, line, col, tag))

    if args.product == "publisherstudio" and live_native_ranges:
        interop_path = source / "wwwroot/js/publisherInterop.js"
        interop = interop_path.read_text(encoding="utf-8", errors="replace") if interop_path.is_file() else ""
        required = (
            "function installStableNativeRangeLifecycle()",
            "target.closest('.dxreRoot, .dx-widget, [class*=\"dxbl-\"], [data-dxbl-loaded]')",
            "event.stopPropagation();",
            "requestAnimationFrame",
            "document.addEventListener('change'",
            "window.addEventListener('pointerup'",
        )
        for token in required:
            if token not in interop:
                failures.append(
                    "src/PublisherStudio.Web/wwwroot/js/publisherInterop.js:1:1: PublisherStudio still has native @oninput range controls, but the reviewed browser-owned/coalesced range lifecycle is incomplete. "
                    f"Missing token: {token!r}. {REMEDIATION}"
                )

    if failures:
        print("InteractiveServer transient UI-state ownership audit failed:", file=sys.stderr)
        for item in failures:
            print(f"FAIL: {item}", file=sys.stderr)
        return 1

    extra = ""
    if args.product == "publisherstudio":
        extra = f"; {len(live_native_ranges)} reviewed native live-range input(s) remain protected by the browser coalescer"
    print(
        f"{args.product} transient UI-state ownership audit passed across {len(razor_files)} Razor component(s): "
        "no complex-editor live document/selection two-way binding, no server-round-tripped DxRangeSelector handle-move state"
        f"{extra}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
