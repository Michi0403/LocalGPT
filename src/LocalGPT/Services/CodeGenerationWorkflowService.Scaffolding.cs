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
    /// Performs scaffold output as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="workspaceRoot">Workspace root value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="output">Output value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="reviewedSources">Reviewed source artifact dependency used by the code generation workflow workflow to provide the corresponding application capability.</param>
    /// <param name="writtenFiles">Written files value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="buildTargets">Build targets value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ScaffoldOutputAsync(
        string workspaceRoot,
        CodeGenerationOutputSpec output,
        IReadOnlyList<ReviewedSourceArtifact> reviewedSources,
        List<string> writtenFiles,
        List<string> buildTargets,
        CancellationToken cancellationToken)
    {
    try
    {
            var outputRoot = ResolveInsideRoot(workspaceRoot, output.RelativeDirectory);
            Directory.CreateDirectory(outputRoot);

            switch (output.Kind)
            {
                case CodeGenerationOutputKinds.SourceFiles:
                    return;

                case CodeGenerationOutputKinds.CSharpScript:
                {
                    var copied = await CopyReviewedSourcesAsync(
                        workspaceRoot,
                        outputRoot,
                        reviewedSources.Where(source => Path.GetExtension(source.RelativePath).Equals(".csx", StringComparison.OrdinalIgnoreCase)),
                        writtenFiles,
                        cancellationToken).ConfigureAwait(false);
                    if (!copied)
                    {
                        var fileName = $"{output.Name}.csx";
                        var path = Path.Combine(outputRoot, fileName);
                        if (!File.Exists(path))
                        {
                            await File.WriteAllTextAsync(path,
                                $"// Reviewed C# script source. LocalGPT does not execute this file automatically.{Environment.NewLine}Console.WriteLine(\"{EscapeCSharp(output.Description)}\");{Environment.NewLine}",
                                cancellationToken).ConfigureAwait(false);
                            writtenFiles.Add(Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/'));
                        }
                    }
                    return;
                }

                case CodeGenerationOutputKinds.PowerShellScript:
                {
                    var copied = await CopyReviewedSourcesAsync(
                        workspaceRoot,
                        outputRoot,
                        reviewedSources.Where(source => Path.GetExtension(source.RelativePath).Equals(".ps1", StringComparison.OrdinalIgnoreCase)),
                        writtenFiles,
                        cancellationToken).ConfigureAwait(false);
                    if (!copied)
                    {
                        var fileName = $"{output.Name}.ps1";
                        var path = Path.Combine(outputRoot, fileName);
                        if (!File.Exists(path))
                        {
                            await File.WriteAllTextAsync(path,
                                $"# Reviewed PowerShell source. LocalGPT writes this file but never executes it automatically.{Environment.NewLine}Write-Output {JsonSerializer.Serialize(output.Description)}{Environment.NewLine}",
                                cancellationToken).ConfigureAwait(false);
                            writtenFiles.Add(Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/'));
                        }
                    }
                    return;
                }

                case CodeGenerationOutputKinds.JavaScriptModule:
                {
                    var copied = await CopyReviewedSourcesAsync(
                        workspaceRoot,
                        outputRoot,
                        reviewedSources.Where(source => Path.GetExtension(source.RelativePath).Equals(".js", StringComparison.OrdinalIgnoreCase)),
                        writtenFiles,
                        cancellationToken).ConfigureAwait(false);
                    if (!copied)
                    {
                        var fileName = $"{output.Name}.js";
                        var path = Path.Combine(outputRoot, fileName);
                        if (!File.Exists(path))
                        {
                            await File.WriteAllTextAsync(path,
                                $"// Reviewed JavaScript module source. LocalGPT does not execute this file automatically.{Environment.NewLine}export function describe() {{ return {JsonSerializer.Serialize(output.Description)}; }}{Environment.NewLine}",
                                cancellationToken).ConfigureAwait(false);
                            writtenFiles.Add(Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/'));
                        }
                    }
                    return;
                }

                case CodeGenerationOutputKinds.ClassLibrary:
                case CodeGenerationOutputKinds.ConsoleApplication:
                case CodeGenerationOutputKinds.LocalGptAddon:
                case CodeGenerationOutputKinds.Solution:
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported output kind {output.Kind}.");
            }

            var projectRoot = output.Kind == CodeGenerationOutputKinds.Solution
                ? Path.Combine(outputRoot, output.Name)
                : outputRoot;
            Directory.CreateDirectory(projectRoot);
            var projectPath = Path.Combine(projectRoot, $"{output.Name}.csproj");
            var outputType = output.Kind == CodeGenerationOutputKinds.ConsoleApplication ? "Exe" : "Library";
            var projectXml = $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{{output.TargetFramework}}</TargetFramework>
                <OutputType>{{outputType}}</OutputType>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RootNamespace>{{output.RootNamespace}}</RootNamespace>
                <AssemblyName>{{output.Name}}</AssemblyName>
                <Deterministic>true</Deterministic>
                <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
              </PropertyGroup>
            </Project>
            """;
            await File.WriteAllTextAsync(projectPath, projectXml, cancellationToken).ConfigureAwait(false);
            writtenFiles.Add(Path.GetRelativePath(workspaceRoot, projectPath).Replace('\\', '/'));
            buildTargets.Add(projectPath);

            await CopyReviewedSourcesAsync(
                workspaceRoot,
                Path.Combine(projectRoot, "ReviewedSources"),
                reviewedSources.Where(source =>
                    Path.GetExtension(source.RelativePath).Equals(".cs", StringComparison.OrdinalIgnoreCase) &&
                    !IsInsideDirectory(source.FullPath, projectRoot)),
                writtenFiles,
                cancellationToken).ConfigureAwait(false);

            var existingCs = Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories).Any();
            if (!existingCs)
            {
                var sourcePath = Path.Combine(projectRoot, output.Kind == CodeGenerationOutputKinds.ConsoleApplication ? "Program.cs" : "GeneratedFeature.cs");
                var source = output.Kind switch
                {
                    CodeGenerationOutputKinds.ConsoleApplication =>
                        $"Console.WriteLine(\"{EscapeCSharp(output.Description)}\");{Environment.NewLine}",
                    CodeGenerationOutputKinds.LocalGptAddon => BuildAddonSource(output),
                    _ => BuildLibrarySource(output)
                };
                await File.WriteAllTextAsync(sourcePath, source, cancellationToken).ConfigureAwait(false);
                writtenFiles.Add(Path.GetRelativePath(workspaceRoot, sourcePath).Replace('\\', '/'));
            }

            if (output.Kind == CodeGenerationOutputKinds.LocalGptAddon)
            {
                var manifestPath = Path.Combine(projectRoot, "localgpt-addon.json");
                await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(new
                {
                    id = output.Name,
                    displayName = output.Name,
                    version = "0.1.0",
                    entryType = $"{output.RootNamespace}.LocalGptAddon",
                    approved = false,
                    autoLoad = false,
                    description = output.Description,
                    safety = "Generated addon binaries are never loaded automatically. Review and approve the exact assembly before registration."
                }, new JsonSerializerOptions { WriteIndented = true }), cancellationToken).ConfigureAwait(false);
                writtenFiles.Add(Path.GetRelativePath(workspaceRoot, manifestPath).Replace('\\', '/'));
            }

            if (output.Kind == CodeGenerationOutputKinds.Solution)
            {
                var solutionPath = Path.Combine(outputRoot, $"{output.Name}.sln");
                await File.WriteAllTextAsync(solutionPath, BuildSolutionFile(output.Name, Path.GetRelativePath(outputRoot, projectPath)), cancellationToken).ConfigureAwait(false);
                writtenFiles.Add(Path.GetRelativePath(workspaceRoot, solutionPath).Replace('\\', '/'));
                buildTargets.Remove(projectPath);
                buildTargets.Add(solutionPath);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ScaffoldOutputAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ScaffoldOutputAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs copy reviewed sources as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="workspaceRoot">Workspace root value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="destinationRoot">Destination root value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="sources">Reviewed source artifact dependency used by the code generation workflow workflow to provide the corresponding application capability.</param>
    /// <param name="writtenFiles">Written files value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private async Task<bool> CopyReviewedSourcesAsync(
        string workspaceRoot,
        string destinationRoot,
        IEnumerable<ReviewedSourceArtifact> sources,
        List<string> writtenFiles,
        CancellationToken cancellationToken)
    {
    try
    {
            var copiedAny = false;
            foreach (var source in sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = NormalizeRelativePath(source.RelativePath);
                var destinationPath = ResolveInsideRoot(destinationRoot, relativePath);
                if (IsInsideDirectory(source.FullPath, destinationRoot))
                {
                    copiedAny = true;
                    continue;
                }

                if (platform.PathsEqual(source.FullPath, destinationPath))
                {
                    copiedAny = true;
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? destinationRoot);
                var sourceStream = new FileStream(source.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
                await using var configuredSourceStreamAsyncDisposal = sourceStream.ConfigureAwait(false);
                var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                await using var configuredDestinationStreamAsyncDisposal = destinationStream.ConfigureAwait(false);
                await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
                var packagedRelativePath = Path.GetRelativePath(workspaceRoot, destinationPath).Replace('\\', '/');
                if (!writtenFiles.Contains(packagedRelativePath, StringComparer.OrdinalIgnoreCase))
                    writtenFiles.Add(packagedRelativePath);
                copiedAny = true;
                logger.LogDebug("Copied reviewed source {SourcePath} into generated output path {OutputPath}.", relativePath, packagedRelativePath);
            }

            return copiedAny;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(CopyReviewedSourcesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(CopyReviewedSourcesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds library source as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="output">Output value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildLibrarySource(CodeGenerationOutputSpec output) {
    try
    {
        return $$"""
    namespace {{output.RootNamespace}};

    public sealed class GeneratedFeature
    {
        public string Describe() => "{{EscapeCSharp(output.Description)}}";
    }
    """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildLibrarySource)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildLibrarySource)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds addon source as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="output">Output value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildAddonSource(CodeGenerationOutputSpec output) {
    try
    {
        return $$"""
    namespace {{output.RootNamespace}};

    public interface ILocalGptAddon
    {
        string Id { get; }
        string Describe();
    }

    public sealed class LocalGptAddon : ILocalGptAddon
    {
        public string Id => "{{EscapeCSharp(output.Name)}}";
        public string Describe() => "{{EscapeCSharp(output.Description)}}";
    }
    """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildAddonSource)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildAddonSource)} failed.");
        throw;
    }
}

    }
}
