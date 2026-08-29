#!/usr/bin/env python3
"""Static audit for the database-backed, opt-in provider stream repetition watchdog."""
from __future__ import annotations
from pathlib import Path
import re, sys
root=Path(__file__).resolve().parents[1]
watch=(root/'src/LocalGPT/Services/ProviderStreamRepetitionWatchdog.cs').read_text(encoding='utf-8')
models=(root/'src/LocalGPT/BusinessObjects/LocalGptRuntimePolicyModels.cs').read_text(encoding='utf-8')
seed=(root/'src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs').read_text(encoding='utf-8')
catalog=(root/'src/LocalGPT/Services/LocalGptCatalogService.RuntimePatterns.cs').read_text(encoding='utf-8')
keys=[
'ProviderStreamRepetitionWatchdogEnabled','ProviderStreamRepetitionMaximumBufferedCharacters','ProviderStreamRepetitionMinimumObservedCharacters',
'ProviderStreamRepetitionMinimumAnalyzedTokens','ProviderStreamRepetitionMaximumPeriodTokens','ProviderStreamRepetitionShortPeriodMaximumTokens',
'ProviderStreamRepetitionMinimumRepeatedCycles','ProviderStreamRepetitionMinimumLongPeriodRepeatedCycles',
'ProviderStreamRepetitionMinimumPeriodicAgreementBasisPoints','ProviderStreamRepetitionMinimumLongPeriodAgreementBasisPoints',
'ProviderStreamRepetitionRequiredSuspiciousSamples','ProviderStreamRepetitionInitialObservationMilliseconds',
'ProviderStreamRepetitionSampleIntervalMilliseconds','ProviderStreamRepetitionMinimumSuspiciousDurationMilliseconds']
try:
    for key in keys:
        if key not in models: raise AssertionError(f'missing runtime policy enum {key}')
        if key not in seed: raise AssertionError(f'missing persisted seed {key}')
        if key not in catalog: raise AssertionError(f'missing catalog access {key}')
    if not re.search(r'ProviderStreamRepetitionWatchdogEnabled[^\n]+"0"',seed):
        raise AssertionError('repetition watchdog must ship disabled (opt-in)')
    if re.search(r'const\s+(?:int|double|TimeSpan)\s+(?:MaximumBufferedCharacters|MinimumObservedCharacters|MinimumAnalyzedTokens|MaximumPeriodTokens|RequiredSuspiciousSamples)',watch):
        raise AssertionError('watchdog still owns developer hard-coded resource/threshold constants')
    if 'if (!enabled)' not in watch: raise AssertionError('disabled watchdog fast path is missing')
    if 'catalog.ProviderStreamRepetition' not in watch: raise AssertionError('watchdog does not consume central runtime policy')
    # Preserve previous behavior only as configurable seed values, never as source-owned ceilings.
    expected=['32768','1024','72','512','32','6','4','9700','9850','4','4000','2000','6000']
    for value in expected:
        if f'"{value}"' not in seed: raise AssertionError(f'expected configurable compatibility seed {value} missing')
    print('Provider stream repetition policy audit passed: watchdog is opt-in, thresholds are persisted runtime policy, and prior detection behavior survives only as editable seed data.')
except AssertionError as exc:
    print(f'Provider stream repetition policy audit failed: {exc}',file=sys.stderr); raise SystemExit(1)
