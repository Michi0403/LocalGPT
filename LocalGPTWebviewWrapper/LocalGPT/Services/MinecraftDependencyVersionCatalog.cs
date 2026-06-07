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
    string? DatapackPackFormat,
    bool IsExactMatch,
    bool NeedsVerification,
    string Notes,
    string Source);

public static class MinecraftDependencyVersionCatalog
{
    private const string DefaultJavaVersion = "25";
    private const string DefaultGradleVersion = "8.14.2";
    private const string FabricLoaderVersion = "0.16.9";

    private static readonly CatalogEntry[] KnownVersions =
    [
        new(
            MinecraftVersion: "26.1.2",
            FabricApiVersion: null,
            NeoForgeVersion: null,
            PaperApiVersion: null,
            JavaVersion: "25",
            Notes: "Curated LocalGPT datapack-first mapping for the Minecraft Java 26.1 stable family. Java mods/plugins need official Fabric, NeoForge, or Paper version checks before release."),
        new(
            MinecraftVersion: "26.1.1",
            FabricApiVersion: null,
            NeoForgeVersion: null,
            PaperApiVersion: null,
            JavaVersion: "25",
            Notes: "Curated LocalGPT datapack-first mapping for the Minecraft Java 26.1 stable family. Java mods/plugins need official Fabric, NeoForge, or Paper version checks before release."),
        new(
            MinecraftVersion: "26.1",
            FabricApiVersion: null,
            NeoForgeVersion: null,
            PaperApiVersion: null,
            JavaVersion: "25",
            Notes: "Curated LocalGPT datapack-first mapping for Minecraft Java 26.1. Java mods/plugins need official Fabric, NeoForge, or Paper version checks before release."),
        new(
            MinecraftVersion: "26.2-snapshot-6",
            FabricApiVersion: null,
            NeoForgeVersion: null,
            PaperApiVersion: null,
            JavaVersion: "25",
            Notes: "Curated LocalGPT snapshot datapack mapping for Minecraft Java 26.2 Snapshot 6. Use only for snapshot worlds and verify loader APIs before Java mod/plugin release."),
        new(
            MinecraftVersion: "26.2",
            FabricApiVersion: null,
            NeoForgeVersion: null,
            PaperApiVersion: null,
            JavaVersion: "25",
            Notes: "Curated LocalGPT snapshot datapack mapping for Minecraft Java 26.2. Use only for snapshot worlds and verify loader APIs before Java mod/plugin release."),
        new(
            MinecraftVersion: "1.21.4",
            FabricApiVersion: "0.116.9+1.21.4",
            NeoForgeVersion: "21.1.231",
            PaperApiVersion: "1.21.4-R0.1-SNAPSHOT",
            JavaVersion: "21",
            Notes: "Curated LocalGPT 1.21.4 mapping; NeoForge value is a cautious 1.21.x fallback and should be source-checked before release."),
        new(
            MinecraftVersion: "1.21.1",
            FabricApiVersion: "0.116.9+1.21.1",
            NeoForgeVersion: "21.1.231",
            PaperApiVersion: "1.21.1-R0.1-SNAPSHOT",
            JavaVersion: "21",
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
            ? MinecraftDatapackVersionCatalog.DefaultMinecraftVersion
            : minecraftVersion.Trim();
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
        entry ??= requestedMinecraftVersion.StartsWith("26.", StringComparison.OrdinalIgnoreCase)
            ? KnownVersions.First(item => item.MinecraftVersion == "26.1")
            : requestedMinecraftVersion.StartsWith("1.21", StringComparison.OrdinalIgnoreCase)
            ? KnownVersions.First(item => item.MinecraftVersion == "1.21.4")
            : KnownVersions.First(item => item.MinecraftVersion == "26.1");

        var requestedJavaVersion = string.IsNullOrWhiteSpace(javaVersion)
            ? entry.JavaVersion ?? DefaultJavaVersion
            : javaVersion.Trim();

        var datapack = MinecraftDatapackVersionCatalog.Resolve(requestedMinecraftVersion);
        var needsVerification = !exact ||
            datapack.NeedsVerification ||
            (normalizedLoader is "Fabric" or "NeoForge" or "Paper") &&
            (entry.FabricApiVersion is null || entry.NeoForgeVersion is null || entry.PaperApiVersion is null) ||
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

    public sealed record CatalogEntry(
        string MinecraftVersion,
        string? FabricApiVersion,
        string? NeoForgeVersion,
        string? PaperApiVersion,
        string? JavaVersion,
        string Notes);
}
