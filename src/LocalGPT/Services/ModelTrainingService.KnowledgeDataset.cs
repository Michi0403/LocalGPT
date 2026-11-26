using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

public sealed partial class ModelTrainingService
{
    private sealed record DatasetReceipt(string Kind, string TrainSha256, string EvaluationSha256, string? KnowledgeRevisionsSha256);
    private sealed record KnowledgeRevision(Guid KnowledgeId, string ContentSha256, bool Evaluation);
    private sealed record CheckpointFileReceipt(string FileName, string Sha256);

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
            var provenance = new List<KnowledgeRevision>();
            var seenHashes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sample in samples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = await knowledge.GetEntryAsync(sample.KnowledgeId, cancellationToken).ConfigureAwait(false);
                var hash = entry is null ? null : knowledge.InspectTrainingRevision(entry, DateTime.UtcNow).ContentSha256;
                if (entry is null || hash is null)
                    throw new InvalidOperationException($"Knowledge entry {sample.KnowledgeId} needs a current, user-verified source excerpt.");

                if (!hash.Equals(sample.ContentSha256, StringComparison.OrdinalIgnoreCase) || !seenHashes.Add(hash))
                    throw new InvalidOperationException($"Knowledge entry {sample.KnowledgeId} has changed or duplicates another selected answer.");

                var destination = sample.Evaluation ? evaluation : train;
                destination.Add(JsonSerializer.Serialize(new { prompt = sample.Prompt, completion = entry.Content }));
                provenance.Add(new(sample.KnowledgeId, hash, sample.Evaluation));
            }

            var folder = Path.Combine(ResolveTrainingRoot(), $"{key}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(folder);
            var encoding = new UTF8Encoding(false);
            var revisionsFile = Path.Combine(folder, "knowledge-revisions.json");
            await File.WriteAllTextAsync(revisionsFile, JsonSerializer.Serialize(provenance), encoding, cancellationToken).ConfigureAwait(false);
            await WritePreparedDatasetAsync(folder, train, evaluation, revisionsFile, cancellationToken).ConfigureAwait(false);
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

    private async Task WritePreparedDatasetAsync(string folder, IEnumerable<string> train, IEnumerable<string> evaluation,
        string? revisionsFile, CancellationToken cancellationToken)
    {
        var encoding = new UTF8Encoding(false);
        var trainFile = Path.Combine(folder, runtimePolicy.GetString("ModelTrainingTrainFileName"));
        var evaluationFile = Path.Combine(folder, runtimePolicy.GetString("ModelTrainingEvalFileName"));
        var pendingEvaluationFile = evaluationFile + ".pending";
        await File.WriteAllLinesAsync(trainFile, train, encoding, cancellationToken).ConfigureAwait(false);
        await File.WriteAllLinesAsync(pendingEvaluationFile, evaluation, encoding, cancellationToken).ConfigureAwait(false);
        var receipt = new DatasetReceipt(revisionsFile is null ? "identity" : "reviewed-knowledge",
            await HashTrainingFileAsync(trainFile, cancellationToken).ConfigureAwait(false),
            await HashTrainingFileAsync(pendingEvaluationFile, cancellationToken).ConfigureAwait(false),
            revisionsFile is null ? null : await HashTrainingFileAsync(revisionsFile, cancellationToken).ConfigureAwait(false));
        await File.WriteAllTextAsync(Path.Combine(folder, "dataset.json"), JsonSerializer.Serialize(receipt), encoding, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        File.Move(pendingEvaluationFile, evaluationFile);
    }

    private async Task<DatasetReceipt> InspectDatasetAsync(string folder, string trainFile, string evaluationFile, CancellationToken cancellationToken)
    {
        var receiptPath = Path.Combine(folder, "dataset.json");
        var revisionsPath = Path.Combine(folder, "knowledge-revisions.json");
        var hasRevisions = File.Exists(revisionsPath);
        var actual = new DatasetReceipt(hasRevisions ? "reviewed-knowledge" : "identity",
            await HashTrainingFileAsync(trainFile, cancellationToken).ConfigureAwait(false),
            await HashTrainingFileAsync(evaluationFile, cancellationToken).ConfigureAwait(false),
            hasRevisions ? await HashTrainingFileAsync(revisionsPath, cancellationToken).ConfigureAwait(false) : null);
        if (!File.Exists(receiptPath))
        {
            if (hasRevisions)
                throw new InvalidOperationException("Prepare the knowledge dataset again to record its reviewed split before training.");
            return actual with { Kind = "legacy-unverified" };
        }
        var prepared = JsonSerializer.Deserialize<DatasetReceipt>(await File.ReadAllTextAsync(receiptPath, cancellationToken).ConfigureAwait(false));
        if (actual != prepared)
            throw new InvalidOperationException("The prepared dataset or its source receipt has changed. Prepare the approved dataset again.");
        if (hasRevisions)
        {
            var revisions = JsonSerializer.Deserialize<List<KnowledgeRevision>>(await File.ReadAllTextAsync(revisionsPath, cancellationToken).ConfigureAwait(false));
            if (revisions is null || revisions.Count < 3)
                throw new InvalidOperationException("The prepared dataset is missing its reviewed source revisions.");
            var snapshotUtc = DateTime.UtcNow;
            foreach (var revision in revisions)
            {
                var entry = await knowledge.GetEntryAsync(revision.KnowledgeId, cancellationToken).ConfigureAwait(false);
                if (entry is null || knowledge.InspectTrainingRevision(entry, snapshotUtc).ContentSha256 != revision.ContentSha256)
                    throw new InvalidOperationException($"Knowledge entry {revision.KnowledgeId} needs a current reviewed revision. Prepare the dataset again.");
            }
        }
        return actual;
    }

    private async Task<string> HashTrainingFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
    }

    private async Task<IReadOnlyList<CheckpointFileReceipt>> InspectTrainingCheckpointAsync(string folder, CancellationToken cancellationToken)
    {
        var files = Directory.EnumerateFiles(folder)
            .Where(path => Path.GetExtension(path) is ".safetensors" or ".json" or ".model" or ".txt" or ".jinja")
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal);
        var receipts = new List<CheckpointFileReceipt>();
        foreach (var file in files)
            receipts.Add(new(Path.GetFileName(file), await HashTrainingFileAsync(file, cancellationToken).ConfigureAwait(false)));
        if (!receipts.Any(item => item.FileName.EndsWith(".safetensors", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("The checkpoint has no safetensors weights to verify.");
        return receipts;
    }
}
