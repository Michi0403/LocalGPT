namespace LocalGPT.Services;

public sealed record DxaichatFunctionInfo(
    string Name,
    string Method,
    string Route,
    string Purpose,
    string Parameters,
    string SafetyNotes);

public static class DxaichatFunctionCatalog
{
    private static readonly DxaichatFunctionInfo[] Functions =
    [
        new(
            "minecraft.datapack_version",
            "GET",
            "/__diag/minecraft/datapack-version?minecraftVersion=1.21.4",
            "Resolve the datapack pack_format and singular/plural function folder convention for a Minecraft Java version.",
            "minecraftVersion: optional, defaults to 1.21.4.",
            "Read-only. Unknown versions are marked NeedsVerification instead of guessed as fact."),
        new(
            "minecraft.workspace_smoke",
            "GET",
            "/__diag/minecraft/workspace-smoke?loader=datapack",
            "Generate a small Minecraft workspace for datapack, Fabric, NeoForge, or Paper smoke testing.",
            "loader: datapack, fabric, neoforge, or paper.",
            "Creates files under LocalAppData/LocalGPT/MinecraftModWorkspaces; does not launch Minecraft."),
        new(
            "minecraft.datapack_benchmark",
            "GET",
            "/__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4",
            "Generate, validate, zip, and save a compact council-knowledge entry for the Living Cities datapack benchmark.",
            "minecraftVersion: optional, defaults to 1.21.4.",
            "Runs the generated local build-local.ps1 validator; does not copy into a game world or run /reload."),
        new(
            "council.models",
            "GET",
            "/__diag/council/models",
            "List configured and installed Ollama council model candidates.",
            "No parameters.",
            "Read-only model discovery."),
        new(
            "localgpt.blazor_devexpress_guidance",
            "GET",
            "/__diag/blazor-devexpress-guidance",
            "Return compact LocalGPT/TacosPortalOpen-derived guidance for generating real .NET 10 Blazor server-interactive pages with DevExpress components.",
            "No parameters.",
            "Read-only. Use this before generating Razor pages or blaming missing DevExpress/.NET context."),
        new(
            "council.artifact_smoke",
            "GET",
            "/__diag/council/artifact-smoke?target=blazor",
            "Generate a deterministic sandbox artifact bundle without calling Ollama, useful for testing .razor/.cs/.dll and whole-solution zip downloads.",
            "target: optional, defaults to blazor. Use target=solution for a zipped .NET 10 Blazor/DevExpress solution, or target=ollama for an Ollama-inspired .NET/DevExpress control-plane lab.",
            "Writes files under LocalAppData/LocalGPT/CouncilArtifacts; does not integrate generated code into the project."),
        new(
            "council.run",
            "POST",
            "/__diag/council",
            "Run the LocalGPT AI Council backend with an explicit MultiModelCouncilRequest.",
            "JSON body: model names, prompt, token limits, CPU/GPU options, and artifact flags.",
            "Potentially expensive. Prefer CPU-only and one small model when the machine is unstable.")
    ];

    public static IReadOnlyList<DxaichatFunctionInfo> GetFunctions() => Functions;

    public static string BuildPromptBriefing()
    {
        return string.Join(Environment.NewLine, Functions.Select(function =>
            $"- {function.Name}: {function.Method} {function.Route} — {function.Purpose} Parameters: {function.Parameters} Safety: {function.SafetyNotes}"));
    }
}
