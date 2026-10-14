"""Source-only regression checks for the 5.5.2 Install hardware and CanIRun.ai score repairs."""

from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent


def require(relative_path: str, *markers: str) -> None:
    content = (ROOT / relative_path).read_text(encoding="utf-8-sig")
    for marker in markers:
        if marker not in content:
            raise SystemExit(f"Install/CanIRun.ai contract missing in {relative_path}: {marker}")


require(
    "src/LocalGPT/Components/Shared/InvariantDoubleEditor.razor",
    'return $"n{Math.Clamp(DecimalPlaces, 0, 8)}";',
    'MaxValue="@Max"',
    'Culture="@CultureInfo.InvariantCulture"',
)
require(
    "src/LocalGPT/Components/Pages/Install.razor",
    '<InitialSetupAssistantPanel @bind-CachedRecommendations="CanIRunRecommendationCache" />',
    'InvariantDoubleEditor @bind-Value="hardwareDraft.DedicatedVramGiB"',
)
require(
    "src/LocalGPT/Components/Pages/Install.razor.cs",
    'IReadOnlyList<CanIRunModelRecommendation> CanIRunRecommendationCache',
)
require(
    "src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor",
    'InvariantDoubleEditor @bind-Value="device.DedicatedVramGiB"',
    'recommendations.AddRange(CachedRecommendations)',
    'CachedRecommendationsChanged.InvokeAsync(reduced.ToList())',
    '<DxGrid Data="@recommendations" PageSize="12"',
    'FieldName="@nameof(CanIRunModelRecommendation.Score)"',
    'await RebuildModelChoicesAsync().ConfigureAwait(false);',
)
require(
    "src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs",
    'var matchingRow = compatibilityRows',
    'var fallbackRow = BuildCompatibilityFallback',
    '.OrderByDescending(item => item!.Score)',
    '"fitScore", "overallScore"',
    'double.TryParse(property.Value.GetString(), NumberStyles.Float',
    'Math.Round(numericScore, MidpointRounding.AwayFromZero)',
)

print("LocalGPT 5.5.2 Install VRAM and CanIRun.ai score/source-retention audit passed.")
