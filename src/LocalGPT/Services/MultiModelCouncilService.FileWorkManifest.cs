using System.Text.Json;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Normalizes architecture descriptions at the coordinated source-manifest boundary without interpreting source or executing model JSON.</summary>
public sealed partial class MultiModelCouncilService
{
    /// <summary>Resolves the run-local output budget for configured preparation steps.</summary>
    /// <param name="teamKey">Configured team identity.</param>
    /// <param name="stepKey">Configured workflow step.</param>
    /// <returns>Optional ceiling; other teams and steps keep their existing policy.</returns>
    private int? GetFileWorkPreparationOutputCeiling(string teamKey, string stepKey)
    {
        try
        {
            var policy = runtimePolicy.GetJson<CouncilFileWorkRuntimeParameters>("CouncilFileWorkRuntimeParametersJson");
            return policy.Enabled && string.Equals(teamKey, policy.TeamKey, StringComparison.OrdinalIgnoreCase) &&
                (policy.PreparationStepKeys ?? []).Contains(stepKey, StringComparer.OrdinalIgnoreCase)
                ? Math.Max(64, policy.MaximumPreparationOutputTokens) : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to resolve the configured source preparation budget.");
            throw;
        }
    }

    /// <summary>Extracts explicit .NET target requirements using the configured catalog, independently of model proposals.</summary>
    /// <param name="goal">Authoritative current user goal.</param>
    /// <returns>Distinct normalized target identifiers, without choosing between multiple explicit targets.</returns>
    private List<string> GetExplicitFileWorkTargets(string goal)
    {
        try
        {
            return runtimePolicy.GetPattern("runtime.codegen.explicit-dotnet-target").Matches(goal)
                .Select(match => "net" + match.Groups["major"].Value + "." +
                    (match.Groups["minor"].Success ? match.Groups["minor"].Value : "0"))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving explicit source-work framework requirements failed.");
            throw;
        }
    }

