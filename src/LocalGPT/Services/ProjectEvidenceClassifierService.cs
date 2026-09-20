using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Classifies repository/toolchain evidence by bounded manifest names only; it never executes project code.</summary>
public sealed class ProjectEvidenceClassifierService(ILogger<ProjectEvidenceClassifierService> logger) : IProjectEvidenceClassifierService
{
    private readonly (string Marker, string ProjectKind, string Toolchain)[] Markers =
    [
        ("*.sln", "DotNetSolution", ".NET/MSBuild"), ("*.slnx", "DotNetSolution", ".NET/MSBuild"), ("*.csproj", "DotNetProject", ".NET/MSBuild"),
        ("pom.xml", "JavaMaven", "Java/Maven"), ("build.gradle", "JavaGradle", "Java/Gradle"), ("build.gradle.kts", "JavaGradle", "Java/Gradle"),
        ("package.json", "Node", "Node/npm"), ("pnpm-lock.yaml", "Node", "Node/pnpm"), ("yarn.lock", "Node", "Node/yarn"),
        ("Cargo.toml", "Rust", "Rust/Cargo"), ("go.mod", "Go", "Go"), ("pyproject.toml", "Python", "Python"), ("requirements.txt", "Python", "Python/pip"),
        ("fabric.mod.json", "MinecraftMod", "Minecraft/Fabric"), ("mods.toml", "MinecraftMod", "Minecraft/Forge"), ("neoforge.mods.toml", "MinecraftMod", "Minecraft/NeoForge"), ("paper-plugin.yml", "MinecraftPlugin", "Minecraft/Paper"), ("plugin.yml", "MinecraftPlugin", "Minecraft/Bukkit")
    ];

    public Task<(IReadOnlyList<string> ProjectKinds, IReadOnlyList<string> Toolchains)> ClassifyAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(rootPath)) return Task.FromResult<(IReadOnlyList<string>, IReadOnlyList<string>)>(([], []));
            var kinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var toolchains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .Where(path => !IsExcluded(path, rootPath)).Take(20000).ToList();
            foreach (var marker in Markers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var matched = marker.Marker.StartsWith("*.", StringComparison.Ordinal)
                    ? files.Any(path => path.EndsWith(marker.Marker[1..], StringComparison.OrdinalIgnoreCase))
                    : files.Any(path => string.Equals(Path.GetFileName(path), marker.Marker, StringComparison.OrdinalIgnoreCase));
                if (!matched) continue;
                kinds.Add(marker.ProjectKind);
                toolchains.Add(marker.Toolchain);
            }
            return Task.FromResult<(IReadOnlyList<string>, IReadOnlyList<string>)>((kinds.OrderBy(x => x).ToList(), toolchains.OrderBy(x => x).ToList()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Classifying repository evidence failed; source content was not executed.");
            throw;
        }
    }

    private bool IsExcluded(string path, string root)
    {
        try
        {
            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            return relative.Split('/').Any(segment => segment is ".git" or ".vs" or "bin" or "obj" or "node_modules" or "target" or ".gradle");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Checking project-classifier excluded path failed.");
            throw;
        }
    }
}
