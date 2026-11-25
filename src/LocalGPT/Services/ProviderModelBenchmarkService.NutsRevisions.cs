namespace LocalGPT.Services;

public sealed partial class ProviderModelBenchmarkService
{
    private async Task<Dictionary<string, string>> ReadBenchmarkRevisionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var candidates = await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
            return candidates.Where(model => model.IsInstalled && model.RevisionDigest.Length > 0)
                .ToDictionary(model => model.SelectionKey, model => model.RevisionDigest, StringComparer.OrdinalIgnoreCase);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Reading provider revisions for NUTS was canceled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Provider revisions could not be verified for the held-out benchmark.");
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }
}
