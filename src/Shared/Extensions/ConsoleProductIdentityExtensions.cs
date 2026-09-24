using System;
using System.Linq;
using System.Reflection;
using ProjectConsoleIdentity.BusinessObjects;

namespace ProjectConsoleIdentity.Extensions;

/// <summary>
/// Materializes immutable console identity from assembly metadata and formats that state for startup output without introducing mutable global runtime state.
/// </summary>
internal static class ConsoleProductIdentityExtensions
{
    /// <summary>
    /// Resolves the product identity described by one executable assembly.
    /// </summary>
    /// <param name="assembly">Assembly whose generated project metadata owns the identity.</param>
    /// <returns>An immutable business object containing the resolved identity.</returns>
    internal static ConsoleProductIdentity ToConsoleProductIdentity(this Assembly assembly)
    {
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = !string.IsNullOrWhiteSpace(informationalVersion)
            ? informationalVersion.WithoutBuildMetadata()
            : ToSemanticVersion(assembly.GetName().Version);

        var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        if (string.IsNullOrWhiteSpace(product))
            product = assembly.GetName().Name ?? "Application";

        var repositoryUrl = ResolveMetadata(assembly, "RepositoryUrl");
        return new ConsoleProductIdentity(
            product,
            version,
            repositoryUrl,
            repositoryUrl.ToRepositorySlug(),
            ResolveMetadata(assembly, "Owner"),
            ResolveMetadata(assembly, "License"));
    }

    /// <summary>
    /// Writes one already-resolved identity to the startup console before normal application diagnostics begin.
    /// </summary>
    /// <param name="identity">Identity business object to present.</param>
    internal static void WriteStartupHeader(this ConsoleProductIdentity identity)
    {
        Console.WriteLine($"{identity.Product} {identity.Version}");
        Console.WriteLine($"Repository: {identity.RepositoryUrl}");
        Console.WriteLine($"Owner: {identity.Owner}");
        Console.WriteLine($"License: {identity.License}");
        Console.WriteLine();
    }

    /// <summary>
    /// Reads one required project metadata value without inventing fallback repository identity.
    /// </summary>
    private static string ResolveMetadata(Assembly assembly, string key)
    {
        var value = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, key, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Required project assembly metadata '{key}' is missing.");

        return value;
    }

    /// <summary>
    /// Converts an assembly version into the three-slot semantic identity used by LocalGPT release packaging.
    /// </summary>
    private static string ToSemanticVersion(Version? version) =>
        version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
}
