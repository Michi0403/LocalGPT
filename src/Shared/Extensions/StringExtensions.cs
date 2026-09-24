using System;

namespace ProjectConsoleIdentity.Extensions;

/// <summary>
/// Provides the small, deterministic string transformations used when materializing shared console product identity.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Removes optional informational-version build metadata while preserving the semantic version visible to operators.
    /// </summary>
    /// <param name="value">Informational version to normalize.</param>
    /// <returns>The version text before an optional <c>+</c> build-metadata suffix.</returns>
    internal static string WithoutBuildMetadata(this string value)
    {
        var buildMetadataIndex = value.IndexOf('+', StringComparison.Ordinal);
        return buildMetadataIndex > 0 ? value[..buildMetadataIndex] : value;
    }

    /// <summary>
    /// Converts an absolute repository URL into its owner/name slug.
    /// </summary>
    /// <param name="repositoryUrl">Repository URL supplied by assembly metadata.</param>
    /// <returns>The owner/name repository slug.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the URL is not absolute or does not contain owner/name path segments.</exception>
    internal static string ToRepositorySlug(this string repositoryUrl)
    {
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var repositoryUri))
            throw new InvalidOperationException("The project RepositoryUrl assembly metadata is not an absolute URL.");

        var segments = repositoryUri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            throw new InvalidOperationException("The project RepositoryUrl assembly metadata does not contain an owner/name path.");

        return $"{segments[0]}/{segments[1]}";
    }
}
