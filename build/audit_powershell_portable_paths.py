from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SKIP_PARTS = {'.git', '.vs', 'artifacts', 'bin', 'obj', 'packages', 'node_modules'}
QUOTED_BACKSLASH = re.compile(r'''(["'])(?P<value>[^"'\r\n]*\\[^"'\r\n]*)\1''')
REPOSITORY_RELATIVE_BACKSLASH = re.compile(
    r'''(?ix)^
    (?:
        \.\.?\\
        |
        (?:src|build|docs|wwwroot|controller|controllers|services|components|diagnostics|properties)[/\\]
    )
    .*\\
    '''
)


def is_skipped(path: Path) -> bool:
    relative = path.relative_to(ROOT)
    if any(part in SKIP_PARTS for part in relative.parts):
        return True
    return len(relative.parts) >= 2 and relative.parts[0] == 'docs' and relative.parts[1] == '_site'


def main() -> int:
    failures: list[str] = []
    files = sorted(
        path for path in ROOT.rglob('*')
        if path.is_file() and path.suffix.lower() in {'.ps1', '.psm1'} and not is_skipped(path)
    )

    for path in files:
        relative = path.relative_to(ROOT).as_posix()
        text = path.read_text(encoding='utf-8-sig', errors='strict')
        for line_number, line in enumerate(text.splitlines(), start=1):
            matches = list(QUOTED_BACKSLASH.finditer(line))
            for match in matches:
                value = match.group('value')
                if REPOSITORY_RELATIVE_BACKSLASH.match(value):
                    failures.append(
                        f"{relative}:{line_number}: repository-relative PowerShell path literal uses backslash separators: {value!r}. "
                        "Use '/' in repository-relative literals so Windows PowerShell and pwsh on macOS/Linux resolve the same source path."
                    )

            if 'join-path' not in line.lower():
                continue
            for match in matches:
                value = match.group('value')
                if value.startswith('[') or '(?:' in value:
                    continue
                failures.append(
                    f"{relative}:{line_number}: Join-Path receives a quoted backslash-delimited child path: {value!r}. "
                    "Use '/' or nested Join-Path calls."
                )

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print(f"PowerShell portable-path audit passed for {len(files)} repository scripts.")
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
