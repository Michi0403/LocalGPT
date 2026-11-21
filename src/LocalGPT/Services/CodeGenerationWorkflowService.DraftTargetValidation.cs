using System.Xml;
using System.Xml.Linq;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Checks generated project declarations against the explicitly requested .NET framework.</summary>
public sealed partial class CodeGenerationWorkflowService
{
    /// <summary>Identifies an inspection that proposes a forbidden replacement for a single explicit framework.</summary>
    /// <param name="goal">Persisted original goal.</param>
    /// <param name="diagnostic">Untrusted reviewer diagnostic.</param>
    /// <returns>True when the review proposes a conflicting framework.</returns>
    private bool IsContradictoryFrameworkInspection(string goal, string diagnostic)
    {
        var requested = runtimePolicy.GetPattern("runtime.codegen.explicit-dotnet-target").Matches(goal)
            .Select(match => "net" + match.Groups["major"].Value + "." +
                (match.Groups["minor"].Success ? match.Groups["minor"].Value : "0"))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (requested.Count != 1) return false;
        return runtimePolicy.GetPattern("runtime.codegen.review-target-replacement").Matches(diagnostic)
            .Select(match => "net" + match.Groups["major"].Value + "." +
                (match.Groups["minor"].Success ? match.Groups["minor"].Value : "0"))
            .Any(target => !string.Equals(target, requested[0], StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Rejects a generated project that silently changes an unambiguous user-specified .NET target.</summary>
    /// <returns>A concrete target/XML diagnostic or an empty value when this check does not find a defect.</returns>
    /// <summary>
    /// Checks a greenfield manifest layout from its paths alone, before any model authors content.
    /// </summary>
    /// <param name="manifest">Normalized relative paths of the requested draft.</param>
    /// <returns>A correction for the author, or an empty value when the layout is consistent.</returns>
    private string ValidateManifestLayout(IReadOnlyDictionary<string, string> manifest)
    {
        var paths = manifest.Keys.Select(path => path.Replace('\\', '/')).ToList();
        var projectDirectories = paths
            .Where(path => string.Equals(Path.GetExtension(path), ".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(path => (Path.GetDirectoryName(path) ?? string.Empty).Replace('\\', '/').TrimEnd('/'))
            .ToList();
        if (projectDirectories.Count == 0) return string.Empty; // Existing-repository edits and non-project drafts are not layout-checked here.
        var hasRazor = paths.Any(path => string.Equals(Path.GetExtension(path), ".razor", StringComparison.OrdinalIgnoreCase));
        var hasRootComponent = paths.Any(path => string.Equals(Path.GetFileName(path), "App.razor", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileName(path), "_Imports.razor", StringComparison.OrdinalIgnoreCase));
        if (hasRazor && !hasRootComponent) return runtimePolicy.GetString("CouncilManifestRazorRootMissingNotice");
        foreach (var path in paths)
        {
            var isWwwroot = path.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/wwwroot/", StringComparison.OrdinalIgnoreCase);
            if (!isWwwroot) continue;
            if (!projectDirectories.Any(directory => directory.Length == 0 || path.StartsWith(directory + "/", StringComparison.OrdinalIgnoreCase)))
                return runtimePolicy.GetString("CouncilManifestWwwrootOutsideProjectNotice");
        }
        return string.Empty;
    }

    private string ValidateDraftProjectTarget(string goal, string path, string source)
    {
        try
        {
            if (!new[] { ".csproj", ".fsproj", ".vbproj", ".props", ".targets" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) return string.Empty;
            var targets = runtimePolicy.GetPattern("runtime.codegen.explicit-dotnet-target").Matches(goal)
                .Select(match => "net" + match.Groups["major"].Value + "." +
                    (match.Groups["minor"].Success ? match.Groups["minor"].Value : "0"))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            using var text = new StringReader(source);
            using var reader = XmlReader.Create(text, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            var project = XDocument.Load(reader);
            if (runtimePolicy.GetPattern("runtime.codegen.explicit-no-devexpress").IsMatch(goal) &&
                project.Descendants().Where(element => element.Name.LocalName is "PackageReference" or "Reference" or "PackageVersion")
                    .Any(element => new[] { (string?)element.Attribute("Include"), (string?)element.Attribute("Update") }
                        .Any(value => value?.StartsWith("DevExpress", StringComparison.OrdinalIgnoreCase) == true)))
                return runtimePolicy.GetString("CouncilFileWorkForbiddenDevExpressNotice");
            if (targets.Count != 1) return string.Empty; // Ambiguous/multitarget requests still use the normal review and build.
            var declared = project.Descendants().Where(element => element.Name.LocalName is "TargetFramework" or "TargetFrameworks")
                .SelectMany(element => element.Value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).ToList();
            // Dynamic MSBuild declarations still need the compiler, but cannot hide a contradictory literal.
            declared = declared.Where(value => !value.Contains("$(", StringComparison.Ordinal)).ToList();
            return declared.All(value => string.Equals(value, targets[0], StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith(targets[0] + "-", StringComparison.OrdinalIgnoreCase)) ? string.Empty :
                $"Requested target {targets[0]}; generated project declares {string.Join("; ", declared)}.";
        }
        catch (XmlException exception)
        {
            logger.LogDebug("Generated project XML was invalid at line {Line}, position {Position}.", exception.LineNumber, exception.LinePosition);
            return $"Project XML is invalid at line {exception.LineNumber}, position {exception.LinePosition}.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Source draft project target validation failed.");
            throw;
        }
    }
}
