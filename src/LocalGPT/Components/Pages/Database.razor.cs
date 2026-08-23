using LocalGPT.BusinessObjects;
using LocalGPT.Components.Shared;
using Microsoft.AspNetCore.Components;

namespace LocalGPT.Components.Pages;

/// <summary>
/// Renders the database Razor component and coordinates the component-local state, relationship editors, commands,
/// and responsive workbench presentation used by the surrounding LocalGPT interface.
/// </summary>
public partial class Database
{
    /// <summary>Stores the active database workbench section without mixing the knowledge and generic table editors.</summary>
    /// <value>The active workbench section key.</value>
    private string ActiveDatabaseSection { get; set; } = "knowledge";

    /// <summary>Stores the regex patterns available for explicit knowledge recognition relationships.</summary>
    /// <value>The ordered regex pattern choices.</value>
    private IReadOnlyList<RegexPattern> RelationshipRegexPatterns { get; set; } = [];

    /// <summary>Stores the regex pattern currently selected for a relationship edit.</summary>
    /// <value>The selected regex pattern, or <see langword="null"/> when no pattern is selected.</value>
    private RegexPattern? SelectedRelationshipRegexPattern { get; set; }

    /// <summary>Stores the existing regex relationships for the selected knowledge entry.</summary>
    /// <value>The selected knowledge entry's persisted regex relationships.</value>
    private IReadOnlyList<CouncilKnowledgeRegexPatternLink> KnowledgeRegexRelationships { get; set; } = [];

    /// <summary>Stores the semantic role entered for the selected regex relationship.</summary>
    /// <value>The relationship purpose.</value>
    private string KnowledgeRegexPurpose { get; set; } = "Classification";

    /// <summary>Stores the human-readable meaning entered for the selected regex relationship.</summary>
    /// <value>The relationship meaning.</value>
    private string KnowledgeRegexMeaning { get; set; } = string.Empty;

    /// <summary>Stores whether the edited regex relationship should participate in recognition workflows.</summary>
    /// <value><see langword="true"/> when the relationship is enabled.</value>
    private bool KnowledgeRegexEnabled { get; set; } = true;

    /// <summary>Stores temporary caller-supplied text used to test the selected knowledge entry's enabled recognition links.</summary>
    /// <value>The non-persisted recognition test text.</value>
    private string KnowledgeRecognitionTestText { get; set; } = string.Empty;

    /// <summary>Stores successful recognition matches from the most recent non-persisted relationship test.</summary>
    /// <value>The most recent recognition matches.</value>
    private IReadOnlyList<KnowledgeRegexRecognitionMatch> KnowledgeRecognitionMatches { get; set; } = [];

    /// <summary>Stores durable and transient project choices, ordered so long-lived projects are easier to reach.</summary>
    /// <value>The available project summaries.</value>
    private IReadOnlyList<LocalGptProjectSummary> RelationshipProjects { get; set; } = [];

    /// <summary>Stores the project currently selected for topic-to-knowledge linking.</summary>
    /// <value>The selected project summary.</value>
    private LocalGptProjectSummary? SelectedRelationshipProject { get; set; }

    /// <summary>Stores the topics belonging to the relationship project currently selected by the user.</summary>
    /// <value>The project topic choices.</value>
    private IReadOnlyList<LocalGptProjectTopic> RelationshipProjectTopics { get; set; } = [];

    /// <summary>Tracks the concrete durable project topic that will receive the selected Council knowledge relationship.</summary>
    /// <value>The selected project topic.</value>
    private LocalGptProjectTopic? SelectedRelationshipProjectTopic { get; set; }

    /// <summary>Stores the existing project/topic relationships for the selected knowledge entry.</summary>
    /// <value>The selected knowledge entry's persisted project/topic relationships.</value>
    private IReadOnlyList<KnowledgeProjectTopicLinkSummary> KnowledgeProjectRelationships { get; set; } = [];

    /// <summary>Stores the explicit reason entered for a project/topic knowledge relationship.</summary>
    /// <value>The relationship reason.</value>
    private string ProjectKnowledgeLinkReason { get; set; } = "Manually linked from the SQLite Database knowledge editor.";

