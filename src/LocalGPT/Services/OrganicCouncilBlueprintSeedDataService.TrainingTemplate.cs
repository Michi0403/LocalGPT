using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

public sealed partial class OrganicCouncilBlueprintSeedDataService
{
    private OrganicCouncilTeamDefinition CreateNeuralTrainingTeam()
    {
        try
        {
            return new()
            {
                Key = "neural-training",
                DisplayName = "Neural Training Council",
                Purpose = "Review measured gaps, guide Nursing and model training, and evaluate a revision against held-out tasks.",
                Roles =
                [
                    new() { Role = "Evidence curator", Expertise = "provider-qualified benchmarks and Council recovery", Responsibility = "Identify reproducible gaps for an exact model and task." },
                    new() { Role = "Source curator", Expertise = "verified technical sources and data splits", Responsibility = "Plan source-backed, approved training and independent evaluation samples." },
                    new() { Role = "Training reviewer", Expertise = "adapter compatibility and comparative evaluation", Responsibility = "Review evidence and propose an exact training job or a targeted Nursing action." }
                ],
                PreferredCapabilities = ["localgpt.training.gaps.list", "localgpt.knowledge.list", "localgpt.training.knowledge.prepare", "localgpt.training.probe", "localgpt.training.run"],
                AllowedAutomaticFunctions = ["localgpt.training.gaps.list", "localgpt.knowledge.list"],
                WorkflowSteps =
                [
                    Step("training-evidence", "Review measured model gaps", 10, "Evidence", "Evidence curator",
                        "Use saved benchmark and Council outcome evidence to name an exact provider and model, relevant role, and reproducible failures. Distinguish infrastructure failures from answer quality. Mark missing measurements explicitly.",
                        "LeaderSingle", canUseOrganicFunctions: true, includePriorTranscript: false, allowedAutomaticFunctions: ["localgpt.training.gaps.list"]),
                    Step("training-sources", "Select source revisions and held-out cases", 20, "Sources", "Source curator",
                        "Review the preceding evidence. Find relevant user-verified source revisions in knowledge, preserving their provenance. Specify separately which examples teach the task and which examples will test transfer. Propose the exact dataset only when independent evaluation is possible.",
                        "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["localgpt.knowledge.list"]),
                    Step("training-review", "Review the training or Nursing proposal", 30, "Review", "Training reviewer",
                        "Review the evidence, source revisions, base checkpoint and intended provider. Prefer targeted context or tool guidance for fixable workflow gaps. Request an exact approved training job only for documented gaps and a compatible base checkpoint. Record what the base and candidate must demonstrate on the same held-out checks before proposing a Council default.",
                        "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["localgpt.training.gaps.list", "localgpt.knowledge.list"])
                ]
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the Neural Training Council seed failed.");
            throw;
        }
    }
}
