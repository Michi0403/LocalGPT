namespace LocalGPT.Services;

public sealed record MinecraftDependencyVersionInfo(
    string Loader,
    string RequestedMinecraftVersion,
    string MatchedMinecraftVersion,
    string JavaVersion,
    string GradleVersion,
    string? FabricLoaderVersion,
    string? FabricApiVersion,
    string? NeoForgeVersion,
    string? PaperApiVersion,
    int? DatapackPackFormat,
    bool IsExactMatch,
    bool NeedsVerification,
    string Notes,
    string Source);

public static class MinecraftDependencyVersionCatalog
{
    private const string DefaultJavaVersion = "21";
    private const string DefaultGradleVersion = "8.14.2";
    private const string FabricLoaderVersion = "0.16.9";

    private static readonly CatalogEntry[] KnownVersions =
    [
        new(
            MinecraftVersion: "1.21.4",
            FabricApiVersion: "0.116.9+1.21.4",
            NeoForgeVersion: "21.1.231",
            PaperApiVersion: "1.21.4-R0.1-SNAPSHOT",
            Notes: "Curated LocalGPT 1.21.4 mapping; NeoForge value is a cautious 1.21.x fallback and should be source-checked before release."),
        new(
            MinecraftVersion: "1.21.1",
            FabricApiVersion: "0.116.9+1.21.1",
            NeoForgeVersion: "21.1.231",
            PaperApiVersion: "1.21.1-R0.1-SNAPSHOT",
            Notes: "Curated LocalGPT 1.21.1 mapping used by Java workspace smoke tests.")
    ];

    public static MinecraftDependencyVersionInfo Resolve(
        string? loader,
        string? minecraftVersion,
        string? javaVersion = null,
        string? gradleVersion = null)
    {
        var normalizedLoader = NormalizeLoader(loader);
        var requestedMinecraftVersion = string.IsNullOrWhiteSpace(minecraftVersion)
            ? "1.21.4"
            : minecraftVersion.Trim();
        var requestedJavaVersion = string.IsNullOrWhiteSpace(javaVersion)
            ? DefaultJavaVersion
            : javaVersion.Trim();
        var requestedGradleVersion = string.IsNullOrWhiteSpace(gradleVersion)
            ? DefaultGradleVersion
            : gradleVersion.Trim();

        var entry = KnownVersions.FirstOrDefault(item =>
            requestedMinecraftVersion.Equals(item.MinecraftVersion, StringComparison.OrdinalIgnoreCase));
        var exact = entry is not null;
        entry ??= KnownVersions
            .Where(item => requestedMinecraftVersion.StartsWith(item.MinecraftVersion, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.MinecraftVersion.Length)
            .FirstOrDefault();
        entry ??= requestedMinecraftVersion.StartsWith("1.21", StringComparison.OrdinalIgnoreCase)
            ? KnownVersions.First(item => item.MinecraftVersion == "1.21.4")
            : KnownVersions.First(item => item.MinecraftVersion == "1.21.1");

        var datapack = MinecraftDatapackVersionCatalog.Resolve(requestedMinecraftVersion);
        var needsVerification = !exact ||
            datapack.NeedsVerification ||
            normalizedLoader.Equals("NeoForge", StringComparison.OrdinalIgnoreCase) && !requestedMinecraftVersion.Equals("1.21.1", StringComparison.OrdinalIgnoreCase);

        var notes = exact
            ? entry.Notes
            : $"No exact curated Java-loader mapping for Minecraft {requestedMinecraftVersion}; using {entry.MinecraftVersion} as a fallback. Verify loader/API versions against official Fabric, NeoForge, and Paper sources before release.";

        return new MinecraftDependencyVersionInfo(
            Loader: normalizedLoader,
            RequestedMinecraftVersion: requestedMinecraftVersion,
            MatchedMinecraftVersion: entry.MinecraftVersion,
            JavaVersion: requestedJavaVersion,
            GradleVersion: requestedGradleVersion,
            FabricLoaderVersion: normalizedLoader is "Fabric" or "NeoForge" ? FabricLoaderVersion : null,
            FabricApiVersion: normalizedLoader is "Fabric" ? entry.FabricApiVersion : null,
            NeoForgeVersion: normalizedLoader is "NeoForge" ? entry.NeoForgeVersion : null,
            PaperApiVersion: normalizedLoader is "Paper" ? entry.PaperApiVersion : null,
            DatapackPackFormat: normalizedLoader is "Datapack" ? datapack.PackFormat : null,
            IsExactMatch: exact && !datapack.NeedsVerification,
            NeedsVerification: needsVerification,
            Notes: notes,
            Source: "LocalGPT curated Minecraft dependency catalog. Verify unknown versions with official Fabric, NeoForge, Paper, Gradle, and Minecraft version sources.");
    }

    private static string NormalizeLoader(string? loader)
    {
        return (loader ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "fabric" => "Fabric",
            "neoforge" or "neo forge" => "NeoForge",
            "paper" or "paper plugin" => "Paper",
            "datapack" or "data pack" or "vanilla datapack" => "Datapack",
            "bedrock" or "bedrock addon" or "bedrock add-on" => "Bedrock",
            _ => "Fabric"
        };
    }

    private sealed record CatalogEntry(
        string MinecraftVersion,
        string FabricApiVersion,
        string NeoForgeVersion,
        string PaperApiVersion,
        string Notes);
}
