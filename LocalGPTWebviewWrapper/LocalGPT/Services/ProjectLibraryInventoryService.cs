using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    public partial class ProjectLibraryInventoryService(ILogger<ProjectLibraryInventoryService> logger) : IProjectLibraryInventoryService
    {
        public async Task<string> BuildDevExpressBriefingAsync(CancellationToken cancellationToken = default)
        {
            var root = FindRepositoryRoot();
            var builder = new StringBuilder()
                .AppendLine("DevExpress package and capability inventory for LocalGPT:");

            var wroteSourceInventory = false;
            if (root is not null)
            {
                wroteSourceInventory = await AppendProjectPackageReferencesAsync(builder, root, cancellationToken);
                await AppendDevExpressImportsAsync(builder, root, cancellationToken);
                await AppendDevExpressRegistrationsAsync(builder, root, cancellationToken);
            }

            AppendLoadedDevExpressAssemblies(builder);
            if (!wroteSourceInventory)
                builder.AppendLine("- Source `LocalGPT.csproj` was not found from this runtime location; use loaded assemblies and copied AI docs as fallback evidence.");

            builder
                .AppendLine("- Rule: do not invent DevExpress components or APIs beyond the referenced package/version family. Mark uncertain APIs as Needs verification.")
                .AppendLine("- Rule: Office document generation, report generation, PDF export, and file download workflows belong in the ASP.NET Core/Blazor server backend with service methods and safe download endpoints. The frontend should call those services and display links/status.");

            return builder.ToString().Trim();
        }

        private async Task<bool> AppendProjectPackageReferencesAsync(StringBuilder builder, string root, CancellationToken cancellationToken)
        {
            var projectPath = Path.Combine(root, "LocalGPTWebviewWrapper", "LocalGPT", "LocalGPT.csproj");
            if (!File.Exists(projectPath))
                return false;

            try
            {
                await using var stream = File.OpenRead(projectPath);
                var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
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

        private static async Task AppendDevExpressImportsAsync(StringBuilder builder, string root, CancellationToken cancellationToken)
        {
            var importsPath = Path.Combine(root, "LocalGPTWebviewWrapper", "LocalGPT", "Components", "_Imports.razor");
            if (!File.Exists(importsPath))
                return;

            var text = await File.ReadAllTextAsync(importsPath, cancellationToken);
            var imports = DevExpressImportPattern()
                .Matches(text)
                .Select(match => match.Groups["namespace"].Value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList();

            if (imports.Count == 0)
                return;

            builder.AppendLine("- Imported DevExpress namespaces in Blazor:");
            foreach (var item in imports)
                builder.AppendLine($"  - {item}");
        }

        private static async Task AppendDevExpressRegistrationsAsync(StringBuilder builder, string root, CancellationToken cancellationToken)
        {
            var programPath = Path.Combine(root, "LocalGPTWebviewWrapper", "LocalGPT", "Program.cs");
            if (!File.Exists(programPath))
                return;

            var text = await File.ReadAllTextAsync(programPath, cancellationToken);
            var registrations = DevExpressRegistrationPattern()
                .Matches(text)
                .Select(match => match.Value.TrimEnd('('))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList();

            if (registrations.Count == 0)
                return;

            builder.AppendLine("- DevExpress services registered in ASP.NET Core:");
            foreach (var registration in registrations)
                builder.AppendLine($"  - {registration}");
        }

        private static void AppendLoadedDevExpressAssemblies(StringBuilder builder)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetName())
                .Where(name => name.Name?.StartsWith("DevExpress.", StringComparison.OrdinalIgnoreCase) == true)
                .OrderBy(name => name.Name)
                .Take(30)
                .ToList();

            if (assemblies.Count == 0)
                return;

            builder.AppendLine("- Loaded DevExpress assemblies:");
            foreach (var assembly in assemblies)
                builder.AppendLine($"  - {assembly.Name} {assembly.Version}");
        }

        private static string? FindRepositoryRoot()
        {
            foreach (var start in new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            })
            {
                var directory = new DirectoryInfo(start);
                while (directory is not null)
                {
                    if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) ||
                        Directory.Exists(Path.Combine(directory.FullName, ".git")))
                    {
                        return directory.FullName;
                    }

                    directory = directory.Parent;
                }
            }

            return null;
        }

        [GeneratedRegex("^\\s*@using\\s+(?<namespace>DevExpress(?:\\.[A-Za-z0-9_]+)+)", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
        private static partial Regex DevExpressImportPattern();

        [GeneratedRegex("AddDevExpress[A-Za-z0-9_]*\\(", RegexOptions.CultureInvariant)]
        private static partial Regex DevExpressRegistrationPattern();
    }
}
