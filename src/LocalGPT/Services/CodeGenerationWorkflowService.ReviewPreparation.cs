using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates code generation workflow behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CodeGenerationWorkflowService
    {
    /// <summary>
    /// Performs begin review scope as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="operation">Operation value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="reviewId">Identifier of the review to use for this operation.</param>
    /// <returns>The i disposable produced by the operation.</returns>
    private IDisposable? BeginReviewScope(string operation, Guid reviewId) {
    try
    {
        return logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationId"] = Guid.NewGuid(),
            ["Operation"] = operation,
            ["ReviewId"] = reviewId
        });
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BeginReviewScope)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BeginReviewScope)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs enrich output intent as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task EnrichOutputIntentAsync(CreateCodeGenerationReviewRequest request)
    {
    try
    {
            if (request.Outputs.Count > 0 || request.Files.Count > 0 || request.CodeDomTypes.Count > 0)
                return;

            var evidence = string.Join(" ", new[] { request.Title, request.Goal, request.ChangeSummary }.Where(value => !string.IsNullOrWhiteSpace(value)));
            var kind = await MatchIntentAsync("builtin.codegen-powershell-script-pattern", evidence).ConfigureAwait(false)
                ? CodeGenerationOutputKinds.PowerShellScript
                : await MatchIntentAsync("builtin.codegen-addon-pattern", evidence).ConfigureAwait(false)
                    ? CodeGenerationOutputKinds.LocalGptAddon
                    : await MatchIntentAsync("builtin.codegen-solution-pattern", evidence).ConfigureAwait(false)
                    ? CodeGenerationOutputKinds.Solution
                    : await MatchIntentAsync("builtin.codegen-console-application-pattern", evidence).ConfigureAwait(false)
                        ? CodeGenerationOutputKinds.ConsoleApplication
                        : await MatchIntentAsync("builtin.codegen-class-library-pattern", evidence).ConfigureAwait(false)
                            ? CodeGenerationOutputKinds.ClassLibrary
                            : string.Empty;

            if (string.IsNullOrWhiteSpace(kind))
                return;

            var quotedLiteral = await ExtractQuotedLiteralAsync(evidence).ConfigureAwait(false);
            var name = BuildGeneratedOutputName(request.Title, quotedLiteral ?? request.Goal);
            var description = kind == CodeGenerationOutputKinds.ConsoleApplication && !string.IsNullOrWhiteSpace(quotedLiteral)
                ? quotedLiteral
                : string.IsNullOrWhiteSpace(request.Goal)
                    ? "Generated with LocalGPT after human review."
                    : request.Goal.Trim();
            request.Outputs.Add(new CodeGenerationOutputSpec
            {
                Kind = kind,
                Name = name,
                RelativeDirectory = ".",
                TargetFramework = "net10.0",
                RootNamespace = $"LocalGPT.Generated.{name}",
                Description = description
            });
            logger.LogInformation("Resolved incomplete code-generation review into database-regex-selected output kind {OutputKind}; no source text was logged.", kind);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(EnrichOutputIntentAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(EnrichOutputIntentAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs extract quoted literal as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string?> ExtractQuotedLiteralAsync(string text)
    {
        try
        {
            var pattern = await regexPatterns.GetRegexAsync("builtin.codegen-quoted-literal-pattern").ConfigureAwait(false);
            var match = pattern?.Match(text);
            var value = match?.Groups["text"].Success == true ? match.Groups["text"].Value.Trim() : string.Empty;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not evaluate the database-backed quoted-literal pattern for code generation.");
            return null;
        }
    }

    /// <summary>
    /// Performs match intent as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="patternName">Pattern name value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="text">Text value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private async Task<bool> MatchIntentAsync(string patternName, string text)
    {
        try
        {
            var pattern = await regexPatterns.GetRegexAsync(patternName).ConfigureAwait(false);
            return pattern?.IsMatch(text) == true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not evaluate database-backed code-generation intent pattern {PatternName}.", patternName);
            return false;
        }
    }

    /// <summary>
    /// Builds generated output name as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="title">Title value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="goal">Goal value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildGeneratedOutputName(string? title, string? goal)
    {
    try
    {
            var source = string.IsNullOrWhiteSpace(title) ? goal : title;
            var words = Regex.Matches(source ?? string.Empty, "[A-Za-z0-9]+")
                .Select(match => match.Value)
                .Where(word => !word.Equals("create", StringComparison.OrdinalIgnoreCase) &&
                               !word.Equals("generate", StringComparison.OrdinalIgnoreCase) &&
                               !word.Equals("build", StringComparison.OrdinalIgnoreCase) &&
                               !word.Equals("a", StringComparison.OrdinalIgnoreCase) &&
                               !word.Equals("an", StringComparison.OrdinalIgnoreCase) &&
                               !word.Equals("the", StringComparison.OrdinalIgnoreCase))
                .Take(4)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..])
                .ToList();
            var name = string.Concat(words);
            if (string.IsNullOrWhiteSpace(name))
                name = "GeneratedFeature";
            if (char.IsDigit(name[0]))
                name = "Generated" + name;
            return name[..Math.Min(name.Length, 80)];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildGeneratedOutputName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildGeneratedOutputName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Validates review request as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    private void ValidateReviewRequest(CreateCodeGenerationReviewRequest request)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(request.Goal))
                throw new ArgumentException("A concrete generation goal is required.", nameof(request));
            if (request.Files.Count == 0 && request.CodeDomTypes.Count == 0 && request.Outputs.Count == 0)
                throw new ArgumentException("The generation request needs reviewed files, CodeDOM types, or a concrete output target. LocalGPT could not infer one from the current database-backed code-generation regex catalog.", nameof(request));
            foreach (var file in request.Files)
                _ = NormalizeRelativePath(file.RelativePath);
            foreach (var type in request.CodeDomTypes)
                _ = NormalizeRelativePath(type.RelativePath);
            foreach (var output in request.Outputs)
                _ = NormalizeRelativePath(string.IsNullOrWhiteSpace(output.RelativeDirectory) ? "." : output.RelativeDirectory);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ValidateReviewRequest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ValidateReviewRequest)} failed.");
        throw;
    }
}

    /// <summary>
    /// Validates project references as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="projectRevisionId">Identifier of the project revision to use for this operation.</param>
    /// <param name="projectTopicId">Identifier of the project topic to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ValidateProjectReferencesAsync(
        LocalGptMemoryDbContext db,
        Guid? projectId,
        Guid? projectRevisionId,
        Guid? projectTopicId,
        CancellationToken cancellationToken)
    {
    try
    {
            if (projectId is Guid selectedProjectId)
            {
                var projectExists = await db.LocalGptProjects
                    .AnyAsync(project => project.Id == selectedProjectId && !project.IsArchived, cancellationToken)
                    .ConfigureAwait(false);
                if (!projectExists)
                    throw new InvalidOperationException("The selected LocalGPT project does not exist or is archived.");
            }

            if (projectRevisionId is Guid selectedRevisionId)
            {
                if (projectId is not Guid selectedProjectIdInner)
                    throw new InvalidOperationException("A project revision can only be selected together with its project.");
                var revisionExists = await db.LocalGptProjectRevisions
                    .AnyAsync(revision => revision.Id == selectedRevisionId && revision.ProjectId == selectedProjectIdInner && revision.IsUserApproved, cancellationToken)
                    .ConfigureAwait(false);
                if (!revisionExists)
                    throw new InvalidOperationException("The selected project revision does not exist, is not user-approved, or belongs to another project.");
            }

            if (projectTopicId is Guid selectedTopicId)
            {
                var topicExists = await db.LocalGptProjectTopics
                    .AnyAsync(topic => topic.Id == selectedTopicId && topic.IsUserApproved &&
                        (!projectId.HasValue || topic.ProjectId == projectId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (!topicExists)
                    throw new InvalidOperationException("The selected project topic does not exist, is not user-approved, or belongs to another project.");
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ValidateProjectReferencesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ValidateProjectReferencesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes file as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="file">File value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The code generation file spec produced by the operation.</returns>
    private CodeGenerationFileSpec NormalizeFile(CodeGenerationFileSpec file) {
    try
    {
        return new()
    {
        RelativePath = NormalizeRelativePath(file.RelativePath),
        Content = file.Content ?? string.Empty,
        Purpose = ValueOrFallback(file.Purpose, "Reviewed source file")
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeFile)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeFile)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes code DOM type as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="type">Type value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The code DOM type spec produced by the operation.</returns>
    private CodeDomTypeSpec NormalizeCodeDomType(CodeDomTypeSpec type) {
    try
    {
        return new()
    {
        RelativePath = NormalizeRelativePath(type.RelativePath),
        Namespace = NormalizeIdentifierPath(type.Namespace, "LocalGPT.Generated"),
        TypeName = NormalizeIdentifier(type.TypeName, "GeneratedFeature"),
        MethodName = NormalizeIdentifier(type.MethodName, "Describe"),
        MethodResult = ValueOrFallback(type.MethodResult, "Generated with LocalGPT after human review."),
        Summary = ValueOrFallback(type.Summary, "Reviewed CodeDOM source type")
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeCodeDomType)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeCodeDomType)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes output as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="output">Output value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The code generation output spec produced by the operation.</returns>
    private CodeGenerationOutputSpec NormalizeOutput(CodeGenerationOutputSpec output) {
    try
    {
        return new()
    {
        Kind = NormalizeOutputKind(output.Kind),
        Name = NormalizeIdentifier(output.Name, "LocalGptGeneratedFeature"),
        RelativeDirectory = NormalizeRelativePath(string.IsNullOrWhiteSpace(output.RelativeDirectory) ? "." : output.RelativeDirectory),
        TargetFramework = NormalizeTargetFramework(output.TargetFramework),
        RootNamespace = NormalizeIdentifierPath(output.RootNamespace, "LocalGPT.Generated"),
        Description = ValueOrFallback(output.Description, "Reviewed LocalGPT output")
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeOutput)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeOutput)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes output kind as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeOutputKind(string? kind)
    {
    try
    {
            var value = kind?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                return CodeGenerationOutputKinds.SourceFiles;

            return value switch
            {
                CodeGenerationOutputKinds.SourceFiles => CodeGenerationOutputKinds.SourceFiles,
                CodeGenerationOutputKinds.ClassLibrary => CodeGenerationOutputKinds.ClassLibrary,
                CodeGenerationOutputKinds.ConsoleApplication => CodeGenerationOutputKinds.ConsoleApplication,
                CodeGenerationOutputKinds.Solution => CodeGenerationOutputKinds.Solution,
                CodeGenerationOutputKinds.LocalGptAddon => CodeGenerationOutputKinds.LocalGptAddon,
                CodeGenerationOutputKinds.CSharpScript => CodeGenerationOutputKinds.CSharpScript,
                CodeGenerationOutputKinds.PowerShellScript => CodeGenerationOutputKinds.PowerShellScript,
                CodeGenerationOutputKinds.JavaScriptModule => CodeGenerationOutputKinds.JavaScriptModule,
                _ => throw new ArgumentException($"Unsupported reviewed output kind '{value}'.")
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeOutputKind)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeOutputKind)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes target framework as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeTargetFramework(string? value)
    {
    try
    {
            var framework = string.IsNullOrWhiteSpace(value) ? "net10.0" : value.Trim();
            if (!framework.StartsWith("net", StringComparison.OrdinalIgnoreCase) || framework.Any(character => !(char.IsLetterOrDigit(character) || character is '.' or '-')))
                throw new ArgumentException("Target framework contains unsupported characters.");
            return framework;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeTargetFramework)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeTargetFramework)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds default change summary as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="payload">Payload value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildDefaultChangeSummary(CodeGenerationReviewPayload payload) {
    try
    {
        return $"Create {payload.Files.Count} explicit source file(s), {payload.CodeDomTypes.Count} CodeDOM-generated type(s), and {payload.Outputs.Count} output target(s) in an isolated LocalGPT workspace; when a project revision is linked, preserve every unchanged approved tracked file byte-for-byte.";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildDefaultChangeSummary)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildDefaultChangeSummary)} failed.");
        throw;
    }
}

    /// <summary>
    /// Computes review hash as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="entity">Entity value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ComputeReviewHash(CodeGenerationChangeReview entity)
    {
    try
    {
            var canonical = string.Join("\n",
                entity.ProjectId?.ToString("D") ?? string.Empty,
                entity.ProjectRevisionId?.ToString("D") ?? string.Empty,
                entity.ProjectTopicId?.ToString("D") ?? string.Empty,
                entity.CouncilRunId?.ToString("D") ?? string.Empty,
                entity.Title,
                entity.Goal,
                entity.CurrentProjectState,
                entity.CouncilSummary,
                entity.ChangeSummary,
                entity.SafetySummary,
                entity.PayloadJson);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ComputeReviewHash)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ComputeReviewHash)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs deserialize payload as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="payloadJson">Payload json value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The code generation review payload produced by the operation.</returns>
    private CodeGenerationReviewPayload DeserializePayload(string payloadJson)
    {
    try
    {
            var payload = JsonSerializer.Deserialize<CodeGenerationReviewPayload>(payloadJson, JsonOptions) ?? new CodeGenerationReviewPayload();
            payload.Files ??= [];
            payload.CodeDomTypes ??= [];
            payload.Outputs ??= [];
            return payload;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(DeserializePayload)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(DeserializePayload)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs to snapshot as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="entity">Entity value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="payload">Payload value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The code generation review snapshot produced by the operation.</returns>
    private CodeGenerationReviewSnapshot ToSnapshot(
        CodeGenerationChangeReview entity,
        CodeGenerationReviewPayload payload) {
    try
    {
        return new()
    {
        Id = entity.Id,
        ProjectId = entity.ProjectId,
        ProjectRevisionId = entity.ProjectRevisionId,
        ProjectTopicId = entity.ProjectTopicId,
        CouncilRunId = entity.CouncilRunId,
        Title = entity.Title,
        Goal = entity.Goal,
        CurrentProjectState = entity.CurrentProjectState,
        CouncilSummary = entity.CouncilSummary,
        ChangeSummary = entity.ChangeSummary,
        SafetySummary = entity.SafetySummary,
        ReviewHash = entity.ReviewHash,
        Status = entity.Status,
        DecisionNote = entity.DecisionNote,
        WorkspaceName = entity.WorkspaceName,
        ZipFileName = entity.ZipFileName,
        BuildStatus = entity.BuildStatus,
        ApprovalConsumed = entity.ApprovalConsumed,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc,
        DecidedAtUtc = entity.DecidedAtUtc,
        CompletedAtUtc = entity.CompletedAtUtc,
        Files = payload.Files.Select(file => new CodeGenerationFileReview
        {
            RelativePath = file.RelativePath,
            Purpose = file.Purpose,
            CharacterCount = file.Content.Length,
            ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.Content)))
        }).ToList(),
        CodeDomTypes = payload.CodeDomTypes,
        Outputs = payload.Outputs
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ToSnapshot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ToSnapshot)} failed.");
        throw;
    }
}

    }
}
