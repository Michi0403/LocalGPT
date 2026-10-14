using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates DevExpress AI function catalog behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class DxAiFunctionCatalogService
{
    /// <summary>Two-stage, persisted-policy-aware function discovery. Detail schemas are returned only when explicitly requested.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function discovery result produced by the operation.</returns>
    public async Task<DxAiFunctionDiscoveryResult> DiscoverForAiAsync(DxAiFunctionDiscoveryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var enabled = (await GetEntriesAsync(cancellationToken).ConfigureAwait(false))
                .Where(entry => entry.CatalogKey.StartsWith("dx:", StringComparison.OrdinalIgnoreCase) &&
                                entry.IsAvailable && entry.IsEnabled && entry.ExposeToAiChat)
                .ToDictionary(entry => entry.FunctionName, StringComparer.OrdinalIgnoreCase);
            var tokens = (request.Query ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var results = registry.GetFunctions()
                .Where(function => function.AvailableToAi && function.SupportsDirectInvocation && enabled.ContainsKey(function.Name))
                .Select(function => new
                {
                    Function = function,
                    Policy = enabled[function.Name],
                    Score = tokens.Sum(token =>
                        function.Name.Contains(token, StringComparison.OrdinalIgnoreCase) ? 4 :
                        function.Purpose.Contains(token, StringComparison.OrdinalIgnoreCase) ? 2 :
                        function.Parameters.Contains(token, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                })
                .Where(row => tokens.Length == 0 || row.Score > 0)
                .OrderByDescending(row => row.Score)
                .ThenBy(row => row.Function.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            logger.LogDebug("Policy-filtered DX function discovery returned {Total} candidate(s); page size {Take}.", results.Count, request.Take);
            return new DxAiFunctionDiscoveryResult
            {
                Total = results.Count,
                Functions = results.Skip(Math.Max(0, request.Skip)).Take(Math.Clamp(request.Take, 1, 50))
                    .Select(row => new DxAiFunctionDiscoveryItem
                    {
                        FunctionName = row.Function.Name,
                        Purpose = row.Function.Purpose,
                        DescriptorHash = row.Policy.DescriptorHash,
                        ParameterSchemaJson = request.IncludeSchemas ? row.Function.ParameterSchemaJson : string.Empty,
                        IsReadOnly = row.Function.IsReadOnly,
                        RequiresFrontendConfirmation = row.Policy.RequiresFrontendConfirmation ||
                            (!row.Function.IsReadOnly && !row.Function.IsCoordinationOnly),
                        SupportsAutomaticInvocation = row.Function.SupportsAutomaticInvocation
                    }).ToList()
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Discovering policy-enabled DX functions failed; search input was omitted.");
            throw;
        }
    }
}
