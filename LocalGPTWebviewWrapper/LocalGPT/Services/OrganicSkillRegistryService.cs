using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using LocalGPT.WireProtocol;

namespace LocalGPT.Services;

public sealed class OrganicSkillRegistryService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<OrganicSkillRegistryService> logger) : IOrganicSkillRegistryService
{
    public async Task<IReadOnlyList<OrganicSkillDefinition>> GetSkillsAsync(bool includeDisabled = false, CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.OrganicSkills.AsNoTracking();
        if (!includeDisabled) query = query.Where(item => item.IsEnabled);
        return await query.OrderBy(item => item.Key).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<OrganicSkillDefinition> SaveSkillAsync(SaveOrganicSkillRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.UserConfirmed) throw new InvalidOperationException("Fresh human confirmation is required before changing an organic skill.");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Key);
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var key = request.Key.Trim().ToLowerInvariant();
        var entity = request.Id is Guid id ? await db.OrganicSkills.SingleOrDefaultAsync(item => item.Id == id, cancellationToken).ConfigureAwait(false) : null;
        entity ??= await db.OrganicSkills.SingleOrDefaultAsync(item => item.Key == key, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            entity = new OrganicSkillDefinition { Id = request.Id ?? Guid.NewGuid(), Key = key, CreatedAtUtc = DateTime.UtcNow };
            db.OrganicSkills.Add(entity);
        }
        entity.Key = key;
        entity.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? key : request.DisplayName.Trim();
        entity.Description = request.Description?.Trim() ?? string.Empty;
        entity.SourcePeerId = string.IsNullOrWhiteSpace(request.SourcePeerId) ? "localgpt" : request.SourcePeerId.Trim();
        entity.OrgansJson = SerializeDistinct(request.Organs);
        entity.CapabilityKeysJson = SerializeDistinct(request.CapabilityKeys);
        entity.UiActivationKeysJson = SerializeDistinct(request.UiActivationKeys);
        entity.IsOnline = request.IsOnline;
        entity.IsEnabled = request.IsEnabled;
        entity.IsUserApproved = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Saved organic skill {SkillKey}.", entity.Key);
        return entity;
    }

    public async Task<ProjectOrganicSkillLink> LinkProjectAsync(LinkProjectOrganicSkillRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.UserConfirmed) throw new InvalidOperationException("Fresh human confirmation is required before linking a project skill.");
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (!await db.LocalGptProjects.AnyAsync(item => item.Id == request.ProjectId, cancellationToken).ConfigureAwait(false)) throw new KeyNotFoundException("Project not found.");
        if (!await db.OrganicSkills.AnyAsync(item => item.Id == request.SkillId, cancellationToken).ConfigureAwait(false)) throw new KeyNotFoundException("Organic skill not found.");
        var entity = await db.ProjectOrganicSkillLinks.SingleOrDefaultAsync(item => item.ProjectId == request.ProjectId && item.SkillId == request.SkillId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            entity = new ProjectOrganicSkillLink { ProjectId = request.ProjectId, SkillId = request.SkillId };
            db.ProjectOrganicSkillLinks.Add(entity);
        }
        entity.IsRequired = request.IsRequired;
        entity.IsEnabled = request.IsEnabled;
        entity.Notes = request.Notes?.Trim() ?? string.Empty;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    public async Task<CouncilMemberOrganicSkillLink> ReportMemberSkillAsync(ReportCouncilMemberSkillRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.UserConfirmed) throw new InvalidOperationException("Fresh human confirmation is required before accepting a model self-report.");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MemberKey);
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (!await db.OrganicSkills.AnyAsync(item => item.Id == request.SkillId, cancellationToken).ConfigureAwait(false)) throw new KeyNotFoundException("Organic skill not found.");
        var member = request.MemberKey.Trim();
        var entity = await db.CouncilMemberOrganicSkillLinks.SingleOrDefaultAsync(item => item.MemberKey == member && item.SkillId == request.SkillId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            entity = new CouncilMemberOrganicSkillLink { MemberKey = member, SkillId = request.SkillId };
            db.CouncilMemberOrganicSkillLinks.Add(entity);
        }
        entity.Proficiency = Math.Clamp(request.Proficiency, 0, 100);
        entity.IsSelfRevealed = request.IsSelfRevealed;
        entity.IsEnabled = request.IsEnabled;
        entity.Evidence = request.Evidence?.Trim() ?? string.Empty;
        entity.DxFunctionsJson = SerializeDistinct(request.DxFunctions);
        entity.ControllerMethodsJson = SerializeDistinct(request.ControllerMethods);
        entity.OrganicCapabilitiesJson = SerializeDistinct(request.OrganicCapabilities);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    public async Task RecordUntrustedSelfAssessmentAsync(LocalGPT.WireProtocol.OneWireModelSelfAssessment assessment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        ArgumentException.ThrowIfNullOrWhiteSpace(assessment.MemberKey);
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var reportedSkillKeys = assessment.Skills
            .Concat(assessment.OrganicCapabilities.Select(value => $"capability:{value}"))
            .Concat(assessment.DxFunctions.Select(value => $"dxfunction:{value}"))
            .Concat(assessment.ControllerMethods.Select(value => $"controller:{value}"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(128)
            .ToList();

        foreach (var key in reportedSkillKeys)
        {
            var skill = await db.OrganicSkills.SingleOrDefaultAsync(item => item.Key == key, cancellationToken).ConfigureAwait(false);
            if (skill is null)
            {
                skill = new OrganicSkillDefinition
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    DisplayName = key,
                    Description = "Untrusted model self-report awaiting user review.",
                    SourcePeerId = string.IsNullOrWhiteSpace(assessment.ModelName) ? assessment.MemberKey : assessment.ModelName,
                    IsOnline = true,
                    IsEnabled = false,
                    IsUserApproved = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                db.OrganicSkills.Add(skill);
            }

            var link = await db.CouncilMemberOrganicSkillLinks
                .SingleOrDefaultAsync(item => item.MemberKey == assessment.MemberKey && item.SkillId == skill.Id, cancellationToken)
                .ConfigureAwait(false);
            if (link is null)
            {
                link = new CouncilMemberOrganicSkillLink
                {
                    Id = Guid.NewGuid(),
                    MemberKey = assessment.MemberKey,
                    SkillId = skill.Id
                };
                db.CouncilMemberOrganicSkillLinks.Add(link);
            }
            link.Proficiency = Math.Clamp(assessment.Confidence, 0, 100);
            link.IsSelfRevealed = true;
            link.IsEnabled = false;
            link.Evidence = assessment.Evidence?.Trim() ?? string.Empty;
            link.DxFunctionsJson = SerializeDistinct(assessment.DxFunctions);
            link.ControllerMethodsJson = SerializeDistinct(assessment.ControllerMethods);
            link.OrganicCapabilitiesJson = SerializeDistinct(assessment.OrganicCapabilities);
            link.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Stored untrusted self-assessment for council member {MemberKey}; user approval is still required before routing authority is granted.", assessment.MemberKey);
    }

    public async Task<IReadOnlyList<OneWireSkillDescriptor>> GetWireSkillsAsync(CancellationToken cancellationToken = default)
    {
        var skills = await GetSkillsAsync(false, cancellationToken).ConfigureAwait(false);
        return skills.Select(item => new OneWireSkillDescriptor
        {
            Key = item.Key,
            DisplayName = item.DisplayName,
            Description = item.Description,
            SourcePeerId = item.SourcePeerId,
            Organs = Deserialize(item.OrgansJson),
            CapabilityKeys = Deserialize(item.CapabilityKeysJson),
            UiActivationKeys = Deserialize(item.UiActivationKeysJson),
            IsOnline = item.IsOnline,
            IsEnabled = item.IsEnabled,
            UpdatedUtc = new DateTimeOffset(item.UpdatedAtUtc, TimeSpan.Zero)
        }).ToList();
    }

    private string SerializeDistinct(IEnumerable<string>? values) => JsonSerializer.Serialize((values ?? []).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(128));
    private List<string> Deserialize(string json) { try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; } catch (JsonException) { return []; } }
}
