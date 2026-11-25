using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>Records held-out comparisons of provider-published adapter revisions.</summary>
public sealed class ModelAdapterRevisionService(
    IDbContextFactory<LocalGptMemoryDbContext> database,
    IProviderModelBenchmarkService benchmarks,
    IProviderModelRuntimeService providers,
    NutsBenchmarkAssessment assessment,
    ILogger<ModelAdapterRevisionService> logger)
{
    internal async Task<ModelAdapterRevision> ReviewAsync(Guid runId, string baseKey, NutsRoleEvidence candidate,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await benchmarks.LoadStoredEvidenceAsync(runId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The saved benchmark report is unavailable.");
            if (string.IsNullOrWhiteSpace(report.NutsHeldOutScenarioSha256)
                || string.Equals(baseKey, candidate.ModelSelectionKey, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Select a distinct base model measured in the same held-out benchmark.");

            var adapter = report.Targets.SingleOrDefault(target => target.Model.SelectionKey == candidate.ModelSelectionKey);
            var baseline = report.Targets.SingleOrDefault(target => target.Model.SelectionKey == baseKey);
            if (adapter is null || baseline is null || string.IsNullOrWhiteSpace(adapter.VerifiedRevisionDigest)
                || string.IsNullOrWhiteSpace(baseline.VerifiedRevisionDigest)
                || adapter.VerifiedRevisionDigest.Equals(baseline.VerifiedRevisionDigest, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The selected models need distinct provider revisions verified throughout the run.");

            var adapterProfile = adapter.Profiles.SingleOrDefault(profile => profile.ProfileName == candidate.ProfileName);
            var baseProfile = baseline.Profiles.SingleOrDefault(profile => profile.ProfileName == candidate.ProfileName);
            if (adapterProfile is null || baseProfile is null || adapterProfile.ContextTokens != baseProfile.ContextTokens
                || adapterProfile.OutputTokens != baseProfile.OutputTokens || adapterProfile.OllamaNumGpu != baseProfile.OllamaNumGpu
                || !adapter.Model.ProviderKind.Equals(baseline.Model.ProviderKind, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The base and adapter need a common provider kind and measurement profile.");

            var measured = report.NutsRoleEvidence.SingleOrDefault(evidence => SameRoleProfile(evidence, candidate)
                && evidence.ModelSelectionKey == adapter.Model.SelectionKey);
            var control = report.NutsRoleEvidence.SingleOrDefault(evidence => SameRoleProfile(evidence, candidate)
                && evidence.ModelSelectionKey == baseline.Model.SelectionKey);
            if (measured is null || control is null || measured.UnmeasuredCases.Count > 0 || control.UnmeasuredCases.Count > 0
                || measured.Passed + measured.CapabilityGaps.Count != control.Passed + control.CapabilityGaps.Count)
                throw new InvalidOperationException("Both revisions need complete evidence for the same held-out role cases.");

            var caseCount = measured.Passed + measured.CapabilityGaps.Count;
            await using var db = await database.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var previous = await db.ModelAdapterRevisions.AsNoTracking().FirstOrDefaultAsync(item =>
                item.BenchmarkRunId == runId && item.BaseModelKey == baseKey && item.AdapterModelKey == candidate.ModelSelectionKey
                && item.TeamKey == candidate.TeamKey && item.Role == candidate.Role && item.ProfileName == candidate.ProfileName,
                cancellationToken).ConfigureAwait(false);
            if (previous is not null)
                return previous;

            var review = new ModelAdapterRevision
            {
                Id = Guid.NewGuid(),
                BenchmarkRunId = runId,
                BaseModelKey = baseKey,
                AdapterModelKey = adapter.Model.SelectionKey,
                BaseDigest = baseline.VerifiedRevisionDigest,
                AdapterDigest = adapter.VerifiedRevisionDigest,
                ScenarioSha256 = report.NutsHeldOutScenarioSha256,
                TeamKey = measured.TeamKey,
                Role = measured.Role,
                ProfileName = measured.ProfileName,
                BasePassed = control.Passed,
                AdapterPassed = measured.Passed,
                CaseCount = caseCount,
                Qualified = caseCount >= 2 && measured.Passed == caseCount && measured.Passed > control.Passed,
                ReviewedAtUtc = DateTime.UtcNow
            };
            db.ModelAdapterRevisions.Add(review);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return review;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Recording an adapter revision review was canceled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Recording an adapter revision review failed for benchmark {RunId}.", runId);
            throw;
        }
    }

    internal async Task<IReadOnlyList<ModelAdapterRevision>> GetReviewsAsync(string modelKey, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await database.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await db.ModelAdapterRevisions.AsNoTracking()
                .Where(item => item.AdapterModelKey == modelKey || item.BaseModelKey == modelKey)
                .OrderByDescending(item => item.ReviewedAtUtc)
                .Take(20)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Reading adapter revision reviews was canceled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Reading adapter revision reviews failed.");
            throw;
        }
    }

    internal async Task UseForRoleAsync(Guid reviewId, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await database.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var review = await db.ModelAdapterRevisions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == reviewId, cancellationToken).ConfigureAwait(false);
            if (review is null || !review.Qualified)
                throw new InvalidOperationException("Only a qualified adapter revision can be assigned.");

            var current = await providers.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
            if (!current.Any(model => model.IsInstalled && model.SelectionKey == review.AdapterModelKey
                && model.RevisionDigest.Equals(review.AdapterDigest, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("The reviewed adapter revision has changed or is unavailable. Rerun the comparison.");

            await assessment.SaveRoleDefaultAsync(review.TeamKey, review.Role, review.AdapterModelKey, cancellationToken,
                review.AdapterDigest).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Assigning an adapter revision was canceled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Assigning adapter revision review {ReviewId} failed.", reviewId);
            throw;
        }
    }

    private bool SameRoleProfile(NutsRoleEvidence left, NutsRoleEvidence right) =>
        left.TeamKey == right.TeamKey && left.Role == right.Role && left.ProfileName == right.ProfileName;
}