    /// <summary>Accepts shared contracts expressed as text or a JSON object; the persisted draft still owns one textual representation.</summary>
    /// <param name="answer">Visible architect response, excluding model thinking.</param>
    /// <param name="goal">Authoritative user goal used to check explicit framework requirements.</param>
    /// <returns>Parsed manifest for subsequent domain validation by the existing draft gateway.</returns>
    private CreateCodeGenerationReviewRequest ReadSourceManifest(string answer, string goal)
    {
        try
        {
            var fields = ReadFileWorkJson<Dictionary<string, JsonElement>>(answer);
            var normalized = new Dictionary<string, JsonElement>(fields, StringComparer.OrdinalIgnoreCase);
            if (normalized.TryGetValue("draftSharedContract", out var contract) &&
                contract.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                normalized["draftSharedContract"] = JsonSerializer.SerializeToElement(contract.GetRawText());
            if (normalized.TryGetValue("draftDependencies", out var dependencies) && dependencies.ValueKind == JsonValueKind.Object)
            {
                var edges = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                foreach (var edge in dependencies.EnumerateObject())
                    edges.Add(edge.Name, edge.Value.ValueKind == JsonValueKind.String
                        ? JsonSerializer.SerializeToElement(new[] { edge.Value.GetString() }) : edge.Value);
                normalized["draftDependencies"] = JsonSerializer.SerializeToElement(edges);
            }
            var plan = JsonSerializer.Deserialize<CreateCodeGenerationReviewRequest>(JsonSerializer.Serialize(normalized),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new JsonException("Missing source manifest.");
            if (plan.DraftWorkspaceMode is "ReviewedSourceDraft" or "HyperlightSourceDraft")
            {
                var targetPattern = runtimePolicy.GetPattern("runtime.codegen.explicit-dotnet-target");
                var requested = GetExplicitFileWorkTargets(goal);
                foreach (var output in plan.Outputs ?? [])
                {
                    if (output is null) throw new ArgumentException("Manifest output entries must be objects.");
                    var target = output.TargetFramework ?? string.Empty;
                    var match = targetPattern.Match(target);
                    if (match.Success && match.Length == target.Trim().Length)
                        output.TargetFramework = "net" + match.Groups["major"].Value + "." +
                            (match.Groups["minor"].Success ? match.Groups["minor"].Value : "0");
                    if (requested.Count == 1 && string.IsNullOrWhiteSpace(output.TargetFramework) &&
                        output.Kind is "Solution")
                        output.TargetFramework = requested[0]; // Solution metadata inherits the explicit user requirement, not a model-selected fallback.
                    if (requested.Count == 1 && !string.Equals(output.TargetFramework, requested[0], StringComparison.OrdinalIgnoreCase) &&
                        !(output.TargetFramework ?? string.Empty).StartsWith(requested[0] + "-", StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException($"Requested target {requested[0]}; manifest output '{output.Name}' declares {output.TargetFramework}. Correct the output and file work orders before beginning the draft.");
                }
                if (requested.Count == 1 && plan.Outputs is { Count: > 0 } outputs &&
                    (outputs.Any(output => output.Kind is "Solution" or "ClassLibrary" or "ConsoleApplication" or "LocalGptAddon") ||
                        runtimePolicy.GetPattern("runtime.codegen.explicit-project-output").IsMatch(goal)) &&
                    plan.DraftManifest is { Count: > 0 } manifest &&
                    !manifest.Keys.Any(path => string.Equals(Path.GetExtension(path), ".csproj", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(Path.GetExtension(path), ".fsproj", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(Path.GetExtension(path), ".vbproj", StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException($"The requested {requested[0]} solution requires its actual project file in draftManifest.");
            }
            if (plan.DraftWorkspaceMode == "ReviewedSourceDraft")
                SeparateManifestPackageProposals(plan);
            return plan;
        }
        catch (Exception exception)
        {
            logger.LogDebug("Source manifest normalization failed with {Category}.", exception.GetType().Name);
            throw;
        }
    }

    /// <summary>Separates unverified external package proposals from prerequisite file paths while preserving every proposal for the author and reviewer.</summary>
    /// <param name="plan">Parsed ordinary source plan; no project, package or filesystem operation has been executed.</param>
    private void SeparateManifestPackageProposals(CreateCodeGenerationReviewRequest plan)
    {
        try
        {
            if (plan.DraftManifest is not { Count: > 0 } manifest || plan.DraftDependencies is not { Count: > 0 } dependencies)
                return;
            var paths = manifest.Keys.Select(path => path.Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var sourceExtensions = paths.Select(path => Path.GetExtension(path)).Where(extension => !string.IsNullOrEmpty(extension)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            sourceExtensions.UnionWith(new[] { ".cs", ".fs", ".vb", ".csproj", ".fsproj", ".vbproj", ".sln", ".slnx", ".razor", ".cshtml", ".json", ".xml", ".props", ".targets", ".dll", ".exe", ".js", ".ts", ".css", ".html", ".md", ".txt", ".config", ".yaml", ".yml", ".resx", ".ps1", ".py" });
            var packagePattern = runtimePolicy.GetPattern("runtime.codegen.package-proposal-id");
            var proposals = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (path, direct) in dependencies)
            {
                // Only MSBuild project files can own these proposals. Exact file references always win.
                if (!paths.Contains(path.Replace('\\', '/')) || direct is null ||
                    !new[] { ".csproj", ".fsproj", ".vbproj" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) continue;
                var external = direct.Where(dependency => !string.IsNullOrWhiteSpace(dependency) &&
                    !paths.Contains(dependency.Replace('\\', '/')) &&
                    !sourceExtensions.Contains(Path.GetExtension(dependency)) &&
                    packagePattern.IsMatch(dependency)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (external.Count == 0) continue;
                proposals.Add(path, external);
            }
            if (proposals.Count == 0) return;
            foreach (var (path, external) in proposals)
                dependencies[path] = dependencies[path].Where(dependency => !external.Contains(dependency, StringComparer.OrdinalIgnoreCase)).ToList();
            // Suggestions are not package identities verified against a registry, and never authorize restore.
            // Keep the original contract and its package proposals together for the exact reviewed source.
            plan.DraftSharedContract = JsonSerializer.Serialize(new
            {
                Architecture = plan.DraftSharedContract,
                UnverifiedExternalReferencesByProject = proposals,
                Instruction = runtimePolicy.GetString("CouncilFileWorkPackageProposalInstruction")
            });
            logger.LogInformation("Separated {ProposalCount} unverified external dependency proposals from {ProjectCount} source dependency lists.",
                proposals.Values.Sum(items => items.Count), proposals.Count);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to separate external package proposals from the source manifest.");
            throw;
        }
    }

}
