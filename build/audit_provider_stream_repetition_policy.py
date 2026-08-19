#!/usr/bin/env python3
"""Static behavioral-contract audit for the LocalGPT provider stream repetition watchdog."""
from __future__ import annotations
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
source = (root / 'src/LocalGPT/Services/ProviderStreamRepetitionWatchdog.cs').read_text(encoding='utf-8')

EXPECTED = {
    'MaximumBufferedCharacters': 12288,
    'MinimumObservedCharacters': 1024,
    'MinimumAnalyzedTokens': 72,
    'MaximumPeriodTokens': 32,
    'MinimumRepeatedCycles': 6,
    'RequiredSuspiciousSamples': 4,
}


def constant(name: str) -> int:
    match = re.search(rf'{re.escape(name)}\s*=\s*([0-9_]+)', source)
    if not match:
        raise AssertionError(f'missing watchdog constant {name}')
    return int(match.group(1).replace('_', ''))


def tokenize(text: str) -> list[str]:
    return re.findall(r'[\w]+', text.lower(), flags=re.UNICODE)


def repeated_cycle(text: str) -> tuple[int, float] | None:
    tokens = tokenize(text)
    minimum_tokens = EXPECTED['MinimumAnalyzedTokens']
    cycles = EXPECTED['MinimumRepeatedCycles']
    if len(tokens) < minimum_tokens:
        return None
    maximum_period = min(EXPECTED['MaximumPeriodTokens'], len(tokens) // cycles)
    for period in range(1, maximum_period + 1):
        analyzed = max(minimum_tokens, period * cycles)
        if len(tokens) < analyzed:
            continue
        tail = tokens[-analyzed:]
        comparisons = analyzed - period
        matches = sum(1 for index in range(period, analyzed) if tail[index] == tail[index - period])
        agreement = matches / comparisons if comparisons else 0.0
        if agreement >= 0.97:
            return period, agreement
    return None


try:
    for name, expected in EXPECTED.items():
        actual = constant(name)
        if actual != expected:
            raise AssertionError(f'{name} changed: {actual} != {expected}')
    if 'MinimumPeriodicAgreement = 0.97d' not in source:
        raise AssertionError('periodic agreement floor is no longer 97%')
    for seconds in ('TimeSpan.FromSeconds(4)', 'TimeSpan.FromSeconds(2)', 'TimeSpan.FromSeconds(6)'):
        if seconds not in source:
            raise AssertionError(f'missing timing contract {seconds}')

    runaway = ('loop is the ' * 400).strip()
    runaway_result = repeated_cycle(runaway)
    if runaway_result is None or runaway_result[0] != 3 or runaway_result[1] < 0.999:
        raise AssertionError(f'known runaway fixture was not detected as a 3-token cycle: {runaway_result}')

    normal = ' '.join(
        f'Task {index} validates provider endpoint identity, structured settings, keyboard focus, and bounded C sharp correctness with evidence {index}.'
        for index in range(1, 50)
    )
    if repeated_cycle(normal) is not None:
        raise AssertionError('non-periodic benchmark-like prose was incorrectly classified as a repeated cycle')

    print('Provider stream repetition policy audit passed: conservative constants, known loop detection, and normal-prose non-detection verified.')
except AssertionError as exc:
    print(f'Provider stream repetition policy audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
