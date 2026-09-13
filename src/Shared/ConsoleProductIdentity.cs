using System;
using System.Linq;
using System.Reflection;

namespace ProjectConsoleIdentity;

/// <summary>
/// Resolves the visible startup identity of an executable from generated assembly metadata.
/// </summary>
internal static class ConsoleProductIdentity
{
    /// <summary>
    /// Returns the operator-visible semantic version from the running entry assembly while removing optional informational build metadata so console identity remains stable across release packaging.
    /// </summary>
    /// <value>The informational version without build metadata, or the assembly version when no informational version is available.</value>
    public static string Version
    {
        get
        {
            var assembly = ResolveAssembly();
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                var buildMetadataIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
                return buildMetadataIndex > 0 ? informationalVersion[..buildMetadataIndex] : informationalVersion;
            }

            var version = assembly.GetName().Version;
            return version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    /// <summary>
    /// Gets the repository URL emitted from the project RepositoryUrl MSBuild property.
    /// </summary>
    /// <value>The canonical repository URL stored in assembly metadata.</value>
    public static string RepositoryUrl => ResolveMetadata("RepositoryUrl");

    /// <summary>
    /// Gets the repository owner/name slug derived from the metadata-backed repository URL.
    /// </summary>
    /// <value>A GitHub-style owner/name slug.</value>
    public static string RepositorySlug
    {
        get
        {
            if (!Uri.TryCreate(RepositoryUrl, UriKind.Absolute, out var repositoryUri))
                throw new InvalidOperationException("The project RepositoryUrl assembly metadata is not an absolute URL.");

            var segments = repositoryUri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
                throw new InvalidOperationException("The project RepositoryUrl assembly metadata does not contain an owner/name path.");

            return $"{segments[0]}/{segments[1]}";
        }
    }

    /// <summary>
    /// Writes the product, version, repository, owner, and license identity before normal startup diagnostics.
    /// </summary>
    public static void WriteStartupHeader()
    {
        var assembly = ResolveAssembly();
        var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        if (string.IsNullOrWhiteSpace(product))
            product = assembly.GetName().Name ?? "Application";

        Console.WriteLine($"{product} {Version}");
        Console.WriteLine($"Repository: {RepositoryUrl}");
        Console.WriteLine($"Owner: {ResolveMetadata("Owner")}");
        Console.WriteLine($"License: {ResolveMetadata("License")}");
        Console.WriteLine();
    }

    /// <summary>
    /// Reads one required value emitted from MSBuild project metadata and fails explicitly when packaging omitted it, preventing the console banner from silently drifting to invented fallback identity.
    /// </summary>
    /// <param name="key">Metadata key to retrieve.</param>
    /// <returns>The non-empty metadata value.</returns>
    private static string ResolveMetadata(string key)
    {
        var value = ResolveAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, key, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Required project assembly metadata '{key}' is missing.");

        return value;
    }

    /// <summary>
    /// Resolves the running entry assembly, falling back to the assembly containing this helper for hosted test contexts.
    /// </summary>
    /// <returns>The assembly whose project metadata describes the running executable.</returns>
    private static Assembly ResolveAssembly() => Assembly.GetEntryAssembly() ?? typeof(ConsoleProductIdentity).Assembly;
}
