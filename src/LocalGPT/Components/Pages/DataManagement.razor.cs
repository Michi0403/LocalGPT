using LocalGPT.BusinessObjects;
using LocalGPT.Components.Shared;
using System.Globalization;

namespace LocalGPT.Components.Pages;

/// <summary>DevExpress workbench for navigating the maintained data domain and its live SQLite relationships.</summary>
public partial class DataManagement
{
    private const string ToastName = "DataManagementToasts";
    private bool Busy { get; set; }
    private bool DeleteConfirmed { get; set; }
    private bool ProtectedFieldsConfirmed { get; set; }
    private bool PreferenceSaveConfirmed { get; set; }
    private bool PreferenceIsSensitive { get; set; }
    private bool PreferenceRequiresApproval { get; set; }
    private string PreferenceKind { get; set; } = "Automatic";
    private string StatusText { get; set; } = string.Empty;
    private string ActiveGroup { get; set; } = "councils";
    private int RowLimit { get; set; } = 100;
    private int ActivePageSize { get; set; } = 100;
    private int RowOffset { get; set; }
    private string? RelationColumn { get; set; }
    private string? RelationValue { get; set; }
    private IReadOnlyList<DataManagementTableInfo> AllTables { get; set; } = [];
    private DataManagementTableInfo? SelectedTable { get; set; }
    private SqliteTableSnapshot? CurrentTable { get; set; }
    private SqliteRowSnapshot? SelectedRow { get; set; }
    private SqliteColumnSummary? SelectedPreferenceColumn { get; set; }
    private Dictionary<string, string?> EditValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, SqliteEditorFieldOverride> EditorOverrides { get; set; } =
        new Dictionary<string, SqliteEditorFieldOverride>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<HumanCollaborationRequest> ActiveModelHealthExclusions { get; set; } = [];
    private HumanCollaborationRequest? SelectedHealthExclusion { get; set; }
    private IReadOnlyList<string> BooleanOptions { get; } = ["1", "0", "[null]"];
    private IReadOnlyList<string> EditorKindOptions { get; } = ["Automatic", "Text", "LongText", "Number", "Boolean", "DateTime", "Guid", "Json", "Secret"];

    /// <summary>Projects live catalog metadata into the established vertical DevExpress navigation.</summary>
    private IReadOnlyList<WorkbenchNavItem> DomainGroups =>
    [
        new("councils", T("DataManagement.Councils", "Council & models"), T("DataManagement.CouncilsHelp", "Teams, model profiles and runtime presets"), CountGroup("councils")),
        new("approvals", T("DataManagement.Approvals", "Approvals & health"), T("DataManagement.ApprovalsHelp", "Human decisions and recoverable model exclusions"), CountGroup("approvals")),
        new("projects", T("DataManagement.Projects", "Projects & workspaces"), T("DataManagement.ProjectsHelp", "Project identities, files and revisions"), CountGroup("projects")),
        new("conversations", T("DataManagement.Conversations", "Conversations & logs"), T("DataManagement.ConversationsHelp", "Chat history and operational records"), CountGroup("conversations")),
        new("knowledge", T("DataManagement.Knowledge", "Knowledge & prompts"), T("DataManagement.KnowledgeHelp", "Learned knowledge, patterns and skills"), CountGroup("knowledge")),
        new("system", T("DataManagement.System", "System & remaining data"), T("DataManagement.SystemHelp", "All remaining persisted entities"), CountGroup("system"))
    ];

    /// <summary>Groups all live tables without omitting entities that lack declared foreign keys.</summary>
    private IReadOnlyList<DataManagementTableInfo> VisibleTables => AllTables
        .Where(table => table.GroupKey == ActiveGroup)
        .ToList();

