using System.Xml;
using System.Xml.Linq;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Deterministic project graph and requirement-coverage gates for reviewed source drafts.
/// No files are created here and no unapproved source bytes are modified.</summary>
public sealed partial class CodeGenerationWorkflowService
{
    /// <summary>Recognizes solution paths using the existing database-backed workspace file pattern.
    /// Compiler installations identify executable toolchains, not preferred project filenames.</summary>
    /// <param name="path">Manifest-relative source path.</param>
    /// <returns>Whether this path matches the configured solution-file pattern.</returns>
    private bool IsSourceSolutionPath(string path) =>
        runtimePolicy.GetPattern("runtime.project.solution-file-extension").IsMatch(path.Replace('\\', '/'));

    /// <summary>Recognizes an MSBuild project filename by its project-document suffix, without assuming C#, F#, VB,
    /// a particular basename, or a fixed project folder. Source XML is checked separately.</summary>
    /// <param name="path">Manifest-relative source path.</param>
    /// <returns>Whether the path identifies an MSBuild project file.</returns>
    private bool IsSourceProjectPath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Length > 1 && extension.EndsWith("proj", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Determines whether the source uses an MSBuild XML declaration format.</summary>
    /// <param name="path">Manifest-relative source path.</param>
    /// <returns>Whether source XML/project-target checks apply.</returns>
    private bool IsSourceBuildDeclarationPath(string path)
    {
        var extension = Path.GetExtension(path);
        return IsSourceProjectPath(path) ||
            extension.Equals(".props", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".targets", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Reads the current draft and protected human decisions before any approval is proposed.</summary>
    public async Task<string> GetSourceDraftClosureDiagnosticAsync(Guid runId, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var draft = await FindCouncilDraftAsync(db, runId, cancellationToken).ConfigureAwait(false);
        if (draft is null || draft.Status != DraftStatus)
            return "No active writable source draft exists for this Council run.";
        var plan = ReadDraftPlan(draft);
        var manifest = plan.DraftManifest ?? new Dictionary<string, string>();
        if (plan.Files.Count != manifest.Count || manifest.Keys.Any(path => !plan.Files.Any(file =>
            string.Equals(file.RelativePath, path, StringComparison.OrdinalIgnoreCase))))
            return "The active draft has uncompleted source files; author and independently inspect the remaining manifest paths first.";
        var decisions = await humanCollaboration.GetSourcePlanningDecisionsAsync(runId, cancellationToken).ConfigureAwait(false);
        return ValidateCompletedSourceCoverage(plan, decisions);
    }

    /// <summary>Creates a traceable, source-identity-only coverage graph for protected requirements.
    /// It records what was planned and which source bytes exist; it never claims successful compilation.</summary>
    private List<CodeGenerationSourceRequirement> BuildSourceRequirementGraph(string goal,
        IReadOnlyDictionary<string, string> manifest, IReadOnlyList<CouncilSourceHumanDecision> decisions,
        IReadOnlyList<CodeGenerationFileSpec>? files = null)
    {
        var requirements = new List<CodeGenerationSourceRequirement>();
        var reviewed = (files ?? []).Select(file => file.RelativePath.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        void Map(string key, string origin, string requirement, Func<string, string, bool> matches)
        {
            var paths = manifest.Where(item => matches(item.Key.Replace('\\', '/'), item.Value))
                .Select(item => item.Key.Replace('\\', '/')).ToList();
            var sourcePaths = paths.Where(reviewed.Contains).ToList();
            requirements.Add(new CodeGenerationSourceRequirement
            {
                Key = key, Origin = origin, Requirement = requirement, ManifestPaths = paths,
                SourcePaths = sourcePaths,
                EvidenceState = paths.Count == 0 ? "ManifestMissing" : sourcePaths.Count == 0 ?
                    "ManifestDeclared" : "SourcePresent"
            });
        }
        if (runtimePolicy.GetPattern("runtime.codegen.explicit-dotnet-target").IsMatch(goal) ||
            goal.Contains("blazor", StringComparison.OrdinalIgnoreCase) ||
            goal.Contains("asp.net", StringComparison.OrdinalIgnoreCase))
        {
            Map("goal:project", "OriginalGoal", "Compiler project definition", (path, _) => IsSourceProjectPath(path));
            if (goal.Contains("blazor", StringComparison.OrdinalIgnoreCase))
            {
                Map("goal:components", "OriginalGoal", "Blazor component source", (path, _) =>
                    Path.GetExtension(path).Equals(".razor", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(path).Equals(".cshtml", StringComparison.OrdinalIgnoreCase));
                Map("goal:hosting", "OriginalGoal", "Application startup/hosting implementation", (path, work) =>
                    !IsSourceSolutionPath(path) && !IsSourceBuildDeclarationPath(path) &&
                    (work.Contains("host", StringComparison.OrdinalIgnoreCase) ||
                     work.Contains("startup", StringComparison.OrdinalIgnoreCase) ||
                     work.Contains("bootstrap", StringComparison.OrdinalIgnoreCase) ||
                     work.Contains("entry point", StringComparison.OrdinalIgnoreCase)));
            }
        }
        foreach (var decision in decisions)
        {
            var decisionId = $"human:{decision.RequestId:N}";
            if (IsDatabaseChoice(decision))
            {
                var provider = GetSelectedDatabaseProvider(decision.Answer);
                Map(decisionId + ":storage", decisionId, decision.Answer, (path, work) =>
                    Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase) &&
                    (!string.IsNullOrWhiteSpace(provider) &&
                    (work.Contains(provider, StringComparison.OrdinalIgnoreCase) ||
                     work.Contains("persist", StringComparison.OrdinalIgnoreCase) ||
                     work.Contains("database", StringComparison.OrdinalIgnoreCase) ||
                     work.Contains("storage", StringComparison.OrdinalIgnoreCase))));
            }
            var choice = GetSelectedSolutionName([decision]);
            if (!string.IsNullOrWhiteSpace(choice))
                Map(decisionId + ":solution", decisionId, decision.Answer, (path, _) =>
                    string.Equals(Path.GetFileNameWithoutExtension(path), choice, StringComparison.OrdinalIgnoreCase) &&
                    IsSourceSolutionPath(path));
            if (!IsDatabaseChoice(decision) && string.IsNullOrWhiteSpace(choice))
                requirements.Add(new CodeGenerationSourceRequirement
                {
                    Key = decisionId + ":choice", Origin = decisionId, Requirement = decision.Answer,
                    EvidenceState = "NeedsSemanticVerification"
                });
        }
        return requirements;
    }

    /// <summary>Rejects a manifest that omits host, domain or explicitly selected storage obligations.</summary>
    private string ValidateSourceManifestCoverage(string goal, IReadOnlyDictionary<string, string> manifest,
        IReadOnlyList<CouncilSourceHumanDecision> decisions)
    {
        var entries = manifest.Select(item => (Path: item.Key.Replace('\\', '/'), Work: item.Value)).ToList();
        bool HasImplementationPurpose(params string[] words) => entries.Any(item =>
            !IsSourceSolutionPath(item.Path) && !IsSourceBuildDeclarationPath(item.Path) &&
            words.Any(word => item.Work.Contains(word, StringComparison.OrdinalIgnoreCase)));
        bool Wants(string keyword) => goal.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        var hasProject = entries.Any(item => IsSourceProjectPath(item.Path));
        var hasSolution = entries.Any(item => IsSourceSolutionPath(item.Path));
        var requestedDotNet = runtimePolicy.GetPattern("runtime.codegen.explicit-dotnet-target").IsMatch(goal);
        var selectedProvider = decisions.Reverse().Where(IsDatabaseChoice)
            .Select(decision => GetSelectedDatabaseProvider(decision.Answer))
            .FirstOrDefault(provider => !string.IsNullOrWhiteSpace(provider)) ?? string.Empty;
        var selectedName = GetSelectedSolutionName(decisions);
        if ((requestedDotNet || Wants("blazor") || Wants("aspnet") || Wants("asp.net")) && !hasProject)
            return "A requested .NET application requires an authored project definition. Use the project filename and toolchain selected for this workspace.";
        if (Wants("blazor") && hasProject)
        {
            if (!entries.Any(item => Path.GetExtension(item.Path).Equals(".razor", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(item.Path).Equals(".cshtml", StringComparison.OrdinalIgnoreCase)))
                return "A Blazor application requires component or host markup in the source manifest; use the selected template's paths.";
            if (!HasImplementationPurpose("host", "startup", "bootstrap", "entry point"))
                return "The Blazor application's source manifest must explicitly assign application startup/hosting work; no specific file basename or folder is required.";
        }
        if (Wants("solution") && hasProject && !hasSolution)
            return "The requested solution is absent from the manifest; use the workspace's configured solution-file naming convention.";
        if (!string.IsNullOrWhiteSpace(selectedProvider) &&
            !HasImplementationPurpose(selectedProvider, "database", "persist", "storage", "repository", "dbcontext"))
            return $"Human-selected database provider '{selectedProvider}' has no explicit persistence implementation work order in the manifest.";
        if (!string.IsNullOrWhiteSpace(selectedName) &&
            !entries.Any(item => IsSourceSolutionPath(item.Path) &&
                string.Equals(Path.GetFileNameWithoutExtension(item.Path), selectedName, StringComparison.OrdinalIgnoreCase)))
            return $"Human-selected solution name '{selectedName}' is missing from the configured solution paths; do not rename it without a new decision.";
        return string.Empty;
    }

    /// <summary>Requires a real question about storage before treating a human answer as a database selection.</summary>
    private bool IsDatabaseChoice(CouncilSourceHumanDecision decision) =>
        (decision.Title + " " + decision.Question).Contains("database", StringComparison.OrdinalIgnoreCase) ||
        (decision.Title + " " + decision.Question).Contains("storage provider", StringComparison.OrdinalIgnoreCase);

    /// <summary>Extracts a recorded provider choice without relying on an LLM's later summary.
    /// Unknown single-token alternatives are retained literally and must be backed by actual source.</summary>
    private string GetSelectedDatabaseProvider(string answer)
    {
        if (answer.Contains("sqlite", StringComparison.OrdinalIgnoreCase)) return "Sqlite";
        if (answer.Contains("postgresql", StringComparison.OrdinalIgnoreCase) ||
            answer.Contains("postgres", StringComparison.OrdinalIgnoreCase) ||
            answer.Contains("npgsql", StringComparison.OrdinalIgnoreCase)) return "Npgsql";
        if (answer.Contains("sql server", StringComparison.OrdinalIgnoreCase) ||
            answer.Contains("mssql", StringComparison.OrdinalIgnoreCase)) return "SqlServer";
        if (answer.Contains("mariadb", StringComparison.OrdinalIgnoreCase)) return "MariaDb";
        if (answer.Contains("mysql", StringComparison.OrdinalIgnoreCase)) return "MySql";
        if (answer.Contains("mongodb", StringComparison.OrdinalIgnoreCase)) return "MongoDB";
        if (answer.Contains("duckdb", StringComparison.OrdinalIgnoreCase)) return "DuckDB";
        var parts = answer.Split(new[] { ' ', '\r', '\n', '\t', ',', ';', '/', ':', '=' },
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 && parts[0].Length is > 1 and <= 70 &&
            char.IsLetter(parts[0][0]) && parts[0].All(c => char.IsLetterOrDigit(c) || c is '_' or '-') &&
            !new[] { "none", "no", "not", "without", "and", "unsure", "unknown", "please", "choose" }
                .Contains(parts[0], StringComparer.OrdinalIgnoreCase) ? parts[0] : string.Empty;
    }

    /// <summary>Recognizes a simple explicit solution-name token from a question asking about the name.
    /// More complex free-text answers remain visible to the architect and need clarification, never guessing.</summary>
    private string GetSelectedSolutionName(IReadOnlyList<CouncilSourceHumanDecision> decisions)
    {
        foreach (var decision in decisions.Reverse())
        {
            var question = decision.Title + " " + decision.Question;
            if (!question.Contains("name", StringComparison.OrdinalIgnoreCase) &&
                !question.Contains("solution", StringComparison.OrdinalIgnoreCase)) continue;
            var words = decision.Answer.Split(new[] { ' ', '\r', '\n', '\t', ',', ';', '/', ':', '=' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var separatedName = decision.Answer.Contains('/') || decision.Answer.Contains(':') || decision.Answer.Contains('=');
            var name = words.Length == 1 ? words[0] : words.Length > 1 &&
                (string.Equals(words[^2], "and", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(words[^2], "name", StringComparison.OrdinalIgnoreCase) || separatedName)
                ? words[^1] : string.Empty;
            if (name.Length is > 0 and <= 100 && char.IsLetter(name[0]) &&
                name.All(character => char.IsLetterOrDigit(character) || character is '_' or '-') &&
                (!IsDatabaseChoice(decision) || words.Length > 1))
                return name;
        }
        return string.Empty;
    }

    /// <summary>Canonicalizes references relative to the owning file, preserving Windows-style authored paths on all hosts.</summary>
    private string ResolveDraftReference(string sourcePath, string reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.StartsWith('/') || reference.StartsWith('\\') ||
            reference.Contains(':') || reference.Contains("$(", StringComparison.Ordinal))
            throw new ArgumentException($"Non-local or dynamic project reference '{reference}' requires an independently verified resolution.");
        var sourceParts = sourcePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var resolved = sourceParts.Take(sourceParts.Length - 1).ToList();
        foreach (var part in reference.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".") continue;
            if (part == "..")
            {
                if (resolved.Count == 0) throw new ArgumentException($"Project reference '{reference}' escapes the source root.");
                resolved.RemoveAt(resolved.Count - 1);
            }
            else resolved.Add(part);
        }
        return string.Join("/", resolved);
    }

    /// <summary>Per-file deterministic checks before storing candidate bytes, independent of a model review vote.</summary>
    private string ValidateDraftSourceReferences(string path, string content, IReadOnlyDictionary<string, string> manifest)
    {
        try
        {
            var solution = IsSourceSolutionPath(path);
            if (solution && !Path.GetExtension(path).Equals(".slnx", StringComparison.OrdinalIgnoreCase) &&
                !content.TrimStart().StartsWith("<", StringComparison.Ordinal))
            {
                var lines = content.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 4 || !lines[0].TrimStart('\uFEFF').StartsWith("Microsoft Visual Studio Solution File, Format Version ", StringComparison.Ordinal))
                    return "Solution is missing the Visual Studio solution-format header.";
                if (!lines.Contains("Global", StringComparer.Ordinal) || !lines.Contains("EndGlobal", StringComparer.Ordinal))
                    return "Solution is missing the Global/EndGlobal configuration block.";
                var projects = lines.Where(line => line.StartsWith("Project(\"", StringComparison.Ordinal)).ToList();
                var projectPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var projectGuids = new HashSet<Guid>();
                if (projects.Count == 0) return "Solution declares no projects.";
                foreach (var line in projects)
                {
                    var fields = line.Split('\"');
                    if (fields.Length < 8 || !Guid.TryParse(fields[1], out var typeGuid) ||
                        !Guid.TryParse(fields[7], out var projectGuid) || projectGuid == Guid.Empty)
                        return "Solution has an invalid project type or project GUID (placeholders are forbidden).";
                    if (!projectGuids.Add(projectGuid)) return "Solution declares a duplicate project GUID.";
                    if (typeGuid == Guid.Parse("66A26720-8FB5-11D2-AA7E-00C04F688DDE")) continue;
                    var reference = fields[5];
                    if (!IsSourceProjectPath(reference))
                        return "Solution project entry does not reference a compiler project file.";
                    var target = ResolveDraftReference(path, reference);
                    if (!manifest.Keys.Any(file => string.Equals(file.Replace('\\', '/'), target, StringComparison.OrdinalIgnoreCase)))
                        return $"Solution references '{target}' but no such project is in the source manifest.";
                    if (!projectPaths.Add(target)) return "Solution declares a duplicate project file reference.";
                }
                return ValidateSolutionProjectCoverage(path, projectPaths, manifest);
            }
            if (solution || IsSourceProjectPath(path))
            {
                using var reader = XmlReader.Create(new StringReader(content), new XmlReaderSettings
                { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
                var document = XDocument.Load(reader);
                var rootName = solution ? "Solution" : "Project";
                if (document.Root?.Name.LocalName != rootName) return $"Expected {rootName} XML root.";
                if (IsSourceProjectPath(path) && manifest.TryGetValue(path, out var workOrder) &&
                    runtimePolicy.GetPattern("runtime.codegen.console-project-work-order").IsMatch(workOrder))
                {
                    var outputs = document.Descendants().Where(element => element.Name.LocalName == "OutputType").ToList();
                    if (outputs.Count == 1 && outputs[0].Value.Trim().Equals("Library", StringComparison.OrdinalIgnoreCase) &&
                        !outputs[0].AncestorsAndSelf().Any(element => element.Attribute("Condition") is not null))
                        return "The console application work order requires an executable. The project declares OutputType Library.";
                }
                var references = document.Descendants().Where(element =>
                    element.Name.LocalName == (solution ? "Project" : "ProjectReference"))
                    .Select(element => (string?)element.Attribute(solution ? "Path" : "Include")).ToList();
                if (solution && references.Count == 0) return "Solution XML declares no projects.";
                var projectPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var reference in references)
                {
                    if (string.IsNullOrWhiteSpace(reference)) return $"{rootName} contains a project reference without a path.";
                    if (!IsSourceProjectPath(reference))
                        return $"{rootName} project reference '{reference}' is not a compiler project file.";
                    var target = ResolveDraftReference(path, reference);
                    if (!manifest.Keys.Any(file => string.Equals(file.Replace('\\', '/'), target, StringComparison.OrdinalIgnoreCase)))
                        return $"{rootName} references '{target}' but no such project is in the source manifest.";
                    if (!projectPaths.Add(target)) return $"{rootName} declares a duplicate project file reference.";
                }
                if (solution)
                    return ValidateSolutionProjectCoverage(path, projectPaths, manifest);
            }
            return string.Empty;
        }
        catch (XmlException exception) { return $"Source XML invalid at line {exception.LineNumber}, position {exception.LinePosition}."; }
        catch (ArgumentException exception) { return exception.Message; }
    }

    /// <summary>One solution in a source draft must include every authored project; orphan projects are not a connected application.</summary>
    private string ValidateSolutionProjectCoverage(string solutionPath, IReadOnlySet<string> referenced,
        IReadOnlyDictionary<string, string> manifest)
    {
        var solutions = manifest.Keys.Count(IsSourceSolutionPath);
        if (solutions != 1) return string.Empty; // Multiple solutions can intentionally partition projects.
        var missing = manifest.Keys.Select(path => path.Replace('\\', '/'))
            .Where(IsSourceProjectPath)
            .FirstOrDefault(path => !referenced.Contains(path));
        return missing is null ? string.Empty :
            $"Solution '{solutionPath}' omits manifest project '{missing}'; connect the project using its actual relative path.";
    }

    /// <summary>Validates older immutable reviews against today's protected decisions without altering review bytes.</summary>
    private string ValidateCompletedSourceCoverage(CodeGenerationReviewPayload payload, string originalGoal,
        IReadOnlyList<CouncilSourceHumanDecision> decisions) =>
        ValidateCompletedSourceCoverage(new CreateCodeGenerationReviewRequest
        {
            Goal = originalGoal, DraftManifest = payload.DraftManifest, Files = payload.Files
        }, decisions);

    /// <summary>Checks the completed manifest against actual source bytes, not only models' work orders or review votes.</summary>
    private string ValidateCompletedSourceCoverage(CreateCodeGenerationReviewRequest plan,
        IReadOnlyList<CouncilSourceHumanDecision> decisions)
    {
        var manifest = plan.DraftManifest ?? new Dictionary<string, string>();
        // Content hashes and a complete file count are both necessary. Older reviews might
        // contain only a subset of the agreed manifest and must never bypass this check.
        var sourcePaths = plan.Files.Select(file => file.RelativePath.Replace('\\', '/')).ToList();
        if (sourcePaths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != sourcePaths.Count)
            return "Reviewed source contains duplicate relative file paths.";
        var manifestPaths = manifest.Keys.Select(path => path.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (sourcePaths.Count != manifestPaths.Count || sourcePaths.Any(path => !manifestPaths.Contains(path)))
            return "Reviewed source is not identical to its manifest: one or more declared file paths are missing or extra.";
        var files = plan.Files.ToDictionary(file => file.RelativePath, StringComparer.OrdinalIgnoreCase);
        var diagnostic = ValidateSourceManifestCoverage(plan.Goal, manifest, decisions);
        if (!string.IsNullOrWhiteSpace(diagnostic)) return diagnostic;
        foreach (var file in files.Values)
        {
            diagnostic = ValidateDraftSourceReferences(file.RelativePath, file.Content, manifest);
            if (!string.IsNullOrWhiteSpace(diagnostic)) return $"{file.RelativePath}: {diagnostic}";
        }
        var provider = decisions.Reverse().Where(IsDatabaseChoice)
            .Select(decision => GetSelectedDatabaseProvider(decision.Answer))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(provider))
        {
            var declarations = files.Values.Where(file => IsSourceBuildDeclarationPath(file.RelativePath)).ToList();
            var providerTokens = string.Equals(provider, "MariaDb", StringComparison.OrdinalIgnoreCase)
                ? new[] { "MariaDb", "MySql", "Pomelo" } : string.Equals(provider, "MySql", StringComparison.OrdinalIgnoreCase)
                ? new[] { "MySql", "Pomelo" } : new[] { provider };
            if (!declarations.Any(file => providerTokens.Any(token => file.Content.Contains(token, StringComparison.OrdinalIgnoreCase))))
                return $"Human-selected {provider} database has no reviewed project/central provider dependency declaration (for example: Human-selected SQLite requires a SQLite provider).";
            if (!files.Values.Any(file => !IsSourceBuildDeclarationPath(file.RelativePath) &&
                !IsSourceSolutionPath(file.RelativePath) &&
                providerTokens.Any(token => file.Content.Contains(token, StringComparison.OrdinalIgnoreCase))))
                return $"Human-selected {provider} database has no reviewed provider configuration or persistence source.";
        }
        // Project filenames do not define CLR namespaces or DI contracts. Compiler verification
        // and independent source inspection own symbol resolution; never invent a namespace
        // from a .proj basename or reject valid external DI interfaces here.
        return string.Empty;
    }
}
