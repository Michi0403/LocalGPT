using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Maintains media- and panel-specific Council team templates for explicitly paired PublisherStudio workflows.</summary>
public sealed partial class OrganicCouncilBlueprintSeedDataService
{
    /// <summary>Creates a category-specific media team that keeps model availability distinct from verified generation receipts.</summary>
    /// <param name="category">Image, video or audio generation domain.</param>
    /// <param name="teamKey">Stable persisted team identity.</param>
    /// <returns>The enabled seed definition; users can review and customize it.</returns>
    private OrganicCouncilTeamDefinition CreatePublisherMediaTeam(string category, string teamKey)
    {
        try
        {
            var choiceInstruction = category switch
            {
                "image" => "If the user has not specified how to create the image, ask them to choose between generating it with a model (offer help finding or setting up a suitable model such as a diffusion model when needed) and drawing it using the visible PublisherStudio Picture Studio tools. A request to draw in Picture Studio already selects the second option.",
                "video" => "If the video workflow is ambiguous, ask whether to generate a new video or edit existing footage in PublisherStudio Video Studio.",
                _ => "If the audio workflow is ambiguous, ask whether to generate new audio or edit an existing track in PublisherStudio Audio Studio."
            };
            var preferred = new List<string>
            {
                "initial.setup.status", "initial.setup.canirun.recommendations",
                "publisher.media.capabilities", "publisher.file.formats", "publisher.browser.view.inspect",
                "publisher.panel.modules.inspect", "publisher.panel.module.invoke"
            };
            if (string.Equals(category, "image", StringComparison.Ordinal))
                preferred.Add("localai.image.generate");
            if (string.Equals(category, "video", StringComparison.Ordinal))
                preferred.Add("localai.video.generate");
            return new OrganicCouncilTeamDefinition
            {
                Key = teamKey,
                DisplayName = $"PublisherStudio {category} production",
                Purpose = $"Help an explicitly linked PublisherStudio author create and edit {category} through real installed generation providers or PublisherStudio native Studios, while accurately presenting capability and hardware-fit evidence.",
                Roles =
                [
                    new() { Role = "Provider readiness reviewer", Expertise = "configured generation providers, installed model inventories, hardware limits and attributed CanIRun.ai results", Responsibility = "identify proven available tools and models; if a model is absent ask for explicit approval before optional CanIRun.ai lookup and never fabricate scores" },
                    new() { Role = "Media author", Expertise = "creative briefs, reproducible prompts and source-asset selection", Responsibility = "prepare category-specific safe parameters and use only actually advertised/approved organic functions" },
                    new() { Role = "Studio editor", Expertise = "PublisherStudio media editor, browser view and 1-Wire typed module operations", Responsibility = "prefer available native media operations for trimming, timing, layout and other simple edits; respect session ownership" },
                    new() { Role = "Result reviewer", Expertise = "artifact identification, media validation, provenance and export parity", Responsibility = "report generated asset identifiers and verified receipts or distinguish an unexecuted proposal from a finished asset" }
                ],
                PreferredCapabilities = preferred,
                ArchitectureContracts =
                [
                    choiceInstruction,
                    "If a choice is still needed, ask in the user-visible response and stop before generation, editor input or model setup. If the user already chose a path, use it. If export is requested, use the currently advertised formats and ask for the target only when required.",
                    "Never claim a video/image/audio was created unless an actual output file or validated media receipt exists.",
                    "Do not send hardware details to CanIRun.ai without its existing fresh human approval. Review installed models and source evidence first; never hard-code model rankings.",
                    "No publication, browser or filesystem mutations without an exact scoped publisher command and its existing approval/review path."
                ],
                WorkflowSteps =
                [
                    Step($"{category}-readiness", "Verify generation readiness", 10, "Discovery", "Provider readiness reviewer",
                        $"{choiceInstruction} Use the installed-provider inventory from initial.setup.status when available. For images/videos inspect capability-bound localai.image.generate/localai.video.generate only if present and configured; audio may need a separate provider or native Audio Studio. Distinguish text-only Council models from media generators and installed tools. When none is verified, explain that absence and offer a separately approved initial.setup.canirun.recommendations lookup with reviewed hardware details; list only sourced, category-relevant results. Never assume a model from an example list is installed. User task: {{{{UserPrompt}}}}",
                        "LeaderSingle", canUseOrganicFunctions: true),
                    Step($"{category}-authoring", "Create or plan approved media", 20, "Execution", "Media author",
                        "If the user still needs to choose, carry the question into the final answer and perform no media action. After the user selects a workflow, invoke only advertised authorized generation (localai.image.generate/localai.video.generate where supported) or PublisherStudio editing operations. Use native media timelines and browser/1-Wire commands when they suffice, avoiding expensive analysis for simple trims/cuts. No autonomous installs or unapproved network calls. Report the exact returned artifacts and receipts, or provide a reviewable work plan with model readiness still pending. Task: {{UserPrompt}}\nEvidence: {{Transcript}}",
                        "LeaderSingle", canUseOrganicFunctions: true),
                    Step($"{category}-verify", "Review output and handoff", 30, "Review", "Result reviewer",
                        "If the user has not selected a workflow, present the available choices and wait for their response. Otherwise identify produced files/receipts and supported PublisherStudio insertion steps. Do not confuse a proposed prompt, source text or media editor launch with a generated asset. State outstanding approval/compatibility gaps. Task: {{UserPrompt}}\nTranscript: {{Transcript}}",
                        "LeaderSingle", producesFinalAnswer: true)
                ]
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating PublisherStudio {Category} Council seed failed.", category);
            throw;
        }
    }

    /// <summary>Creates the dedicated panel/template/OpenSCAD team using the same authorized 1-Wire module surface as exported websites.</summary>
    /// <returns>The persisted panel team seed.</returns>
    private OrganicCouncilTeamDefinition CreatePublisherPanelTeam()
    {
        try
        {
            return new OrganicCouncilTeamDefinition
            {
                Key = "publisher-panel",
                DisplayName = "PublisherStudio Panel and 3D team",
                Purpose = "Design reviewable Panel Studio/Div Studio compositions and OpenSCAD-compatible 3D objects from user prompts, then use only approved native commands to apply them.",
                Roles =
                [
                    new() { Role = "Layout architect", Expertise = "multi-view panels, reusable templates, sizing, DevExtreme module metadata and web export", Responsibility = "produce a document-native component plan with valid data bindings, page orientation and accessibility" },
                    new() { Role = "Module operation reviewer", Expertise = "1-Wire organic module discovery, typed function catalogs and DevExtreme widget actions", Responsibility = "inspect mounted modules before proposing exact field-safe commands; preserve exported-site runtime equivalence" },
                    new() { Role = "3D source author", Expertise = "bounded OpenSCAD source and PublisherStudio 3D blob interchange", Responsibility = "write reviewable 3D geometry source only when asked and never claim unverified rendering or execute arbitrary source" },
                    new() { Role = "Publication verifier", Expertise = "canonical publication state, undo, approval and standalone exports", Responsibility = "validate requested changes against actual command receipts and isolate unapproved draft templates" }
                ],
                PreferredCapabilities = ["publisher.panel.modules.inspect", "publisher.panel.module.invoke", "publisher.browser.view.inspect", "publisher.project.inspect"],
                ArchitectureContracts =
                [
                    "Only the user's active PublisherStudio session may authorize publication mutations.",
                    "Inspect current mounted module metadata before issuing an exact approved typed 1-Wire operation.",
                    "OpenSCAD code, template JSON and proposed website layouts remain reviewable drafts until PublisherStudio validates and the user approves an actual insertion."
                ],
                WorkflowSteps =
                [
                    Step("panel-discover", "Inspect panel context", 10, "Discovery", "Module operation reviewer",
                        "Use only available read-only PublisherStudio browser or panel-module inspection. Identify the precise view, configured fields, component capabilities and requested document orientation. Never infer objects merely from a screenshot. Task: {{UserPrompt}}",
                        "LeaderSingle", canUseOrganicFunctions: true),
                    Step("panel-draft", "Draft native template or 3D geometry", 20, "Authoring", "Layout architect",
                        "Draft a PublisherStudio Panel/Div view using supported component and data bindings, or bounded reviewable OpenSCAD when explicitly requested. For modifications use only the approved exact typed 1-Wire function and report its receipt; do not execute arbitrary JavaScript. Task: {{UserPrompt}}\nInspected state: {{Transcript}}",
                        "LeaderSingle", canUseOrganicFunctions: true),
                    Step("panel-review", "Review export and permission boundaries", 30, "Review", "Publication verifier",
                        "Separate proposed JSON/OpenSCAD source from committed publication state. Verify scoped approval receipts and note which features work autonomously in exported HTML. Task: {{UserPrompt}}\nTranscript: {{Transcript}}",
                        "LeaderSingle", producesFinalAnswer: true)
                ]
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating PublisherStudio panel Council seed failed.");
            throw;
        }
    }
}