    /// <summary>Gets the semantic purposes offered by the knowledge-to-regex relationship editor.</summary>
    /// <value>The maintained relationship purpose choices.</value>
    private IReadOnlyList<string> RegexRelationshipPurposeOptions { get; } =
        ["Alias", "Classification", "Extraction", "Validation", "Routing", "Identifier", "Structure"];

    /// <summary>Builds the two-panel database navigation model together with live knowledge and table count badges.</summary>
    /// <value>The knowledge and SQLite-table workbench sections.</value>
    private IReadOnlyList<WorkbenchNavItem> DatabaseSections =>
    [
        new("knowledge", "Knowledge & relationships", "Grounded Council knowledge, project topics and regex meaning", KnowledgeEntries.Count.ToString()),
        new("tables", "SQLite tables", "Inspect and edit live database rows with semantic record selection", Tables.Count.ToString())
    ];

    /// <summary>Changes the active database workbench panel without discarding either editor's state.</summary>
    /// <param name="key">The selected workbench section key.</param>
    /// <returns>A completed task for the navigation callback.</returns>
    private Task OnDatabaseSectionChanged(string key)
    {
        try
        {
            ActiveDatabaseSection = key;
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Changing the SQLite Database workbench section failed.");
            Notifier.ShowError(toastName, "The requested database section could not be opened. See local logs for details.", "Database navigation");
            return Task.CompletedTask;
        }
    }

    /// <summary>Selects a knowledge entry and refreshes its structured project and regex relationships.</summary>
    /// <param name="entry">The knowledge entry selected by the user.</param>
    /// <returns>A task that completes when the relationship sidecars match the selected entry.</returns>
    private async Task SelectKnowledgeEntryAsync(CouncilKnowledgeEntry entry)
    {
        try
        {
            SelectKnowledgeEntry(entry);
            await RefreshSelectedKnowledgeRelationshipsAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Selecting a Council knowledge entry and its relationships failed.");
            Notifier.ShowError(toastName, "The knowledge entry could not be fully selected. See local logs for details.", "Knowledge selection");
        }
    }

