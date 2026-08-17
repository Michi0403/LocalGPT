using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Provides the maintained AI-guided first-run/runtime-setup Council blueprint.</summary>
public sealed partial class OrganicCouncilBlueprintSeedDataService
{
    /// <summary>Creates the Council team that guides hardware discovery, optional attributed compatibility lookup, provider/model bootstrap and initial benchmark preparation through maintained DXFunctions.</summary>
    /// <returns>The source-controlled initial-setup Council template.</returns>
    private OrganicCouncilTeamDefinition CreateInitialSetupAssistantTeam()
    {
        try
        {
            return new OrganicCouncilTeamDefinition
            {
                Key = "initial-setup-assistant",
                DisplayName = "AI-guided Initial Setup Team",
                Purpose = "Guides the user through LocalGPT hardware evidence, optional attributed compatibility lookup, local provider/model bootstrap and a hardware-curated benchmark team while every consequential action remains user-confirmed.",
                Roles =
                [
                    new() { Role = "Hardware evidence curator", Expertise = "multi-host GPU/VRAM evidence, local probes and HWiNFO reports", Responsibility = "separate physical hosts, collect reviewable accelerator facts and never merge unrelated endpoints into one machine" },
                    new() { Role = "Provider and runtime setup guide", Expertise = "knowledge-backed Ollama/LM Studio bootstrap profiles and LocalGPT provider endpoints", Responsibility = "detect existing local runtimes first and propose installs, starts or endpoint registration only through confirmation-gated DXFunctions" },
                    new() { Role = "Model capability curator", Expertise = "installed provider-qualified models, optional CanIRun.ai evidence and model-quality tradeoffs", Responsibility = "keep web lookup opt-in, attribute external evidence and prefer stronger installed models for curator/reviewer work" },
                    new() { Role = "Benchmark configuration curator", Expertise = "LocalGPT benchmark teams, hardware presets and Council role pools", Responsibility = "turn reviewed hardware and installed models into a user-owned initial benchmark configuration without overwriting unrelated teams" }
                ],
                PreferredCapabilities =
                [
                    "initial.setup.status",
                    "initial.setup.hardware.detect",
                    "initial.setup.hardware.hwinfo.import",
                    "initial.setup.hardware.save",
                    "initial.setup.canirun.recommendations",
                    "initial.setup.provider.list",
                    "initial.setup.provider.detect",
                    "initial.setup.provider.models.list",
                    "initial.setup.provider.install",
                    "initial.setup.provider.start",
                    "initial.setup.provider.configure",
                    "initial.setup.provider.model.install",
                    "initial.setup.benchmark.team.create",
                    "human.collaboration.request",
                    "toolchain.installation.list",
                    "localgpt.console.history"
                ],
                ExpertPreparationPromptTemplate = """
You are preparing LocalGPT's AI-guided initial setup. Begin with initial.setup.status and use only the evidence it returns. If hardware evidence is missing or ambiguous, ask the user through human.collaboration.request whether to use local detection, a supplied HWiNFO text/file report, or a manually reviewed list. Treat each endpoint/physical host separately. Never contact CanIRun.ai unless the user explicitly opts in after seeing the attribution. Detect an existing local provider and its model store before proposing installation. Every install, start, endpoint-registration, model-download, hardware-save or benchmark-team mutation must use its maintained confirmation-gated DXFunction; do not replace those actions with prose or an unreviewed generic shell command.

User request:
{{UserPrompt}}
""",
                LeaderSynthesisPromptTemplate = """
You lead the LocalGPT initial-setup workflow. Produce the smallest next-action plan from the preparation evidence. Use human.collaboration.request with suggestedResponses for hardware source, optional CanIRun.ai opt-in, provider choice and model/benchmark choices. Prefer already installed providers and models. When external compatibility evidence is requested, clearly credit CanIRun.ai and keep its result advisory. After a provider is locally available, list its models, install only user-selected missing models, register the loopback endpoint through the maintained provider configuration function, then create/refresh the hardware-curated benchmark team. Stronger installed models should be preferred for curator/director/reviewer work; small models remain valid benchmark subjects. Do not invent successful installation, startup or benchmark results.

Preparation:
{{Preparation}}
Original request:
{{UserPrompt}}
""",
                MainRoundInstructionTemplate = "Review the current setup state from your role and invoke only the maintained setup/coordination functions needed for the next bounded step. Keep Windows, Linux and macOS paths distinct, preserve multi-host hardware identity, and stop at every fresh human-confirmation boundary.",
                ArchitectureContracts =
                [
                    .. DefaultArchitectureContracts(),
                    "The setup team is offline-first. CanIRun.ai is optional, attributed and invoked only after explicit user opt-in.",
                    "Hardware is a list of accelerators grouped by physical endpoint/host; GPUs from different AI hosts must never be collapsed into one machine profile.",
                    "Provider commands are knowledge-owned and execute only through the bounded LocalGPT console service; generic shell access is not a substitute for maintained setup functions.",
                    "Consequential installation/start/download/configuration/save operations retain their DXFunction human-approval policy.",
                    "Curator/director/reviewer model pools prefer stronger installed models while the broader selected pool remains available for benchmark coverage."
                ]
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the AI-guided initial-setup Council template failed.");
            throw;
        }
    }
}
