#!/usr/bin/env python3
"""Source-only invariant checks for 5.4.3 provider-model benchmark improvements."""
from pathlib import Path
import argparse

parser = argparse.ArgumentParser()
parser.add_argument('--root', type=Path, required=True)
args = parser.parse_args()
root = args.root
sources = {
    'models': 'src/LocalGPT/BusinessObjects/ProviderModelBenchmarkModels.cs',
    'execution': 'src/LocalGPT/Services/ProviderModelBenchmarkService.ProfileExecution.cs',
    'tiers': 'src/LocalGPT/Services/HardwarePerformancePresetService.cs',
    'calibration': 'src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs',
    'policy': 'src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs',
}
texts = {key:(root / value).read_text(encoding='utf-8') for key,value in sources.items()}
checks = [
    ('per-task first-provider text tracked', 'FirstProviderTextMilliseconds' in texts['models'] and 'FirstProviderTextMilliseconds' in texts['execution']),
    ('no fake provider-native loading measurement', 'It is not an Ollama-native load-duration metric.' in texts['models']),
    ('post-first-text visible-token estimate separate from wall clock', 'EstimatedGenerationTokensPerSecond' in texts['models'] and 'lastAttemptStartedMilliseconds' in texts['execution']),
    ('suite-coverage penalty', '(successful.Count / (double)result.Tasks.Count)' in texts['execution']),
    ('benchmark keepalive reads database policy', texts['execution'].count('DefaultSessionKeepAlive') == 2 and '"0s"' not in texts['execution']),
    ('all models still receive same measured token grid', 'ProfileMode = ProviderModelBenchmarkProfileMode.EvenlySpaced' in texts['calibration']),
    ('per-model tier selection in persistence', 'SelectMeasuredTierProfile(item.Profiles, tier)' in texts['tiers']),
    ('no profiles synthesized', 'item.Profiles.FindIndex(profile => ReferenceEquals(profile, item.Selected))' in texts['tiers']),
    ('fast and expert different evidence priorities', 'AverageTotalMilliseconds' in texts['tiers'] and 'AverageQualityScore *' in texts['tiers']),
    ('full-suite correctness guard', 'completeMeasurements.Count > 0 ? completeMeasurements : successfulMeasurements' in texts['tiers']),
    ('seeded benchmark policy remains present', 'ModelBenchmarkRuntimeParametersJson' in texts['policy']),
    ('user confirmation retained', 'if (!userConfirmed)' in texts['tiers']),
]
for name, passed in checks:
    if not passed:
        raise SystemExit(f'FAIL: {name}')
print(f'5.4.3 provider-model benchmark source audit passed: {len(checks)} checks.')
