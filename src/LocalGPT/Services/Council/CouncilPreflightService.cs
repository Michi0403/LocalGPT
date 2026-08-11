using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services.Council;

/// <summary>
/// Performs the bounded, lossless database and runtime-directory audit required before every council run.
/// Deterministic seed gaps are filled by database initialization; missing volatile facts are returned as
/// explicit questions instead of being guessed.
/// </summary>
public sealed class CouncilPreflightService(
    IDatabaseInitializationService databaseInitialization,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IRuntimeCapabilityDirectoryService capabilityDirectory,
    IOneWirePeerRegistry oneWirePeers,
    ICouncilTeamConfigurationService teams,
    ILogger<CouncilPreflightService> logger) : ICouncilPreflightService
{
    /// <summary>
    /// Runs the prepare async operation.
    /// </summary>
    public async Task<CouncilPreflightReport> PrepareAsync(
        MultiModelCouncilRequest request,
        IReadOnlyList<string> participants,
        IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
        CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(participants);
            ArgumentNullException.ThrowIfNull(modelRoutes);

            await databaseInitialization.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var directory = await capabilityDirectory.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
            var functionDirectory = directory.Functions;
            var skillDirectory = directory.Skills;
            var team = await teams.FindTeamAsync(request.CouncilTeamKey, cancellationToken).ConfigureAwait(false)
                ?? await teams.FindTeamAsync("general", cancellationToken).ConfigureAwait(false);

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var regexNames = await db.RegexPatterns.AsNoTracking()
                .OrderBy(item => item.Name)
                .Select(item => item.Name)
                .Take(512)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var projectNames = await db.LocalGptProjects.AsNoTracking()
                .Where(project => !project.IsArchived)
                .OrderBy(project => project.Name)
                .Select(project => project.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var memberLinks = await db.CouncilMemberOrganicSkillLinks.AsNoTracking()
                .Include(item => item.Skill)
                .Where(item => item.IsEnabled)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var report = new CouncilPreflightReport
            {
                RegexPatternCount = await db.RegexPatterns.CountAsync(cancellationToken).ConfigureAwait(false),
                KnowledgeEntryCount = await db.CouncilKnowledgeEntries.CountAsync(cancellationToken).ConfigureAwait(false),
                ProjectCount = projectNames.Count,
                DxFunctionCount = functionDirectory.Count,
                OrganicSkillCount = skillDirectory.Count,
                TeamKey = team?.Key ?? "general",
                TeamName = team?.DisplayName ?? "Organic Project Team",
                IntroductionPromptTemplate = team?.WorkflowSteps
                    .FirstOrDefault(step => step.IsEnabled && string.Equals(step.Key, "member-readiness-introduction", StringComparison.OrdinalIgnoreCase))
                    ?.PromptTemplate ?? string.Empty,
                ProjectNames = projectNames,
                FunctionNames = functionDirectory.Select(item => item.Name).ToList(),
                SkillKeys = skillDirectory.Select(item => item.Key).ToList(),
                OnlineSkillKeys = skillDirectory.Where(item => item.IsEnabled && item.IsOnline).Select(item => item.Key).ToList(),
                OfflineSkillKeys = skillDirectory.Where(item => !item.IsOnline).Select(item => item.Key).ToList(),
                RegexNames = regexNames
            };
            report.Warnings.AddRange(directory.Warnings);

            report.CapabilityTeachings = oneWirePeers.GetPeers()
                .Where(peer => peer.IsConnected)
                .SelectMany(peer => peer.Capabilities.Select(capability => new { Peer = peer, Capability = capability }))
                .Where(item => item.Capability.IsEnabled && item.Capability.IsOnline && item.Capability.IsExposedToPeer)
                .OrderBy(item => item.Peer.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Capability.Key, StringComparer.OrdinalIgnoreCase)
                .Take(120)
                .Select(item =>
                {
                    var capability = item.Capability;
                    var roles = capability.SuggestedCouncilRoles.Count == 0 ? "not role-restricted" : string.Join(", ", capability.SuggestedCouncilRoles);
                    return $"{item.Peer.DisplayName}/{capability.Key} | input: {capability.InputContract} | output: {capability.OutputContract} | security: {capability.SecurityContract} | organic use: {capability.OrganicUseCase} | suggested roles: {roles}";
                })
                .ToList();

            var allCallableFunctions = functionDirectory
                .Where(item => item.AvailableToAi)
                .Select(item => item.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var allOnlineSkills = skillDirectory
                .Where(item => item.IsEnabled && item.IsOnline)
                .Select(item => item.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var participant in participants.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                modelRoutes.TryGetValue(participant, out var plan);
                var links = memberLinks
                    .Where(item => string.Equals(item.MemberKey, participant, StringComparison.OrdinalIgnoreCase))
                    .Where(item => item.Skill is { IsEnabled: true, IsUserApproved: true })
                    .ToList();
                var linkedFunctions = links
                    .SelectMany(item => ParseStringArray(item.DxFunctionsJson))
                    .Where(name => allCallableFunctions.Contains(name, StringComparer.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var linkedSkills = links
                    .Select(item => item.Skill?.Key)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Cast<string>()
                    .Concat(links.SelectMany(item => ParseStringArray(item.OrganicCapabilitiesJson)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var readiness = new CouncilMemberReadiness
                {
                    ModelName = participant,
                    LaneKey = plan?.LaneKey ?? $"auto:{participant}",
                    HardwareKind = plan?.HardwareKind ?? OneWireHardwareKind.Auto,
                    HardwareIndex = plan?.HardwareIndex ?? -1,
                    HardwareName = plan?.HardwareName ?? "Automatic",
                    EffectiveLoadPercent = plan?.EffectiveLoadPercent ?? request.ResourceLoadPercent,
                    EffectiveMaxOutputTokens = plan?.EffectiveMaxOutputTokens ?? request.MaxOutputTokens,
                    EffectiveMaxContextTokens = plan?.EffectiveMaxContextTokens ?? request.MaxContextTokens,
                    OllamaNumGpu = plan?.OllamaNumGpu ?? request.OllamaNumGpu,
                    AssignedDxFunctions = linkedFunctions.Count > 0 ? linkedFunctions : allCallableFunctions.Take(512).ToList(),
                    AssignedOrganicSkills = linkedSkills.Count > 0 ? linkedSkills : allOnlineSkills.Take(256).ToList()
                };

                if (readiness.EffectiveLoadPercent is < 0 or > 100)
                    readiness.MissingCapabilities.Add("The effective hardware-load percentage must be between 0 and 100.");
                if (readiness.EffectiveLoadPercent % 5 != 0)
                    readiness.MissingCapabilities.Add("The effective hardware-load percentage must use a 5% step.");
                if (readiness.EffectiveMaxOutputTokens <= 0)
                    readiness.MissingCapabilities.Add("A positive model output-token budget is required.");
                if (readiness.EffectiveMaxContextTokens <= 0)
                    readiness.MissingCapabilities.Add("A positive model context-token budget is required.");
                if (readiness.EffectiveMaxOutputTokens > readiness.EffectiveMaxContextTokens)
                    readiness.MissingCapabilities.Add("The output-token budget cannot exceed the context-token budget.");
                if (readiness.HardwareKind == OneWireHardwareKind.Cpu && readiness.OllamaNumGpu is > 0)
                    readiness.MissingCapabilities.Add("The selected CPU road conflicts with a positive Ollama num_gpu value.");
                if (readiness.HardwareKind is OneWireHardwareKind.Gpu or OneWireHardwareKind.Accelerator && readiness.HardwareIndex < 0)
                    readiness.MissingCapabilities.Add("A GPU/accelerator road needs an explicit non-negative device index.");
                if (readiness.AssignedDxFunctions.Count == 0)
                    readiness.MissingCapabilities.Add("No AI-callable DXFunctions are registered.");
                if (readiness.AssignedOrganicSkills.Count == 0)
                    readiness.MissingCapabilities.Add("No approved online organic skill is currently assigned or advertised.");
                report.Members.Add(readiness);
            }

            if (report.RegexPatternCount == 0)
                report.MissingRequirements.Add("The database contains no regex definitions. Restart database initialization or restore the deterministic seed catalog.");
            if (report.KnowledgeEntryCount == 0)
                report.MissingRequirements.Add("The knowledge database is empty. Feed source-backed project/domain material before claiming current facts.");
            if (report.ProjectCount < 2)
                report.MissingRequirements.Add("The LocalGPT Core and Humanitarian Collaboration Workspace projects must exist before the run.");
            if (report.DxFunctionCount == 0)
                report.MissingRequirements.Add("The DI-backed DXFunction directory is empty; controller/function discovery must be repaired before automatic tool use.");
            if (team is null)
                report.MissingRequirements.Add($"Council team '{request.CouncilTeamKey}' is missing or disabled.");
            if (string.IsNullOrWhiteSpace(request.Prompt))
                report.MissingRequirements.Add("A user topic or question is required.");

            report.Warnings.AddRange(report.Members.SelectMany(member => member.MissingCapabilities.Select(value => $"{member.ModelName}: {value}")));
            report.PromptContext = BuildPromptContext(report, request);

            logger.LogInformation(
                "Council preflight completed: {RegexCount} regexes, {KnowledgeCount} knowledge entries, {ProjectCount} projects, {FunctionCount} DXFunctions, {SkillCount} organic skills and {MissingCount} missing requirement(s).",
                report.RegexPatternCount,
                report.KnowledgeEntryCount,
                report.ProjectCount,
                report.DxFunctionCount,
                report.OrganicSkillCount,
                report.MissingRequirements.Count);
            return report;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilPreflightService)}.{nameof(PrepareAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilPreflightService)}.{nameof(PrepareAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds member readiness prompt.
    /// </summary>
    public string BuildMemberReadinessPrompt(
        string modelName,
        IReadOnlyList<string> participants,
        CouncilPreflightReport report)
    {
    try
    {
            var member = report.Members.FirstOrDefault(item => string.Equals(item.ModelName, modelName, StringComparison.OrdinalIgnoreCase));
            var assignedFunctions = member?.AssignedDxFunctions ?? report.FunctionNames;
            var assignedSkills = member?.AssignedOrganicSkills ?? report.SkillKeys;
            var lines = new List<string>
            {
                $"You are entering the mandatory readiness and introduction phase as {modelName}.",
                $"Council members: {string.Join(", ", participants)}.",
                RenderIntroductionTemplate(report.IntroductionPromptTemplate, modelName, participants, report),
                "Confirm the hardware road, token range, available DXFunctions, approved skills and organic organs before substantive work.",
                member is null
                    ? "No explicit hardware road was found; report this as a configuration gap and continue conservatively on Automatic."
                    : $"Your road is {member.LaneKey} ({member.HardwareKind} {member.HardwareIndex}, {member.HardwareName}) at {member.EffectiveLoadPercent}% with output {member.EffectiveMaxOutputTokens:n0}, context {member.EffectiveMaxContextTokens:n0}, Ollama num_gpu {(member.OllamaNumGpu?.ToString() ?? "auto")}.",
                $"DXFunctions directly available to you in this run: {string.Join(", ", assignedFunctions.Take(200))}.",
                $"Approved online organic skills/organs directly available to you: {(assignedSkills.Count == 0 ? "none" : string.Join(", ", assignedSkills.Take(120)))}.",
                $"Known organic add-ons that are currently offline or discovery-only: {(report.OfflineSkillKeys.Count == 0 ? "none" : string.Join(", ", report.OfflineSkillKeys.Take(120)))}. Do not claim they are callable until their trusted 1-Wire peer connects.",
                report.CapabilityTeachings.Count == 0
                    ? "No connected peer currently exposes a detailed 1-Wire capability contract. Do not invent external organs."
                    : "Connected 1-Wire capability teaching (exact input, output, security, organic use and suggested role):" + Environment.NewLine + string.Join(Environment.NewLine, report.CapabilityTeachings.Select(value => "- " + value)),
                "For any 1-Wire call, preserve the returned CorrelationId, handle ApprovalRequired without reissuing the request, and continue only from the matching WorkResult.",
                "Introduce yourself to the other members. State what you believe your strongest useful skills are, what evidence would verify them, what you want to improve in LocalGPT, and which DXFunctions or 1-Wire organs you can best use.",
                "Do not claim a function is available unless it appears in the supplied directory. Ask the user for missing compiler versions, scientific constants, project files, matching debug symbols or other current facts instead of guessing.",
                "Leaders and preparation experts must verify every member's hardware road, function directory and skill/organ access before accepting the substantive round.",
                "End with a compact readiness verdict: Ready, ReadyWithQuestions, or Blocked, followed by the exact questions or missing requirements.",
                "A prose verdict does not pause Council execution. If an answer must block the next phase, next round, or completion, invoke human.collaboration.request now with the matching gate and honest Member, SelectedMembers, or Consensus scope. Use gate None for advisory questions that should not stop work."
            };
            if (member?.MissingCapabilities.Count > 0)
            {
                lines.Add("Your member-specific readiness gaps:");
                lines.AddRange(member.MissingCapabilities.Select(item => "- " + item));
            }
            if (report.MissingRequirements.Count > 0)
            {
                lines.Add("Preflight requirements that must be addressed or explicitly carried as questions:");
                lines.AddRange(report.MissingRequirements.Select(item => "- " + item));
            }
            lines.Add("When evidence about your own strengths is available, append one localgpt-self-assessment block using valid JSON. It remains disabled, untrusted evidence until user approval.");
            return string.Join(Environment.NewLine, lines);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilPreflightService)}.{nameof(BuildMemberReadinessPrompt)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilPreflightService)}.{nameof(BuildMemberReadinessPrompt)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the render introduction template operation.
    /// </summary>
    private string RenderIntroductionTemplate(string template, string modelName, IReadOnlyList<string> participants, CouncilPreflightReport report)
    {
    try
    {
            var effective = string.IsNullOrWhiteSpace(template)
                ? "Introduce yourself, confirm readiness, and identify exact missing requirements before substantive work."
                : template.Trim();
            return effective
                .Replace("{{ModelName}}", modelName, StringComparison.Ordinal)
                .Replace("{{CouncilMembers}}", string.Join(", ", participants), StringComparison.Ordinal)
                .Replace("{{TeamName}}", report.TeamName, StringComparison.Ordinal)
                .Replace("{{TeamKey}}", report.TeamKey, StringComparison.Ordinal);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilPreflightService)}.{nameof(RenderIntroductionTemplate)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilPreflightService)}.{nameof(RenderIntroductionTemplate)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds prompt context.
    /// </summary>
    private string BuildPromptContext(CouncilPreflightReport report, MultiModelCouncilRequest request)
    {
    try
    {
            var builder = new StringBuilder()
                .AppendLine("Mandatory LocalGPT council preflight")
                .AppendLine($"Checked UTC: {report.CheckedAtUtc:O}")
                .AppendLine($"Team: {report.TeamName} ({report.TeamKey})")
                .AppendLine($"Database: {report.RegexPatternCount} regexes; {report.KnowledgeEntryCount} knowledge entries; {report.ProjectCount} active projects.")
                .AppendLine($"Runtime directory: {report.DxFunctionCount} DXFunctions; {report.OnlineSkillKeys.Count} online organic skills; {report.OfflineSkillKeys.Count} offline/discovery-only organic add-ons; {report.CapabilityTeachings.Count} connected detailed 1-Wire capability contracts.")
                .AppendLine($"Known offline/discovery-only organic add-ons: {(report.OfflineSkillKeys.Count == 0 ? "none" : string.Join(", ", report.OfflineSkillKeys.Take(120)))}.")
                .AppendLine($"Projects: {string.Join(", ", report.ProjectNames)}")
                .AppendLine($"Relevant reusable regex directory: {string.Join(", ", SelectRelevantRegexes(report.RegexNames, request.Prompt))}")
                .AppendLine("Before answering, inspect the database-grounded project, chat-memory, logs, knowledge, regex, source-file, changelog and function evidence needed for the topic. Fill deterministic seed gaps automatically. Ask the current user for missing volatile facts, versions, files, matching debug symbols or requirements. Never guess current compiler/framework/scientific values.")
                .AppendLine("Keep knowledge compact by linking project/topic/regex/function records instead of copying whole histories into every prompt.");
            if (report.MissingRequirements.Count > 0)
            {
                builder.AppendLine("Open requirements/questions:");
                foreach (var requirement in report.MissingRequirements)
                    builder.AppendLine("- " + requirement);
            }
            return builder.ToString().Trim();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilPreflightService)}.{nameof(BuildPromptContext)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilPreflightService)}.{nameof(BuildPromptContext)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the select relevant regexes operation.
    /// </summary>
    private IReadOnlyList<string> SelectRelevantRegexes(IReadOnlyList<string> names, string prompt)
    {
    try
    {
            var words = (prompt ?? string.Empty)
                .Split([' ', '\t', '\r', '\n', '.', ',', ':', ';', '/', '\\', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(word => word.Length >= 3)
                .Take(80)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var selected = names
                .Where(name => words.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase)))
                .Take(48)
                .ToList();
            if (selected.Count < 16)
                selected.AddRange(names.Where(name => !selected.Contains(name, StringComparer.OrdinalIgnoreCase)).Take(16 - selected.Count));
            return selected;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilPreflightService)}.{nameof(SelectRelevantRegexes)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilPreflightService)}.{nameof(SelectRelevantRegexes)} failed.");
        throw;
    }
}

    /// <summary>
    /// Parses string array.
    /// </summary>
    private IReadOnlyList<string> ParseStringArray(string? json)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(json))
                return [];
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilPreflightService)}.{nameof(ParseStringArray)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilPreflightService)}.{nameof(ParseStringArray)} failed.");
        throw;
    }
}
}
