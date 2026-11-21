using System.Xml;
using System.Xml.Linq;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Deterministic project graph and requirement-coverage gates for reviewed source drafts.
/// No files are created here and no unapproved source bytes are modified.</summary>
public sealed partial class CodeGenerationWorkflowService
{
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
        if (goal.Contains("blazor", StringComparison.OrdinalIgnoreCase))
        {
            Map("goal:blazor-host", "OriginalGoal", "Blazor application host", (path, _) =>
                string.Equals(Path.GetFileName(path), "Program.cs", StringComparison.OrdinalIgnoreCase));
            Map("goal:blazor-root", "OriginalGoal", "Blazor root component", (path, _) =>
                string.Equals(Path.GetFileName(path), "App.razor", StringComparison.OrdinalIgnoreCase));
            Map("goal:web-project", "OriginalGoal", "Actual .NET project declaration", (path, _) =>
                path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
        }
        if (goal.Contains("twitch", StringComparison.OrdinalIgnoreCase))
            Map("goal:twitch-service", "OriginalGoal", "Twitch service/source implementation", (path, work) =>
                path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                (path.Contains("twitch", StringComparison.OrdinalIgnoreCase) ||
                 work.Contains("twitch", StringComparison.OrdinalIgnoreCase)));
        if (goal.Contains("timeline", StringComparison.OrdinalIgnoreCase))
            Map("goal:timeline-service", "OriginalGoal", "Configurable timeline service/source", (path, work) =>
                path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                (path.Contains("timeline", StringComparison.OrdinalIgnoreCase) ||
                 work.Contains("timeline", StringComparison.OrdinalIgnoreCase)));
        foreach (var decision in decisions)
        {
            var decisionId = $"human:{decision.RequestId:N}";
            if (IsDatabaseChoice(decision))
            {
                var provider = GetSelectedDatabaseProvider(decision.Answer);
                Map(decisionId + ":storage", decisionId, decision.Answer, (path, work) =>
                    path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                    (!string.IsNullOrWhiteSpace(provider) &&
                    (path.Contains(provider, StringComparison.OrdinalIgnoreCase) ||
                     work.Contains(provider, StringComparison.OrdinalIgnoreCase) ||
                     work.Contains("persist", StringComparison.OrdinalIgnoreCase) ||
                     work.Contains("database", StringComparison.OrdinalIgnoreCase))));
            }
            var choice = GetSelectedSolutionName([decision]);
            if (!string.IsNullOrWhiteSpace(choice))
                Map(decisionId + ":solution", decisionId, decision.Answer, (path, _) =>
                    string.Equals(Path.GetFileNameWithoutExtension(path), choice, StringComparison.OrdinalIgnoreCase) &&
                    (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                     path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)));
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
        bool HasFile(string file) => entries.Any(item => string.Equals(Path.GetFileName(item.Path), file, StringComparison.OrdinalIgnoreCase));
        bool HasExtension(string extension) => entries.Any(item => string.Equals(Path.GetExtension(item.Path), extension, StringComparison.OrdinalIgnoreCase));
        bool HasPurpose(string extension, params string[] words) => entries.Any(item =>
            string.Equals(Path.GetExtension(item.Path), extension, StringComparison.OrdinalIgnoreCase) &&
            words.Any(word => item.Path.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                item.Work.Contains(word, StringComparison.OrdinalIgnoreCase)));
        bool Wants(string keyword) => goal.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        var selectedProvider = decisions.Reverse().Where(IsDatabaseChoice)
            .Select(decision => GetSelectedDatabaseProvider(decision.Answer))
            .FirstOrDefault(provider => !string.IsNullOrWhiteSpace(provider)) ?? string.Empty;
        var selectedName = GetSelectedSolutionName(decisions);
        if (HasExtension(".csproj") && (Wants("blazor") || Wants("aspnet") || Wants("asp.net")))
        {
            if (!HasFile("Program.cs"))
                return "Required .NET web host is absent from draftManifest: include Program.cs with application DI and hosting.";
            if (!HasFile("App.razor") && !HasPurpose(".cshtml", "host", "app"))
                return "Required Blazor root host is absent from draftManifest: include App.razor or an explicit .cshtml host.";
            if (!HasExtension(".razor"))
                return "Required Blazor component source is absent from draftManifest.";
        }
        if (Wants("solution") && HasExtension(".csproj") && !HasExtension(".sln") && !HasExtension(".slnx"))
            return "The requested .NET solution needs a .sln or .slnx entry in draftManifest.";
        if (Wants("twitch") && !HasPurpose(".cs", "twitch", "stream service", "stream api"))
            return "Twitch behavior has no C# implementation work order; pages alone do not implement the Twitch API.";
        if (Wants("timeline") && !HasPurpose(".cs", "timeline", "schedule", "configuration"))
            return "Configurable timelines need a C# domain/service work order, not only a Razor display page.";
        if (!string.IsNullOrWhiteSpace(selectedProvider))
        {
            if (!HasExtension(".csproj") || !HasPurpose(".cs", selectedProvider, "dbcontext", "repository", "persist", "database"))
                return $"Human-selected database provider '{selectedProvider}' requires a project and concrete C# persistence/data work in draftManifest.";
            if (!HasFile("Program.cs") && (Wants("blazor") || Wants("aspnet") || Wants("asp.net")))
                return $"Human-selected database provider '{selectedProvider}' needs the web application's service registration/host.";
        }
        if (!string.IsNullOrWhiteSpace(selectedName) &&
            !entries.Any(item => (string.Equals(Path.GetExtension(item.Path), ".sln", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Path.GetExtension(item.Path), ".slnx", StringComparison.OrdinalIgnoreCase)) &&
                string.Equals(Path.GetFileNameWithoutExtension(item.Path), selectedName, StringComparison.OrdinalIgnoreCase)))
            return $"Human-selected solution name '{selectedName}' is missing from draftManifest; do not rename it without a new decision.";
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
                !string.Equals(name, "sqlite", StringComparison.OrdinalIgnoreCase))
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
            var extension = Path.GetExtension(path);
            if (string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase))
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
                    var fields = line.Split('"');
                    if (fields.Length < 8 || !Guid.TryParse(fields[1], out var typeGuid) ||
                        !Guid.TryParse(fields[7], out var projectGuid) || projectGuid == Guid.Empty)
                        return "Solution has an invalid project type or project GUID (placeholders are forbidden).";
                    if (!projectGuids.Add(projectGuid)) return "Solution declares a duplicate project GUID.";
                    if (typeGuid == Guid.Parse("66A26720-8FB5-11D2-AA7E-00C04F688DDE")) continue; // Solution folder.
                    var reference = fields[5];
                    if (!new[] { ".csproj", ".fsproj", ".vbproj" }.Contains(Path.GetExtension(reference), StringComparer.OrdinalIgnoreCase))
                        return "A solution project entry must reference an actual .csproj, .fsproj or .vbproj file.";
                    var target = ResolveDraftReference(path, reference);
                    if (!manifest.Keys.Any(file => string.Equals(file.Replace('\\', '/'), target, StringComparison.OrdinalIgnoreCase)))
                        return $"Solution references '{target}' but no such project is in the source manifest.";
                    if (!projectPaths.Add(target)) return "Solution declares a duplicate project file reference.";
                }
                return ValidateSolutionProjectCoverage(path, projectPaths, manifest);
            }
            if (string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase) ||
                new[] { ".csproj", ".fsproj", ".vbproj" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                using var reader = XmlReader.Create(new StringReader(content), new XmlReaderSettings
                { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
                var document = XDocument.Load(reader);
                var rootName = string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase) ? "Solution" : "Project";
                if (document.Root?.Name.LocalName != rootName) return $"Expected {rootName} XML root.";
                var references = document.Descendants().Where(element =>
                    element.Name.LocalName == (rootName == "Solution" ? "Project" : "ProjectReference"))
                    .Select(element => (string?)element.Attribute(rootName == "Solution" ? "Path" : "Include")).ToList();
                if (rootName == "Solution" && references.Count == 0) return "Solution XML declares no projects.";
                var projectPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var reference in references)
                {
                    if (string.IsNullOrWhiteSpace(reference)) return $"{rootName} contains a project reference without a path.";
                    if (!new[] { ".csproj", ".fsproj", ".vbproj" }.Contains(Path.GetExtension(reference), StringComparer.OrdinalIgnoreCase))
                        return $"{rootName} project reference '{reference}' is not a recognized .NET project file.";
                    var target = ResolveDraftReference(path, reference);
                    if (!manifest.Keys.Any(file => string.Equals(file.Replace('\\', '/'), target, StringComparison.OrdinalIgnoreCase)))
                        return $"{rootName} references '{target}' but no such project is in the source manifest.";
                    projectPaths.Add(target);
                }
                if (rootName == "Solution")
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
        var solutions = manifest.Keys.Count(path => new[] { ".sln", ".slnx" }
            .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase));
        if (solutions != 1) return string.Empty; // Multiple solutions can intentionally partition projects.
        var missing = manifest.Keys.Select(path => path.Replace('\\', '/'))
            .Where(path => new[] { ".csproj", ".fsproj", ".vbproj" }
                .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
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
            var declarations = files.Values.Where(file => new[] { ".csproj", ".fsproj", ".vbproj", ".props", ".targets" }
                .Contains(Path.GetExtension(file.RelativePath), StringComparer.OrdinalIgnoreCase)).ToList();
            var providerTokens = string.Equals(provider, "MariaDb", StringComparison.OrdinalIgnoreCase)
                ? new[] { "MariaDb", "MySql", "Pomelo" } : string.Equals(provider, "MySql", StringComparison.OrdinalIgnoreCase)
                ? new[] { "MySql", "Pomelo" } : new[] { provider };
            if (!declarations.Any(file => providerTokens.Any(token => file.Content.Contains(token, StringComparison.OrdinalIgnoreCase))))
                return $"Human-selected {provider} database has no reviewed project/central provider dependency declaration (for example: Human-selected SQLite requires a SQLite provider).";
            if (!files.Values.Any(file => string.Equals(Path.GetExtension(file.RelativePath), ".cs", StringComparison.OrdinalIgnoreCase) &&
                providerTokens.Any(token => file.Content.Contains(token, StringComparison.OrdinalIgnoreCase))))
                return $"Human-selected {provider} database has no reviewed C# provider configuration or persistence source.";
        }
        // An injected project-owned contract must actually exist in the reviewed C# sources.
        var ownSource = files.Values.Where(file => string.Equals(Path.GetExtension(file.RelativePath), ".cs", StringComparison.OrdinalIgnoreCase))
            .Select(file => file.Content).ToList();
        var projectRoots = files.Values.Where(file => string.Equals(Path.GetExtension(file.RelativePath), ".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetFileNameWithoutExtension(file.RelativePath.Replace('\\', '/'))).ToList();
        foreach (var razor in files.Values.Where(file => string.Equals(Path.GetExtension(file.RelativePath), ".razor", StringComparison.OrdinalIgnoreCase)))
        {
            var usesAppContracts = razor.Content.Split('\n').Any(line =>
                projectRoots.Any(root => line.Trim().StartsWith("@using " + root + ".Contracts", StringComparison.Ordinal)));
            if (!usesAppContracts) continue;
            foreach (var line in razor.Content.Split('\n').Select(value => value.Trim()))
            {
                if (!line.StartsWith("@inject I", StringComparison.Ordinal)) continue;
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) continue;
                var type = parts[1];
                if (!ownSource.Any(code => code.Contains("interface " + type, StringComparison.Ordinal)))
                    return $"{razor.RelativePath}: injected project contract '{type}' has no reviewed C# interface declaration.";
            }
        }
        return string.Empty;
    }
}
