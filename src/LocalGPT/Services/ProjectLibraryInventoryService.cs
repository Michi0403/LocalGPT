using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    public partial class ProjectLibraryInventoryService(ILogger<ProjectLibraryInventoryService> logger,
        CouncilRuntimeService councilRuntime) : IProjectLibraryInventoryService
    {
        public async Task<string> BuildDevExpressBriefingAsync(CancellationToken cancellationToken = default)
        {
    try
    {
                var root = councilRuntime.FindRepositoryRoot(logger);
                var builder = new StringBuilder()
                    .AppendLine("DevExpress package and capability inventory for LocalGPT:");

                var wroteSourceInventory = false;
                if (root is not null)
                {
                    wroteSourceInventory = await AppendProjectPackageReferencesAsync(builder, root, cancellationToken, logger).ConfigureAwait(false);
                    await councilRuntime.AppendDevExpressImportsAsync(builder, root, cancellationToken, logger).ConfigureAwait(false);
                    await councilRuntime.AppendDevExpressRegistrationsAsync(builder, root, cancellationToken, logger).ConfigureAwait(false);
                }

                councilRuntime.AppendLoadedDevExpressAssemblies(builder, logger);
                if (!wroteSourceInventory)
                    builder.AppendLine("- Source `LocalGPT.csproj` was not found from this runtime location; use loaded assemblies and copied AI docs as fallback evidence.");

                builder
                    .AppendLine("- Rule: do not invent DevExpress components or APIs beyond the referenced package/version family. Mark uncertain APIs as Needs verification.")
                    .AppendLine("- Rule: Office document generation, report generation, PDF export, and file download workflows belong in the ASP.NET Core/Blazor server backend with service methods and safe download endpoints. The frontend should call those services and display links/status.");

                return builder.ToString().Trim();
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectLibraryInventoryService)}.{nameof(BuildDevExpressBriefingAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectLibraryInventoryService)}.{nameof(BuildDevExpressBriefingAsync)} failed.");
        throw;
    }
}

        private async Task<bool> AppendProjectPackageReferencesAsync(StringBuilder builder, string root, CancellationToken cancellationToken, ILogger logger)
        {
            var projectPath = Path.Combine(root, "src", "LocalGPT", "LocalGPT.csproj");
            if (!File.Exists(projectPath))
                return false;

            try
            {
                await using var stream = File.OpenRead(projectPath);
                var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
                var packages = document
                    .Descendants()
                    .Where(element => element.Name.LocalName == "PackageReference")
                    .Select(element => new
                    {
                        Include = (string?)element.Attribute("Include") ?? string.Empty,
                        Version = (string?)element.Attribute("Version") ?? element.Elements().FirstOrDefault(child => child.Name.LocalName == "Version")?.Value ?? string.Empty
                    })
                    .Where(package => package.Include.StartsWith("DevExpress.", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(package => package.Include)
                    .ToList();

                if (packages.Count == 0)
                    return false;

                builder.AppendLine("- Referenced DevExpress NuGet packages:");
                foreach (var package in packages)
                    builder.AppendLine($"  - {package.Include} {package.Version}");

                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not read DevExpress package references from {ProjectPath}.", projectPath);
                builder.AppendLine($"- Could not read `LocalGPT.csproj`: {ex.Message}");
                return false;
            }
        }

    }
}
