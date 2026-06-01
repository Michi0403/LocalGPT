namespace LocalGPT.Services;

public sealed record MinecraftDatapackVersionInfo(
    string RequestedVersion,
    string MatchedVersion,
    int PackFormat,
    string FunctionRegistryFolder,
    bool IsExactMatch,
    bool NeedsVerification,
    string Notes,
    string Source);

public static class MinecraftDatapackVersionCatalog
{
    private static readonly MinecraftDatapackVersionInfo[] KnownVersions =
    [
        Known("1.21.4", 61, "function", "LocalGPT Living Cities benchmark target."),
        Known("1.21.3", 57, "function", "Minecraft 1.21.2/1.21.3 datapack format family."),
        Known("1.21.2", 57, "function", "Minecraft 1.21.2/1.21.3 datapack format family."),
        Known("1.21.1", 48, "function", "Minecraft 1.21/1.21.1 datapack format family."),
        Known("1.21", 48, "function", "Minecraft 1.21/1.21.1 datapack format family.")
    ];

    public static MinecraftDatapackVersionInfo Resolve(string? minecraftVersion)
    {
        var requested = string.IsNullOrWhiteSpace(minecraftVersion)
            ? "1.21.4"
            : minecraftVersion.Trim();

        var exact = KnownVersions.FirstOrDefault(item =>
            requested.Equals(item.MatchedVersion, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact with { RequestedVersion = requested };

        var prefix = KnownVersions
            .Where(item => requested.StartsWith(item.MatchedVersion, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.MatchedVersion.Length)
            .FirstOrDefault();
        if (prefix is not null)
            return prefix with { RequestedVersion = requested, IsExactMatch = false, NeedsVerification = true, Notes = $"{prefix.Notes} Version matched by prefix; verify against the official Minecraft version manifest before friend testing." };

        var fallback = requested.StartsWith("1.21", StringComparison.OrdinalIgnoreCase)
            ? KnownVersions.First(item => item.MatchedVersion == "1.21.4")
            : KnownVersions.First(item => item.MatchedVersion == "1.21.1");

        return fallback with
        {
            RequestedVersion = requested,
            IsExactMatch = false,
            NeedsVerification = true,
            Notes = $"No exact LocalGPT mapping for Minecraft {requested}. Using {fallback.MatchedVersion} as a cautious fallback; verify pack_format with the official version manifest."
        };
    }

    private static MinecraftDatapackVersionInfo Known(string version, int packFormat, string functionRegistryFolder, string notes) =>
        new(
            RequestedVersion: version,
            MatchedVersion: version,
            PackFormat: packFormat,
            FunctionRegistryFolder: functionRegistryFolder,
            IsExactMatch: true,
            NeedsVerification: false,
            Notes: notes,
            Source: "LocalGPT curated datapack version catalog; verify unknown versions with the official Minecraft version manifest.");
}
