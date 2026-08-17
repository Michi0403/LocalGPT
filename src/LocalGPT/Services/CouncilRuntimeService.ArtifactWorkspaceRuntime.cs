using DevExpress.CodeParser;
using DevExpress.Xpo;
using DevExpress.XtraCharts;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council runtime behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilRuntimeService
    {
        /// <summary>Executes the get request base url operation.</summary>
        /// <param name="httpContext">Input value for httpContext.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string GetRequestBaseUrl(HttpContext httpContext, ILogger logger)
        {
            try
            {
                var request = httpContext.Request;
                return $"{request.Scheme}://{request.Host}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GetRequestBaseUrl");
                return string.Empty;
            }
        }


        /// <summary>Executes the enumerate artifact workspaces operation.</summary>
        /// <param name="artifactRoot">Input value for artifactRoot.</param>
        /// <param name="take">Input value for take.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<ArtifactWorkspaceSummary> EnumerateArtifactWorkspaces(string artifactRoot, int take, ILogger logger)
        {
            try
            {
                if (!Directory.Exists(artifactRoot))
                    return [];

                return Directory
                    .EnumerateDirectories(artifactRoot)
                    .Select(path => BuildArtifactWorkspaceSummary(artifactRoot, path, logger))
                    .Where(summary => summary is not null)
                    .Cast<ArtifactWorkspaceSummary>()
                    .OrderByDescending(summary => summary.LastWriteTimeUtc)
                    .Take(ResolveArtifactEnumerationTake(take))
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"EnumerateArtifactWorkspaces artifactRoot {artifactRoot.ToString()} take {take.ToString()}");
                return new List<ArtifactWorkspaceSummary>();
            }
        }
        /// <summary>Executes the build artifact workspace summary operation.</summary>
        /// <param name="artifactRoot">Input value for artifactRoot.</param>
        /// <param name="workspacePath">Input value for workspacePath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public ArtifactWorkspaceSummary? BuildArtifactWorkspaceSummary(string artifactRoot, string workspacePath, ILogger logger)
        {
            try
            {
                var directory = new DirectoryInfo(workspacePath);
                var files = EnumerateWorkspaceTextFiles(workspacePath, catalog.MaxFiles, logger);
                var zipNames = Directory
                    .EnumerateFiles(artifactRoot, "*.zip", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Where(name => name!.StartsWith(directory.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(name => name!)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new ArtifactWorkspaceSummary(
                    directory.Name,
                    directory.FullName,
                    directory.LastWriteTimeUtc,
                    files.Count,
                    files.Count(file => file.RelativePath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)),
                    files.Count(file => file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)),
                    zipNames);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"BuildArtifactWorkspaceSummary artifactRoot {artifactRoot.ToString()} workspacePath {workspacePath.ToString()}");
                return null;
            }

        }
        /// <summary>Executes the enumerate workspace text files operation.</summary>
        /// <param name="workspaceRoot">Input value for workspaceRoot.</param>
        /// <param name="take">Input value for take.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public List<ArtifactWorkspaceFileSummary> EnumerateWorkspaceTextFiles(string workspaceRoot, int take, ILogger logger)
        {
            try
            {
                 if (!Directory.Exists(workspaceRoot))
                return [];

            return Directory
                .EnumerateFiles(workspaceRoot, "*", SearchOption.AllDirectories)
                .Where(file => IsSupportedArtifactTextFile(file,logger))
                .Select(path =>
                {
                    var info = new FileInfo(path);
                    return new ArtifactWorkspaceFileSummary(
                        text.ToForwardSlash(Path.GetRelativePath(workspaceRoot, path),logger),
                        info.Length,
                        info.LastWriteTimeUtc);
                })
                .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Take(ResolveArtifactEnumerationTake(take))
                .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"EnumerateWorkspaceTextFiles workspaceRoot {workspaceRoot.ToString()} take {take.ToString()}");
                return new();
            }
           
        }
        /// <summary>
        /// Resolves an artifact enumeration take value against the database-backed MaxFiles policy instead of source-code list ceilings.
        /// </summary>
        /// <param name="take">Caller-requested maximum; non-positive values use the configured policy.</param>
        /// <returns>The effective enumeration count.</returns>
        private int ResolveArtifactEnumerationTake(int take)
        {
            try
            {
                var configuredMaximum = Math.Max(1, catalog.MaxFiles);
                return take > 0 ? Math.Min(take, configuredMaximum) : configuredMaximum;
            }
            catch (Exception ex)
            {
                serviceLogger.LogError(ex, "Resolving the artifact enumeration policy failed.");
                throw;
            }
        }

        /// <summary>Executes the resolve artifact workspace operation.</summary>
        /// <param name="artifactRoot">Input value for artifactRoot.</param>
        /// <param name="workspaceName">Input value for workspaceName.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string? ResolveArtifactWorkspace(string artifactRoot, string workspaceName, ILogger logger)
        {
            try
            {
                var safeName = Path.GetFileName(workspaceName);
                if (!string.Equals(workspaceName, safeName, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(safeName))
                {
                    return null;
                }

                var root = Path.GetFullPath(artifactRoot);
                var path = Path.GetFullPath(Path.Combine(root, safeName));
                if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                    !Directory.Exists(path))
                {
                    return null;
                }

                return path;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"ResolveWorkspaceTextFile artifactRoot {artifactRoot.ToString()} workspaceName {workspaceName.ToString()}");
                return null;
            }
        }
        /// <summary>Executes the resolve workspace text file operation.</summary>
        /// <param name="workspaceRoot">Input value for workspaceRoot.</param>
        /// <param name="relativePath">Input value for relativePath.</param>
        /// <param name="allowMissing">Input value for allowMissing.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string? ResolveWorkspaceTextFile(string workspaceRoot, string relativePath, bool allowMissing, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                    return null;

                var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(normalizedRelativePath))
                    return null;

                var root = Path.GetFullPath(workspaceRoot);
                var path = Path.GetFullPath(Path.Combine(root, normalizedRelativePath));
                if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                    !IsSupportedArtifactTextFile(path, logger))
                {
                    return null;
                }

                return allowMissing || System.IO.File.Exists(path) ? path : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"ResolveWorkspaceTextFile workspaceRoot {workspaceRoot.ToString()} relativePath {relativePath.ToString()} allowMissing {allowMissing.ToString()}");
                return null;
            }
        }

        /// <summary>Executes the is supported artifact text file operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool IsSupportedArtifactTextFile(string path, ILogger logger)
        {
            try
            {
                var extension = Path.GetExtension(path);
                return catalog.ArtifactTextExtensions.Contains(extension);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"IsSupportedArtifactTextFile path {path.ToString()}");
                return false;
            }
        }

    }
}
