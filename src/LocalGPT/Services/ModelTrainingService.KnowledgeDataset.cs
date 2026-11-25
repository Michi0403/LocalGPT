using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

public sealed partial class ModelTrainingService
{
    /// <inheritdoc />
    public async Task<(string JobFolder, int TrainSamples, int EvalSamples)> BuildKnowledgeDatasetAsync(
        string modelKey, IReadOnlyList<ModelTrainingKnowledgeSample> samples, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = ValidateTrainingModelKey(modelKey);
            if (samples is not { Count: >= 3 and <= 256 } ||
                samples.Any(sample => sample is null || sample.KnowledgeId == Guid.Empty
                    || sample.ContentSha256?.Length != 64 || string.IsNullOrWhiteSpace(sample.Prompt) || sample.Prompt.Length > 8_000)
                || samples.Count(sample => !sample.Evaluation) < 2 || samples.All(sample => !sample.Evaluation)
                || samples.Select(sample => sample.KnowledgeId).Distinct().Count() != samples.Count
                || samples.Select(sample => sample.Prompt.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != samples.Count)
                throw new InvalidOperationException("Provide 3–256 unique approved samples, including at least two training samples and one held-out evaluation sample.");

            var train = new List<string>();
            var evaluation = new List<string>();
            var provenance = new List<object>();
            var seenHashes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sample in samples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = await knowledge.GetEntryAsync(sample.KnowledgeId, cancellationToken).ConfigureAwait(false);
                if (entry is null || !entry.IsUserApproved || entry.IsArchived || entry.SupersededByKnowledgeId.HasValue
                    || entry.StalenessDetectedAtUtc.HasValue || entry.ExpiresAtUtc <= DateTime.UtcNow || entry.VerificationStatus != "UserVerified"
                    || string.IsNullOrWhiteSpace(entry.Source) || string.IsNullOrWhiteSpace(entry.Content) || entry.Content.Length > 32_000)
                    throw new InvalidOperationException($"Knowledge entry {sample.KnowledgeId} needs a current, user-verified source excerpt.");

                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(entry.Content)));
                if (!hash.Equals(sample.ContentSha256, StringComparison.OrdinalIgnoreCase) || !seenHashes.Add(hash))
                    throw new InvalidOperationException($"Knowledge entry {sample.KnowledgeId} has changed or duplicates another selected answer.");

                var destination = sample.Evaluation ? evaluation : train;
                destination.Add(JsonSerializer.Serialize(new { prompt = sample.Prompt, completion = entry.Content }));
                provenance.Add(new { sample.KnowledgeId, ContentSha256 = hash, sample.Evaluation });
            }

            var folder = Path.Combine(ResolveTrainingRoot(), $"{key}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(folder);
            var encoding = new UTF8Encoding(false);
            await File.WriteAllLinesAsync(Path.Combine(folder, runtimePolicy.GetString("ModelTrainingTrainFileName")), train, encoding, cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(folder, "knowledge-revisions.json"), JsonSerializer.Serialize(provenance), encoding, cancellationToken).ConfigureAwait(false);
            var evaluationFile = Path.Combine(folder, runtimePolicy.GetString("ModelTrainingEvalFileName"));
            var pendingEvaluationFile = evaluationFile + ".pending";
            await File.WriteAllLinesAsync(pendingEvaluationFile, evaluation, encoding, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(pendingEvaluationFile, evaluationFile);
            return (folder, train.Count, evaluation.Count);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Preparing a knowledge training dataset was canceled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning("Preparing a knowledge training dataset failed with {ExceptionType}.", exception.GetType().Name);
            throw;
        }
    }

    private string ValidateTrainingModelKey(string modelKey)
    {
        var key = (modelKey ?? string.Empty).Trim();
        var maximumLength = runtimePolicy.GetInt("ModelTrainingModelKeyMaximumLength");
        if (key.Length == 0 || key.Length > maximumLength || key.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '_' or '.')))
            throw new InvalidOperationException($"modelKey must have 1 to {maximumLength} letters, digits, '-', '_' or '.' (limit: runtime value ModelTrainingModelKeyMaximumLength).");
        return key;
    }
}