    /// <summary>Protects EF migration bookkeeping from the generic editor.</summary>
    private bool CanEditCurrentTable => SelectedTable is not null &&
        !SelectedTable.Name.Equals("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase);

    /// <summary>Shows further pages when the live table count is known or a relationship page is full.</summary>
    private bool HasNextPage => CurrentTable is not null && CurrentTable.Rows.Count >= ActivePageSize
        && (RelationColumn is not null || (SelectedTable?.RowCount ?? 0) > RowOffset + CurrentTable.Rows.Count);

    /// <summary>Requires a fresh explicit UI confirmation when a protected persisted field is actually changed.</summary>
    private bool RequiresProtectedFieldApproval => CurrentTable is not null && CurrentTable.Columns.Any(column =>
        EditorOverrides.TryGetValue(column.Name, out var preference) && preference.RequireHumanApproval
        && (SelectedRow is null
            ? !string.IsNullOrWhiteSpace(GetEditValue(column.Name))
            : !string.Equals(SelectedRow.Values.GetValueOrDefault(column.Name), GetEditValue(column.Name), StringComparison.Ordinal)));

    /// <summary>Starts a fresh catalog read in the non-prerendered interactive server circuit.</summary>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Initializing the Data Management page failed.");
            Notifier.ShowError(ToastName, "Data Management could not initialize; inspect local logs.", "Data Management");
        }
    }

    /// <summary>Refreshes table metadata, decisions and the selected relational slice.</summary>
    private async Task ReloadAsync()
    {
        if (Busy) return;
        Busy = true;
        try
        {
            var name = SelectedTable?.Name;
            AllTables = await DataCatalog.GetTablesAsync().ConfigureAwait(true);
            ActiveModelHealthExclusions = await HumanCollaboration.GetActiveModelHealthExclusionsAsync().ConfigureAwait(true);
            SelectedHealthExclusion = ActiveModelHealthExclusions.FirstOrDefault(request => request.Id == SelectedHealthExclusion?.Id)
                ?? ActiveModelHealthExclusions.FirstOrDefault();
            SelectedTable = AllTables.FirstOrDefault(table => table.GroupKey == ActiveGroup
                && table.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?? AllTables.FirstOrDefault(table => table.GroupKey == ActiveGroup);
            ActivePageSize = Math.Clamp(RowLimit, 1, 500);
            await LoadSelectedTableCoreAsync().ConfigureAwait(true);
            StatusText = string.Format(CultureInfo.CurrentCulture, T("DataManagement.Reloaded", "Loaded {0} entities and {1} active model-health decisions."), AllTables.Count, ActiveModelHealthExclusions.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Refreshing the Data Management catalog failed.");
            Notifier.ShowError(ToastName, "Data could not be refreshed. See local logs.", "Data Management");
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>Selects one logical domain and its first available entity.</summary>
    private async Task SelectGroupAsync(string key)
    {
        if (Busy) return;
        Busy = true;
        try
        {
            ActiveGroup = key;
            SelectedTable = AllTables.FirstOrDefault(table => table.GroupKey == ActiveGroup);
            RowOffset = 0;
            ActivePageSize = Math.Clamp(RowLimit, 1, 500);
            RelationColumn = null;
            RelationValue = null;
            await LoadSelectedTableCoreAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not navigate to data domain {Domain}.", key);
            Notifier.ShowError(ToastName, "This data domain could not be opened.", "Data Management");
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>Opens the selected entity without carrying a previous table's relation filter.</summary>
    private async Task SelectTableAsync(DataManagementTableInfo table)
    {
        if (Busy) return;
        Busy = true;
        try
        {
            SelectedTable = table;
            RowOffset = 0;
            ActivePageSize = Math.Clamp(RowLimit, 1, 500);
            RelationColumn = null;
            RelationValue = null;
            await LoadSelectedTableCoreAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Opening data entity {TableName} failed.", table?.Name);
            Notifier.ShowError(ToastName, "The selected entity could not be loaded.", "Data Management");
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>Opens a child table from the schema tree without implying a record-specific relationship filter.</summary>
    private async Task OpenTableFromTreeAsync(string tableName)
    {
        if (Busy) return;
        try
        {
            var table = AllTables.FirstOrDefault(item => item.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
            if (table is null) return;
            ActiveGroup = table.GroupKey;
            await SelectTableAsync(table).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Opening a child entity from the Data Management tree failed.");
            Notifier.ShowError(ToastName, "The related entity could not be opened.", "Data Management");
        }
    }

    /// <summary>Reloads the current table or its bounded relationship slice.</summary>
    private async Task LoadSelectedTableAsync()
    {
        if (Busy) return;
        Busy = true;
        try
        {
            RowOffset = 0;
            ActivePageSize = Math.Clamp(RowLimit, 1, 500);
            await LoadSelectedTableCoreAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Reloading data entity {TableName} failed.", SelectedTable?.Name);
            Notifier.ShowError(ToastName, "Records could not be loaded.", "Data Management");
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>Loads older records without truncating the table to the latest configured page size.</summary>
    private async Task NextPageAsync()
    {
        if (Busy || !HasNextPage) return;
        Busy = true;
        try
        {
            var previousOffset = RowOffset;
            RowOffset = checked(RowOffset + ActivePageSize);
            await LoadSelectedTableCoreAsync().ConfigureAwait(true);
            if (CurrentTable is { Rows.Count: 0 })
            {
                RowOffset = previousOffset;
                await LoadSelectedTableCoreAsync().ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Loading the next Data Management record page failed.");
            Notifier.ShowError(ToastName, "Older records could not be loaded.", "Data Management");
        }
        finally { Busy = false; }
    }

    /// <summary>Returns to newer records in the same table or relational slice.</summary>
    private async Task PreviousPageAsync()
    {
        if (Busy || RowOffset <= 0) return;
        Busy = true;
        try
        {
            RowOffset = Math.Max(0, RowOffset - ActivePageSize);
            await LoadSelectedTableCoreAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Loading the previous Data Management record page failed.");
            Notifier.ShowError(ToastName, "Newer records could not be loaded.", "Data Management");
        }
        finally { Busy = false; }
    }

    /// <summary>Materializes data before rendering; the Grid never queries services from render paths.</summary>
    private async Task LoadSelectedTableCoreAsync(long? preferredRowId = null)
    {
        try
        {
            CurrentTable = null;
            SelectedRow = null;
            DeleteConfirmed = false;
            ProtectedFieldsConfirmed = false;
            EditValues.Clear();
            if (SelectedTable is null) return;

            CurrentTable = string.IsNullOrWhiteSpace(RelationColumn)
                ? await TableEditor.GetTablePageAsync(SelectedTable.Name, ActivePageSize, RowOffset).ConfigureAwait(true)
                : await TableEditor.GetRelatedRowsPageAsync(SelectedTable.Name, RelationColumn, RelationValue, ActivePageSize, RowOffset).ConfigureAwait(true);
            EditorOverrides = new Dictionary<string, SqliteEditorFieldOverride>(
                await EditorPreferences.GetOverridesAsync(SelectedTable.Name).ConfigureAwait(true), StringComparer.OrdinalIgnoreCase);
            var preferenceName = SelectedPreferenceColumn?.Name;
            SelectedPreferenceColumn = CurrentTable.Columns.FirstOrDefault(column => column.Name == preferenceName)
                ?? CurrentTable.Columns.FirstOrDefault();
            SelectPreferenceColumn(SelectedPreferenceColumn);
            if ((CurrentTable.Rows.FirstOrDefault(row => row.RowId == preferredRowId)
                ?? CurrentTable.Rows.FirstOrDefault()) is { } first)
                SelectRow(first);
            else
                NewRow();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Loading the selected Data Management table failed.");
            throw;
        }
    }

    /// <summary>Opens a live foreign-key parent or child using the currently selected record's key value.</summary>
    private async Task NavigateRelationAsync(DataManagementRelation relation, bool toParent)
    {
        if (Busy) return;
        Busy = true;
        try
        {
            var targetName = toParent ? relation.ParentTable : relation.ChildTable;
            var target = AllTables.FirstOrDefault(table => table.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
            if (target is null) throw new InvalidOperationException("Related table no longer exists.");

            var sourceColumn = toParent ? relation.ChildColumn : relation.ParentColumn;
            if (string.IsNullOrWhiteSpace(sourceColumn))
                sourceColumn = CurrentTable?.Columns.FirstOrDefault(column => column.IsPrimaryKey)?.Name ?? "Id";
            var targetColumn = toParent ? relation.ParentColumn : relation.ChildColumn;
            if (string.IsNullOrWhiteSpace(targetColumn))
            {
                var targetMetadata = await TableEditor.GetTableAsync(target.Name, 1).ConfigureAwait(true);
                targetColumn = targetMetadata.Columns.FirstOrDefault(column => column.IsPrimaryKey)?.Name
                    ?? throw new InvalidOperationException("Related table has no declared parent key.");
            }

            var sourceRow = SelectedRow;
            string? sourceValue = null;
            if (sourceRow is not null && !sourceRow.Values.TryGetValue(sourceColumn, out sourceValue))
                throw new InvalidOperationException("The selected record does not expose the required relation source key.");
            ActiveGroup = target.GroupKey;
            SelectedTable = target;
            RowOffset = 0;
            ActivePageSize = Math.Clamp(RowLimit, 1, 500);
            RelationColumn = sourceRow is null ? null : targetColumn;
            RelationValue = sourceValue;
            await LoadSelectedTableCoreAsync().ConfigureAwait(true);
            StatusText = sourceRow is null
                ? $"Opened {target.Name}. Select a record to follow a specific relationship."
                : $"Opened {target.Name} records related through {targetColumn}.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Navigating from {Source} to {Target} failed.", relation.ChildTable, relation.ParentTable);
            Notifier.ShowError(ToastName, "The selected entity relationship could not be followed.", "Data Management");
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>Removes only the current slice filter, retaining table and group selection.</summary>
    private async Task ClearRelationFilterAsync()
    {
        try
        {
            RelationColumn = null;
            RelationValue = null;
            RowOffset = 0;
            await LoadSelectedTableAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Clearing the relation filter failed.");
            Notifier.ShowError(ToastName, "The relationship filter could not be removed.", "Data Management");
        }
    }

    /// <summary>Selects one row without persisting until the user explicitly saves.</summary>
    private void SelectRow(SqliteRowSnapshot row)
    {
        try
        {
            SelectedRow = row;
            DeleteConfirmed = false;
            ProtectedFieldsConfirmed = false;
            EditValues = CurrentTable?.Columns.ToDictionary(
                column => column.Name,
                column => row.Values.TryGetValue(column.Name, out var value) ? value : null,
                StringComparer.OrdinalIgnoreCase) ?? new(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Selecting a Data Management record failed.");
            Notifier.ShowError(ToastName, "The selected record could not be opened.", "Data Management");
        }
    }

    /// <summary>Creates an unsaved editor buffer and carries the current parent key into a new child record.</summary>
    private void NewRow()
    {
        try
        {
            SelectedRow = null;
            DeleteConfirmed = false;
            ProtectedFieldsConfirmed = false;
            EditValues = CurrentTable?.Columns.ToDictionary(
                column => column.Name, _ => (string?)null, StringComparer.OrdinalIgnoreCase)
                ?? new(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(RelationColumn) && EditValues.ContainsKey(RelationColumn))
                EditValues[RelationColumn] = RelationValue;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Preparing a new Data Management record failed.");
            Notifier.ShowError(ToastName, "A new record could not be prepared.", "Data Management");
        }
    }

    /// <summary>Persists the edit through the pre-existing validated, parameterized SQLite table editor.</summary>
    private async Task SaveRowAsync()
    {
        if (Busy || !CanEditCurrentTable || CurrentTable is null || (RequiresProtectedFieldApproval && !ProtectedFieldsConfirmed)) return;
        Busy = true;
        try
        {
            var values = CurrentTable.Columns.Where(column => SelectedRow is null || !column.IsPrimaryKey)
                .ToDictionary(column => column.Name,
                    column => CouncilText.NormalizeDBNullStringValue(GetEditValue(column.Name), Logger),
                    StringComparer.OrdinalIgnoreCase);
            if (SelectedRow is null)
            {
                await TableEditor.InsertRowAsync(CurrentTable.TableName, values).ConfigureAwait(true);
                RowOffset = 0;
            }
            else
            {
                var changed = values.Where(pair => !string.Equals(
                    SelectedRow.Values.GetValueOrDefault(pair.Key), pair.Value, StringComparison.Ordinal))
                    .Select(pair => new SqliteCellUpdate { ColumnName = pair.Key, Value = pair.Value }).ToList();
                await TableEditor.UpdateRowAsync(CurrentTable.TableName, SelectedRow.RowId, changed).ConfigureAwait(true);
            }
            var retainedRowId = SelectedRow?.RowId;
            AllTables = await DataCatalog.GetTablesAsync().ConfigureAwait(true);
            SelectedTable = AllTables.FirstOrDefault(table => table.Name == CurrentTable.TableName);
            await LoadSelectedTableCoreAsync(retainedRowId).ConfigureAwait(true);
            StatusText = T("DataManagement.Saved", "Record saved.");
            Notifier.ShowSuccess(ToastName, StatusText, "Data Management");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Saving the selected Data Management record failed; field contents were omitted.");
            Notifier.ShowError(ToastName, "Record could not be saved. Review constraints and local logs.", "Data Management");
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>Deletes only the explicitly selected, separately confirmed record.</summary>
    private async Task DeleteRowAsync()
    {
        if (Busy || !CanEditCurrentTable || !DeleteConfirmed || CurrentTable is null || SelectedRow is null) return;
        Busy = true;
        try
        {
            await TableEditor.DeleteRowAsync(CurrentTable.TableName, SelectedRow.RowId).ConfigureAwait(true);
            DeleteConfirmed = false;
            AllTables = await DataCatalog.GetTablesAsync().ConfigureAwait(true);
            SelectedTable = AllTables.FirstOrDefault(table => table.Name == CurrentTable.TableName);
            await LoadSelectedTableCoreAsync().ConfigureAwait(true);
            if (CurrentTable is { Rows.Count: 0 } && RowOffset > 0)
            {
                RowOffset = Math.Max(0, RowOffset - ActivePageSize);
                await LoadSelectedTableCoreAsync().ConfigureAwait(true);
            }
            StatusText = T("DataManagement.Deleted", "Record deleted.");
            Notifier.ShowSuccess(ToastName, StatusText, "Data Management");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Deleting the selected Data Management record failed.");
            Notifier.ShowError(ToastName, "Record could not be deleted. Review related records and local logs.", "Data Management");
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>Selects a persisted one-run exclusion without changing its approval state.</summary>
    private void SelectHealthExclusion(HumanCollaborationRequest request)
    {
        try
        {
            SelectedHealthExclusion = request;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Selecting the model-health decision failed.");
            Notifier.ShowError(ToastName, "The health decision could not be selected.", "Data Management");
        }
    }

    /// <summary>Releases only the selected approved model-health decision through its owning business service.</summary>
    private async Task ReleaseHealthExclusionAsync()
    {
        if (Busy || SelectedHealthExclusion is null) return;
        Busy = true;
        try
        {
            var profile = await HumanCollaboration.GetProfileAsync().ConfigureAwait(true);
            using var scope = HumanAmbientContext.PushHumanInteraction(
                RuntimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId),
                string.IsNullOrWhiteSpace(profile.DisplayName) ? "Human User" : profile.DisplayName,
                nameof(DataManagement), SelectedHealthExclusion.CorrelationId,
                SelectedHealthExclusion.CouncilRunId);
            var released = await HumanCollaboration.ReleaseModelHealthExclusionAsync(SelectedHealthExclusion.Id).ConfigureAwait(true);
            ActiveModelHealthExclusions = await HumanCollaboration.GetActiveModelHealthExclusionsAsync().ConfigureAwait(true);
            SelectedHealthExclusion = ActiveModelHealthExclusions.FirstOrDefault();
            StatusText = released ? T("DataManagement.Released", "One-run exclusion released; this model can participate again.")
                : T("DataManagement.ReleaseGone", "The decision had already been consumed or changed.");
            Notifier.ShowSuccess(ToastName, StatusText, "Council model health");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Releasing a model-health decision failed.");
            Notifier.ShowError(ToastName, "The model-health exclusion could not be released.", "Council model health");
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>Loads one existing field's persisted editor configuration without changing business data.</summary>
    private void SelectPreferenceColumn(SqliteColumnSummary? column)
    {
        try
        {
            SelectedPreferenceColumn = column;
            PreferenceSaveConfirmed = false;
            if (column is not null && EditorOverrides.TryGetValue(column.Name, out var preference))
            {
                PreferenceKind = preference.EditorKind;
                PreferenceIsSensitive = preference.IsSensitive;
                PreferenceRequiresApproval = preference.RequireHumanApproval;
            }
            else
            {
                PreferenceKind = "Automatic";
                PreferenceIsSensitive = false;
                PreferenceRequiresApproval = false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Opening the persisted Data Management field editor preference failed.");
            Notifier.ShowError(ToastName, "Field editor preferences could not be opened.", "Data Management");
        }
    }

    /// <summary>Persists one user-confirmed field editor setting through the existing EF-backed preference service.</summary>
    private async Task SavePreferenceAsync()
    {
        if (Busy || !PreferenceSaveConfirmed || CurrentTable is null || SelectedPreferenceColumn is null) return;
        Busy = true;
        try
        {
            EditorOverrides.TryGetValue(SelectedPreferenceColumn.Name, out var existing);
            var preference = new SqliteEditorFieldOverride
            {
                TableName = CurrentTable.TableName,
                ColumnName = SelectedPreferenceColumn.Name,
                EditorKind = PreferenceKind,
                IsSensitive = PreferenceIsSensitive,
                RequireHumanApproval = PreferenceRequiresApproval,
                InputMask = existing?.InputMask ?? string.Empty,
                FormatString = existing?.FormatString ?? string.Empty,
                NullText = existing?.NullText ?? "[null]"
            };
            await EditorPreferences.SaveOverrideAsync(preference, userConfirmed: true).ConfigureAwait(true);
            EditorOverrides = new Dictionary<string, SqliteEditorFieldOverride>(
                await EditorPreferences.GetOverridesAsync(CurrentTable.TableName).ConfigureAwait(true),
                StringComparer.OrdinalIgnoreCase);
            SelectPreferenceColumn(SelectedPreferenceColumn);
            ProtectedFieldsConfirmed = false;
            StatusText = T("DataManagement.PreferenceSaved", "Field editor preference saved.");
            Notifier.ShowSuccess(ToastName, StatusText, "Data Management");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Saving the Data Management field editor preference failed.");
            Notifier.ShowError(ToastName, "The editor preference could not be saved.", "Data Management");
        }
        finally { Busy = false; }
    }

    /// <summary>Returns the current buffered value of an editor column.</summary>
    private string? GetEditValue(string name)
    {
        try { return EditValues.TryGetValue(name, out var value) ? value : null; }
        catch (Exception ex) { Logger.LogError(ex, "Reading the editor value failed."); throw; }
    }

    /// <summary>Updates a scalar field buffer without writing during a renderer event.</summary>
    private void SetEditValue(string name, string? value)
    {
        try
        {
            if (CanEditCurrentTable && (!EditValues.TryGetValue(name, out var previous) || !string.Equals(previous, value, StringComparison.Ordinal)))
            {
                EditValues[name] = value;
                ProtectedFieldsConfirmed = false;
            }
        }
        catch (Exception ex) { Logger.LogError(ex, "Updating an editor value failed."); throw; }
    }

    /// <summary>Uses persisted user editor preferences before the service's inferred control kind.</summary>
    private string GetEditorKind(SqliteColumnSummary column, string? value)
    {
        try
        {
            if (EditorOverrides.TryGetValue(column.Name, out var sensitivePreference) && sensitivePreference.IsSensitive)
                return "Secret";
            return EditorOverrides.TryGetValue(column.Name, out var preference) &&
                !preference.EditorKind.Equals("Automatic", StringComparison.OrdinalIgnoreCase)
                ? preference.EditorKind : EditorPreferences.InferEditorKind(column, value);
        }
        catch (Exception ex) { Logger.LogError(ex, "Determining an editor type failed."); throw; }
    }

    /// <summary>Counts already-loaded entities in one navigation domain.</summary>
    private string CountGroup(string key)
    {
        try { return AllTables.Count(table => table.GroupKey == key).ToString(CultureInfo.CurrentCulture); }
        catch (Exception ex) { Logger.LogError(ex, "Counting data domain entities failed."); throw; }
    }

    /// <summary>Resolves a localized page label with a defined English fallback.</summary>
    private string T(string key, string fallback)
    {
        try { return Localization.Get(key, fallback: fallback); }
        catch (Exception ex) { Logger.LogError(ex, "Resolving a Data Management translation failed."); throw; }
    }

    /// <summary>Converts SQLite's invariant decimal representation for DevExpress numeric editing.</summary>
    private decimal? ParseDecimal(string? value)
    {
        try { return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null; }
        catch (Exception ex) { Logger.LogError(ex, "Parsing a numeric editor value failed."); throw; }
    }

    /// <summary>Formats numeric values for the maintained SQLite converter.</summary>
    private string FormatDecimal(decimal? value)
    {
        try { return value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty; }
        catch (Exception ex) { Logger.LogError(ex, "Formatting a numeric editor value failed."); throw; }
    }

    /// <summary>Converts an optional SQLite date value for DevExpress date editing.</summary>
    private DateTime? ParseDate(string? value)
    {
        try { return DateTimeOffset.TryParse(value, out var parsed) ? parsed.LocalDateTime : null; }
        catch (Exception ex) { Logger.LogError(ex, "Parsing a date editor value failed."); throw; }
    }

    /// <summary>Formats an optional date for the SQLite editor's existing parser.</summary>
    private string FormatDate(DateTime? value)
    {
        try { return value?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty; }
        catch (Exception ex) { Logger.LogError(ex, "Formatting a date editor value failed."); throw; }
    }
}
