using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Describes the source-generation and generated-workspace write paths available to DXAiChat and the AI Council.
/// </summary>
/// <param name="catalog">Local gpt catalog service dependency used by the code generation capabilities function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CodeGenerationCapabilitiesFunction(
    LocalGptCatalogService catalog,
    ILogger<CodeGenerationCapabilitiesFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the code generation capabilities function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="CodeGenerationCapabilitiesFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "codegen.capabilities",
        "POST",
        "/api/dxai/functions/codegen.capabilities/invoke",
        "Returns the exact LocalGPT source-generation routes, output kinds, plain-file fallback behavior and database-backed scale policies the AI Council can use.",
        "No parameters.",
        "Read-only. This function describes capability; generation remains review/approval-gated and generated workspace writes require their own fresh approval.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{},"additionalProperties":false}
        """);

    /// <summary>
    /// Returns the maintained capability map without executing or writing source code.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = BuildCapabilitySummary();
            logger.LogDebug("DXFunction returned the code-generation capability map.");
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Succeeded = true,
                Status = "Completed",
                Value = value
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Returning the code-generation capability map failed.");
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Status = "Failed",
                Error = "The code-generation capability map could not be returned. Review LocalGPT application logs."
            });
        }
    }

    /// <summary>
    /// Builds the capability object from immutable output identifiers and database-provisioned runtime policy.
    /// </summary>
    /// <returns>The object produced by the operation.</returns>
    private object BuildCapabilitySummary()
    {
        try
        {
            return new
            {
                ReviewWorkflow = new
                {
                    Create = "codegen.review.create",
                    Inspect = "codegen.review.get",
                    Execute = "codegen.review.execute",
                    Reject = "codegen.review.reject"
                },
                OutputKinds = new[]
                {
                    CodeGenerationOutputKinds.SourceFiles,
                    CodeGenerationOutputKinds.ClassLibrary,
                    CodeGenerationOutputKinds.ConsoleApplication,
                    CodeGenerationOutputKinds.Solution,
                    CodeGenerationOutputKinds.LocalGptAddon,
                    CodeGenerationOutputKinds.CSharpScript,
                    CodeGenerationOutputKinds.PowerShellScript,
                    CodeGenerationOutputKinds.JavaScriptModule
                },
                ExactFileGeneration = new
                {
                    ReviewField = "files[]: { relativePath, content, purpose? }",
                    Meaning = "Any reviewed text/source file can be written exactly during codegen.review.execute; CodeDOM is optional.",
                    PowerShell = "Use a .ps1 file directly or select PowerShellScript output.",
                    CodeDomFallback = "When CodeDOM generation fails and no explicit reviewed file already owns the path, LocalGPT writes a plain C# fallback source file instead of losing the artifact."
                },
                ExistingGeneratedWorkspace = new
                {
                    List = "council.artifact_workspace_files",
                    Read = "council.artifact_workspace_file.read",
                    Write = "council.artifact_workspace_file.write",
                    Zip = "council.artifact_workspace_zip"
                },
                DatabaseProvisionedPolicy = new
                {
                    catalog.MaxFiles,
                    catalog.MaxSingleFileBytes,
                    ArtifactTextExtensions = catalog.ArtifactTextExtensions.Order(StringComparer.OrdinalIgnoreCase).ToArray()
                },
                Safety = "Writing/generating source does not execute it. Review execution and direct workspace writes retain their existing human approval gates; build execution is separately gated."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Building the code-generation capability map failed.");
            throw;
        }
    }

}