    /// <summary>Loads shared regex and project choices used by the relationship editors.</summary>
    /// <returns>A task that completes when the relationship catalogs are ready for presentation.</returns>
    private async Task LoadRelationshipCatalogsAsync()
    {
        try
        {
            RelationshipRegexPatterns = (await RegexPatterns.ListAllAsync(1000).ConfigureAwait(true))
                .OrderBy(pattern => pattern.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            RelationshipProjects = (await ProjectService.GetProjectsAsync(includeArchived: false).ConfigureAwait(true))
                .OrderBy(project => string.Equals(project.CurrentVersion, "council-run", StringComparison.OrdinalIgnoreCase))
                .ThenBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(project => project.UpdatedAtUtc)
                .ToList();

            SelectedRelationshipRegexPattern = RelationshipRegexPatterns.FirstOrDefault();
            SelectedRelationshipProject ??= RelationshipProjects.FirstOrDefault(project =>
                !string.Equals(project.CurrentVersion, "council-run", StringComparison.OrdinalIgnoreCase));
            SelectedRelationshipProject ??= RelationshipProjects.FirstOrDefault();
            if (SelectedRelationshipProject is not null)
                await LoadRelationshipProjectTopicsAsync(SelectedRelationshipProject).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Loading database knowledge relationship catalogs failed.");
            Notifier.ShowWarning(toastName, "Knowledge is available, but project or regex relationship choices could not be fully loaded.", "Relationship catalog");
        }
    }

    /// <summary>Refreshes all structured relationships for the knowledge entry currently selected by the user.</summary>
    /// <returns>A task that completes when both relationship lists have been refreshed.</returns>
    private async Task RefreshSelectedKnowledgeRelationshipsAsync()
    {
        try
        {
            if (SelectedKnowledgeEntry is null)
            {
                KnowledgeRegexRelationships = [];
                KnowledgeProjectRelationships = [];
                KnowledgeRecognitionMatches = [];
                return;
            }

            KnowledgeRegexRelationships = await KnowledgeRegexLinks
                .GetForKnowledgeAsync(SelectedKnowledgeEntry.Id)
                .ConfigureAwait(true);
            KnowledgeProjectRelationships = await ProjectService
                .GetKnowledgeLinksAsync(SelectedKnowledgeEntry.Id)
                .ConfigureAwait(true);
            KnowledgeRecognitionMatches = [];
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Refreshing structured relationships for knowledge {KnowledgeEntryId} failed.", SelectedKnowledgeEntry?.Id);
            Notifier.ShowWarning(toastName, "The knowledge note loaded, but one or more relationship lists could not be refreshed.", "Knowledge relationships");
        }
    }

    /// <summary>Loads the topics belonging to the project selected by the relationship editor.</summary>
    /// <param name="project">The selected project summary.</param>
    /// <returns>A task that completes when the topic selector reflects the selected project.</returns>
    private async Task LoadRelationshipProjectTopicsAsync(LocalGptProjectSummary project)
    {
        try
        {
            SelectedRelationshipProject = project;
            var details = await ProjectService.GetProjectAsync(project.Id).ConfigureAwait(true);
            RelationshipProjectTopics = details?.Topics
                .OrderBy(topic => topic.Name, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
            SelectedRelationshipProjectTopic = RelationshipProjectTopics.FirstOrDefault();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Loading project topics for database relationship editing failed for {ProjectId}.", project.Id);
            RelationshipProjectTopics = [];
            SelectedRelationshipProjectTopic = null;
            Notifier.ShowWarning(toastName, "The selected project's topics could not be loaded. See local logs for details.", "Project topics");
        }
    }

    /// <summary>Copies one existing regex relationship into the editable relationship controls.</summary>
    /// <param name="link">The persisted relationship to edit.</param>
    private void EditKnowledgeRegexRelationship(CouncilKnowledgeRegexPatternLink link)
    {
        try
        {
            SelectedRelationshipRegexPattern = RelationshipRegexPatterns.FirstOrDefault(pattern => pattern.Id == link.RegexPatternId);
            KnowledgeRegexPurpose = link.LinkPurpose;
            KnowledgeRegexMeaning = link.Meaning;
            KnowledgeRegexEnabled = link.IsEnabled;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Preparing a knowledge-to-regex relationship edit failed.");
            Notifier.ShowError(toastName, "The regex relationship could not be prepared for editing.", "Regex relationship");
        }
    }

    /// <summary>Tests enabled recognition links for the selected knowledge entry without persisting the caller-supplied text.</summary>
    /// <returns>A task that completes when the transient recognition results are ready for display.</returns>
    private async Task TestKnowledgeRecognitionAsync()
    {
        try
        {
            if (SelectedKnowledgeEntry is null || string.IsNullOrWhiteSpace(KnowledgeRecognitionTestText))
            {
                KnowledgeRecognitionMatches = [];
                return;
            }

            await RunUiActionAsync(async () =>
            {
                KnowledgeRecognitionMatches = await KnowledgeRegexLinks
                    .TestRecognitionAsync(SelectedKnowledgeEntry.Id, KnowledgeRecognitionTestText)
                    .ConfigureAwait(true);
                statusText = KnowledgeRecognitionMatches.Count == 0
                    ? "No enabled recognition pattern matched the test text."
                    : $"Matched {KnowledgeRecognitionMatches.Count} knowledge recognition pattern(s).";
            }, nameof(TestKnowledgeRecognitionAsync)).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Testing the selected knowledge entry's regex recognition links failed; test text was omitted from logs.");
            Notifier.ShowError(toastName, "The recognition test could not be completed. See local logs for details.", "Recognition test");
        }
    }

    /// <summary>Creates or updates the selected knowledge-to-regex relationship after explicit user action.</summary>
    /// <returns>A task that completes when the relationship has been persisted and refreshed.</returns>
    private async Task SaveKnowledgeRegexRelationshipAsync()
    {
        try
        {
            if (SelectedKnowledgeEntry is null || SelectedRelationshipRegexPattern is null)
                return;

            await RunUiActionAsync(async () =>
            {
                await KnowledgeRegexLinks.SaveAsync(new SaveKnowledgeRegexPatternLinkRequest
                {
                    KnowledgeEntryId = SelectedKnowledgeEntry.Id,
                    RegexPatternId = SelectedRelationshipRegexPattern.Id,
                    LinkPurpose = KnowledgeRegexPurpose,
                    Meaning = KnowledgeRegexMeaning,
                    IsEnabled = KnowledgeRegexEnabled,
                    UserConfirmed = true
                }).ConfigureAwait(true);
                await RefreshSelectedKnowledgeRelationshipsAsync().ConfigureAwait(true);
                statusText = $"Linked {SelectedRelationshipRegexPattern.Name} to {SelectedKnowledgeEntry.Topic}.";
                Notifier.ShowSuccess(toastName, "Knowledge recognition relationship saved.", "Regex relationship saved");
            }, nameof(SaveKnowledgeRegexRelationshipAsync)).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Saving a knowledge-to-regex relationship failed.");
            Notifier.ShowError(toastName, "The regex relationship could not be saved. See local logs for details.", "Regex relationship");
        }
    }

    /// <summary>Removes one selected knowledge-to-regex relationship after explicit user action.</summary>
    /// <param name="link">The persisted relationship to remove.</param>
    /// <returns>A task that completes when the relationship list has been refreshed.</returns>
    private async Task RemoveKnowledgeRegexRelationshipAsync(CouncilKnowledgeRegexPatternLink link)
    {
        try
        {
            await RunUiActionAsync(async () =>
            {
                await KnowledgeRegexLinks.DeleteAsync(link.KnowledgeEntryId, link.RegexPatternId, userConfirmed: true).ConfigureAwait(true);
                await RefreshSelectedKnowledgeRelationshipsAsync().ConfigureAwait(true);
                statusText = "Knowledge recognition relationship removed.";
                Notifier.ShowSuccess(toastName, statusText, "Regex relationship removed");
            }, nameof(RemoveKnowledgeRegexRelationshipAsync)).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Removing a knowledge-to-regex relationship failed.");
            Notifier.ShowError(toastName, "The regex relationship could not be removed. See local logs for details.", "Regex relationship");
        }
    }

    /// <summary>Links the selected knowledge entry to the selected durable project topic after explicit user action.</summary>
    /// <returns>A task that completes when the project relationship has been persisted and refreshed.</returns>
    private async Task LinkKnowledgeToProjectTopicAsync()
    {
        try
        {
            if (SelectedKnowledgeEntry is null || SelectedRelationshipProjectTopic is null)
                return;

            await RunUiActionAsync(async () =>
            {
                await ProjectService.LinkKnowledgeAsync(
                    SelectedRelationshipProjectTopic.Id,
                    new LinkProjectTopicKnowledgeRequest
                    {
                        KnowledgeEntryId = SelectedKnowledgeEntry.Id,
                        LinkReason = ProjectKnowledgeLinkReason,
                        UserConfirmed = true
                    }).ConfigureAwait(true);
                await RefreshSelectedKnowledgeRelationshipsAsync().ConfigureAwait(true);
                statusText = $"Linked knowledge to project topic {SelectedRelationshipProjectTopic.Name}.";
                Notifier.ShowSuccess(toastName, statusText, "Project knowledge linked");
            }, nameof(LinkKnowledgeToProjectTopicAsync)).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Linking Council knowledge to a project topic failed.");
            Notifier.ShowError(toastName, "The project/topic relationship could not be saved. See local logs for details.", "Project knowledge");
        }
    }

    /// <summary>Removes one selected knowledge-to-project-topic relationship after explicit user action.</summary>
    /// <param name="link">The persisted relationship to remove.</param>
    /// <returns>A task that completes when the project relationship list has been refreshed.</returns>
    private async Task RemoveKnowledgeProjectRelationshipAsync(KnowledgeProjectTopicLinkSummary link)
    {
        try
        {
            await RunUiActionAsync(async () =>
            {
                await ProjectService.UnlinkKnowledgeAsync(
                    link.ProjectTopicId,
                    link.KnowledgeEntryId,
                    userConfirmed: true).ConfigureAwait(true);
                await RefreshSelectedKnowledgeRelationshipsAsync().ConfigureAwait(true);
                statusText = "Project/topic knowledge relationship removed.";
                Notifier.ShowSuccess(toastName, statusText, "Project knowledge unlinked");
            }, nameof(RemoveKnowledgeProjectRelationshipAsync)).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Removing a Council knowledge project/topic relationship failed.");
            Notifier.ShowError(toastName, "The project/topic relationship could not be removed. See local logs for details.", "Project knowledge");
        }
    }
}
