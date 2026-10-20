using LocalGPT.BusinessObjects;
using LocalGPT.Components.Shared;

namespace LocalGPT.Components.Pages
{
    /// <summary>Exposes user-owned Council receipt recovery and follow-on team policies through the DevExpress team editor.</summary>
    public partial class CouncilTeams
    {
        /// <summary>Offers enabled follow-on teams without losing a saved but currently unavailable team key.</summary>
        /// <value>Available target teams and an explicit off setting.</value>
        private IReadOnlyList<LocalGptSelectionOption<string>> FollowOnTeamOptions
        {
            get
            {
                try
                {
                    var options = new List<LocalGptSelectionOption<string>> { new(string.Empty, "No follow-on team") };
                    if (!string.IsNullOrWhiteSpace(_editor.FollowOn.TeamKey) &&
                        !_teams.Any(team => team.Key.Equals(_editor.FollowOn.TeamKey, StringComparison.OrdinalIgnoreCase)))
                        options.Add(new(_editor.FollowOn.TeamKey, $"Saved but unavailable · {_editor.FollowOn.TeamKey}"));
                    options.AddRange(_teams.Where(team => team.IsEnabled &&
                        !string.Equals(team.Key, _editor.Key, StringComparison.OrdinalIgnoreCase))
                        .Select(team => new LocalGptSelectionOption<string>(team.Key, $"{team.DisplayName} · {team.Key}")));
                    return options;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Could not list configured follow-on Council teams.");
                    throw;
                }
            }
        }

        /// <summary>Offers only the actual required completion receipts configured on the team.</summary>
        /// <value>Required function options, including a clear option while editing.</value>
        private IReadOnlyList<LocalGptSelectionOption<string>> RecoveryRequiredFunctionOptions
        {
            get
            {
                try
                {
                    return [new(string.Empty, "Choose required function"),
                        .. _editor.RequiredCompletedFunctions.Select(name =>
                            new LocalGptSelectionOption<string>(name, name))];
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Could not enumerate user-configured required Council functions.");
                    throw;
                }
            }
        }

        /// <summary>Offers enabled function-capable steps to execute a recovery rule.</summary>
        /// <value>Workflow steps that may request configured registered functions.</value>
        private IReadOnlyList<LocalGptSelectionOption<string>> RecoveryWorkflowStepOptions
        {
            get
            {
                try
                {
                    return [new(string.Empty, "Choose workflow step"),
                        .. _editor.WorkflowSteps.Where(step => step.IsEnabled && step.CanUseOrganicFunctions)
                            .Select(step => new LocalGptSelectionOption<string>(step.Key, step.DisplayName))];
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Could not enumerate function-capable Council workflow steps.");
                    throw;
                }
            }
        }

        /// <summary>Adds an editable, initially disabled recovery rule to the current team.</summary>
        private void AddCompletionRecoveryRule()
        {
            try
            {
                _editor.CompletionRecovery.Rules.Add(new CouncilRequiredFunctionRecoveryRule());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not add Council completion recovery rule.");
                throw;
            }
        }

        /// <summary>Removes a specific user-selected completion recovery rule.</summary>
        /// <param name="rule">Rule selected in the team's editor.</param>
        private void RemoveCompletionRecoveryRule(CouncilRequiredFunctionRecoveryRule rule)
        {
            try
            {
                _editor.CompletionRecovery.Rules.Remove(rule);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not remove Council completion recovery rule.");
                throw;
            }
        }

        /// <summary>Applies the edited list of required successful receipts to one rule.</summary>
        /// <param name="rule">Rule being edited.</param>
        /// <param name="text">Newline-separated function names.</param>
        private void SetRecoveryRulePrerequisites(CouncilRequiredFunctionRecoveryRule rule, string text)
        {
            try
            {
                rule.PrerequisiteFunctions = CouncilText.ParseUserEditableNameList(text ?? string.Empty);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not update Council recovery prerequisites.");
                throw;
            }
        }
        /// <summary>Updates the configured source excerpt selection priorities through the shared text parser.</summary>
        /// <param name="step">Workflow step being edited.</param>
        /// <param name="text">User-authored priority terms, one per line.</param>
        private void SetWorkspaceSourcePriorityTerms(CouncilWorkflowStepDefinition step, string text)
        {
            try
            {
                step.WorkspaceSourcePriorityTerms = CouncilText.ParseUserEditableNameList(text ?? string.Empty);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not update the Council workspace source priorities.");
                throw;
            }
        }

    }
}
