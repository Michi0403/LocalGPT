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
    /// Writes review document as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="workspaceRoot">Workspace root value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="review">Review value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="payload">Payload value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task WriteReviewDocumentAsync(
        string workspaceRoot,
        CodeGenerationChangeReview review,
        CodeGenerationReviewPayload payload,
        CancellationToken cancellationToken)
    {
    try
    {
            var builder = new StringBuilder()
                .AppendLine("# LocalGPT Change Review")
                .AppendLine()
                .AppendLine($"- Review ID: `{review.Id}`")
                .AppendLine($"- Review hash: `{review.ReviewHash}`")
                .AppendLine($"- Project ID: `{review.ProjectId?.ToString() ?? "not linked"}`")
                .AppendLine($"- Project revision ID: `{review.ProjectRevisionId?.ToString() ?? "not linked"}`")
                .AppendLine($"- Council run ID: `{review.CouncilRunId?.ToString() ?? "not linked"}`")
                .AppendLine()
                .AppendLine("## Goal")
                .AppendLine(review.Goal)
                .AppendLine()
                .AppendLine("## Current project state")
                .AppendLine(review.CurrentProjectState)
                .AppendLine()
                .AppendLine("## Council summary")
                .AppendLine(review.CouncilSummary)
                .AppendLine()
                .AppendLine("## Approved change set")
                .AppendLine(review.ChangeSummary)
                .AppendLine()
                .AppendLine("## Safety and execution boundary")
                .AppendLine(review.SafetySummary)
                .AppendLine()
                .AppendLine("## Files")
                .AppendLine();

            foreach (var file in payload.Files)
                builder.AppendLine($"- `{file.RelativePath}` — {file.Purpose} ({file.Content.Length:n0} characters)");
            foreach (var type in payload.CodeDomTypes)
                builder.AppendLine($"- `{type.RelativePath}` — CodeDOM type `{type.Namespace}.{type.TypeName}`");

            builder.AppendLine().AppendLine("## Outputs").AppendLine();
            foreach (var output in payload.Outputs)
                builder.AppendLine($"- `{output.Kind}` — `{output.RelativeDirectory}` / `{output.Name}` targeting `{output.TargetFramework}`");

            builder.AppendLine()
                .AppendLine("Generated source and scripts are not executed automatically. A bounded .NET build occurs only when separately enabled and confirmed by the current human for this exact review hash.");

            await File.WriteAllTextAsync(Path.Combine(workspaceRoot, "CHANGE_REVIEW.md"), builder.ToString(), cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(WriteReviewDocumentAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(WriteReviewDocumentAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs copy tracked project into workspace as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="workspaceRoot">Workspace root value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="result">Result value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> CopyTrackedProjectIntoWorkspaceAsync(
        string workspaceRoot,
        Guid projectId,
        Guid? revisionId,
        CodeGenerationExecutionResult result,
        CancellationToken cancellationToken)
    {
    try
    {
            var tracked = await projectMaintenance.GetTrackedFilesAsync(projectId, revisionId, cancellationToken).ConfigureAwait(false);
            var approved = tracked.Where(item => item.Exists && item.IsUserApproved && !item.IsGenerated).OrderBy(item => item.ProjectRelativePath, StringComparer.Ordinal).ToList();
            if (approved.Count == 0)
            {
                result.Warnings.Add("No approved tracked project files were available to clone. Scan the selected project revision before executing a maintenance review.");
                return string.Empty;
            }

            string solutionPath = string.Empty;
            foreach (var file in approved)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!File.Exists(file.AbsolutePath))
                    throw new FileNotFoundException("A tracked project file disappeared before the approved maintenance workspace was created.", file.AbsolutePath);
                var sourceHash = await ComputeFileHashAsync(file.AbsolutePath, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(sourceHash, file.ContentHash, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Tracked file '{file.ProjectRelativePath}' changed after the approved scan. Rescan the revision before creating a maintenance workspace.");
                var relativePath = NormalizeRelativePath(file.ProjectRelativePath);
                var destination = ResolveInsideRoot(workspaceRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? workspaceRoot);
                {
                    var source = new FileStream(file.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
                    await using var configuredSourceAsyncDisposal = source.ConfigureAwait(false);
                    var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                    await using var configuredTargetAsyncDisposal = target.ConfigureAwait(false);
                    await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
                    await target.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                var destinationHash = await ComputeFileHashAsync(destination, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(sourceHash, destinationHash, StringComparison.Ordinal))
                    throw new IOException($"The isolated copy of '{file.ProjectRelativePath}' did not preserve the approved file bytes.");
                result.WrittenFiles.Add(relativePath.Replace('\\', '/'));
                if (Path.GetExtension(relativePath).Equals(".sln", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(relativePath).Equals(".slnx", StringComparison.OrdinalIgnoreCase))
                    solutionPath = destination;
            }
            logger.LogInformation("Cloned {FileCount} approved tracked file(s) into isolated maintenance workspace for project {ProjectId} revision {RevisionId}; paths omitted from logs.", approved.Count, projectId, revisionId);
            return solutionPath;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(CopyTrackedProjectIntoWorkspaceAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(CopyTrackedProjectIntoWorkspaceAsync)} failed.");
        throw;
    }
}


    /// <summary>
    /// Computes file hash as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> ComputeFileHashAsync(string path, CancellationToken cancellationToken)
    {
    try
    {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
            return Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ComputeFileHashAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ComputeFileHashAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Finds preferred solution path as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="workspaceRoot">Workspace root value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="clonedSolutionPath">Cloned solution path value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string FindPreferredSolutionPath(string workspaceRoot, string clonedSolutionPath)
    {
        if (!string.IsNullOrWhiteSpace(clonedSolutionPath) && File.Exists(clonedSolutionPath))
            return clonedSolutionPath;
        try
        {
            return Directory.EnumerateFiles(workspaceRoot, "*.sln", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(workspaceRoot, "*.slnx", SearchOption.AllDirectories))
                .OrderBy(path => path.Count(character => character == Path.DirectorySeparatorChar))
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? string.Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(ex, "Could not enumerate solution files under workspace {WorkspaceRoot}.", workspaceRoot);
            return string.Empty;
        }
    }

    /// <summary>
    /// Generates code DOM source as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="spec">Spec value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GenerateCodeDomSource(CodeDomTypeSpec spec)
    {
    try
    {
            var unit = new CodeCompileUnit();
            var ns = new CodeNamespace(spec.Namespace);
            ns.Imports.Add(new CodeNamespaceImport("System"));
            unit.Namespaces.Add(ns);

            var type = new CodeTypeDeclaration(spec.TypeName)
            {
                IsClass = true,
                TypeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Sealed
            };
            if (!string.IsNullOrWhiteSpace(spec.Summary))
                type.Comments.Add(new CodeCommentStatement(spec.Summary));
            ns.Types.Add(type);

            var method = new CodeMemberMethod
            {
                Name = spec.MethodName,
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = new CodeTypeReference(typeof(string))
            };
            method.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(spec.MethodResult)));
            type.Members.Add(method);

            using var writer = new StringWriter();
            writer.WriteLine("// <auto-generated>");
            writer.WriteLine("// Generated from a user-approved LocalGPT change review.");
            writer.WriteLine("// </auto-generated>");
            writer.WriteLine();
            using var provider = new CSharpCodeProvider();
            provider.GenerateCodeFromCompileUnit(unit, writer, new CodeGeneratorOptions
            {
                BracingStyle = "C",
                BlankLinesBetweenMembers = true
            });
            return writer.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(GenerateCodeDomSource)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(GenerateCodeDomSource)} failed.");
        throw;
    }
}

    /// <summary>
    /// Generates the deterministic plain-text C# fallback used when the platform CodeDOM provider is unavailable.
    /// </summary>
    /// <param name="spec">Spec value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GeneratePlainCSharpFallbackSource(CodeDomTypeSpec spec)
    {
        try
        {
            var summary = System.Security.SecurityElement.Escape(ValueOrFallback(spec.Summary, "Reviewed generated type")) ?? "Reviewed generated type";
            return $$"""
            namespace {{spec.Namespace}};

            /// <summary>{{summary}}</summary>
            public sealed class {{spec.TypeName}}
            {
                /// <summary>Returns the reviewed generated result.</summary>
                public string {{spec.MethodName}}() => "{{EscapeCSharp(spec.MethodResult)}}";
            }
            """;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Plain C# fallback generation failed; reviewed source content was omitted.");
            throw;
        }
    }

    }
}
