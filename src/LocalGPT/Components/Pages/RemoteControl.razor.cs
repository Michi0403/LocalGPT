using LocalGPT.BusinessObjects;
using LocalGPT.Components.Shared;
using Microsoft.AspNetCore.Components;

namespace LocalGPT.Components.Pages;

/// <summary>
/// Renders the remote control Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding LocalGPT interface.
/// </summary>
public partial class RemoteControl : ComponentBase
{
    /// <summary>
    /// Stores the navigation key for the Remote Control editor section currently visible to the user without reloading connector or pipeline state.
    /// </summary>
    /// <value>The workbench section key currently presented to the user.</value>
    private string ActiveRemoteControlSection { get; set; } = "connectors";

    /// <summary>
    /// Gets the navigation model used by the Remote Control configuration workbench.
    /// </summary>
    /// <value>The connector, pipeline, history, and template sections.</value>
    private IReadOnlyList<WorkbenchNavItem> RemoteControlSections =>
    [
        new("connectors", T("RemoteControl.Connectors", "Connectors"), T("RemoteControl.Connectors.Help", "REST/OData pulls or token-protected inbound webhooks.")),
        new("pipelines", T("RemoteControl.Pipelines", "Action pipelines"), T("RemoteControl.Pipelines.Help", "Compose existing DXFunctions or published public service methods; no raw reflection path is used.")),
        new("history", T("RemoteControl.History", "Execution history"), T("RemoteControl.History.Help", "Bounded audit metadata only; full remote payloads are not persisted here.")),
        new("templates", T("RemoteControl.TemplateLanguage", "Template language"), T("RemoteControl.TemplateLanguage.Help", "Use payload values, previous step results and LocalGPT system variables."))
    ];

    /// <summary>
    /// Changes the active Remote Control workbench section without reloading editor state.
    /// </summary>
    /// <param name="key">The selected workbench section key.</param>
    /// <returns>A completed task for the navigation callback.</returns>
    private Task OnRemoteControlSectionChanged(string key)
    {
        try
        {
            ActiveRemoteControlSection = key;
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Changing the Remote Control workbench section failed.");
            throw;
        }
    }
    /// <summary>
    /// Gets or sets the connector rows collection maintained or exposed by this remote control instance for downstream processing.
    /// </summary>
    /// <value>The connector rows value exposed by <see cref="RemoteControl"/>.</value>
    private IReadOnlyList<RemoteControlConnectorDefinition> ConnectorRows { get; set; } = [];
    /// <summary>
    /// Gets or sets the pipeline rows collection maintained or exposed by this remote control instance for downstream processing.
    /// </summary>
    /// <value>The pipeline rows value exposed by <see cref="RemoteControl"/>.</value>
    private IReadOnlyList<RemoteControlPipelineDefinition> PipelineRows { get; set; } = [];
    /// <summary>
    /// Gets or sets the history rows collection maintained or exposed by this remote control instance for downstream processing.
    /// </summary>
    /// <value>The history rows value exposed by <see cref="RemoteControl"/>.</value>
    private IReadOnlyList<RemoteControlExecutionRecord> HistoryRows { get; set; } = [];
    /// <summary>
    /// Gets or sets the target entries collection maintained or exposed by this remote control instance for downstream processing.
    /// </summary>
    /// <value>The target entries value exposed by <see cref="RemoteControl"/>.</value>
    private IReadOnlyList<DxAiFunctionCatalogEntry> TargetEntries { get; set; } = [];
    /// <summary>
    /// Gets or sets the connector edit value that forms part of the remote control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The connector edit value exposed by <see cref="RemoteControl"/>.</value>
    private RemoteControlConnectorDefinition ConnectorEdit { get; set; } = new();
    /// <summary>Gets or sets the guided allowed-host rows shown by the connector editor while preserving the persisted JSON-array contract.</summary>
    /// <value>The editable allowed-host values.</value>
    private List<string> AllowedHostRows { get; set; } = [];
    /// <summary>Gets or sets the guided request-header rows shown by the connector editor while preserving the persisted JSON-object contract.</summary>
    /// <value>The editable request-header values.</value>
    private List<RemoteControlHeaderEditRow> HeaderRows { get; set; } = [];
    /// <summary>
    /// Gets or sets the pipeline edit value that forms part of the remote control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pipeline edit value exposed by <see cref="RemoteControl"/>.</value>
    private RemoteControlPipelineDefinition PipelineEdit { get; set; } = new();
    /// <summary>
    /// Gets or sets the editing steps collection maintained or exposed by this remote control instance for downstream processing.
    /// </summary>
    /// <value>The editing steps value exposed by <see cref="RemoteControl"/>.</value>
    private List<RemoteControlPipelineStepDefinition> EditingSteps { get; set; } = [];
    /// <summary>
    /// Gets or sets the manual payload value that forms part of the remote control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The manual payload value exposed by <see cref="RemoteControl"/>.</value>
    private string ManualPayload { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the last payload preview value that forms part of the remote control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last payload preview value exposed by <see cref="RemoteControl"/>.</value>
    private string LastPayloadPreview { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last webhook token value that forms part of the remote control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last webhook token value exposed by <see cref="RemoteControl"/>.</value>
    private string LastWebhookToken { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the remote control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="RemoteControl"/>.</value>
    private string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether trigger manual applies to the remote control state.
    /// </summary>
    /// <value>The trigger manual value exposed by <see cref="RemoteControl"/>.</value>
    private bool TriggerManual { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether trigger pull applies to the remote control state.
    /// </summary>
    /// <value>The trigger pull value exposed by <see cref="RemoteControl"/>.</value>
    private bool TriggerPull { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether trigger webhook applies to the remote control state.
    /// </summary>
    /// <value>The trigger webhook value exposed by <see cref="RemoteControl"/>.</value>
    private bool TriggerWebhook { get; set; }
    /// <summary>
    /// Gets the transport kinds collection maintained or exposed by this remote control instance for downstream processing.
    /// </summary>
    /// <value>The transport kinds value exposed by <see cref="RemoteControl"/>.</value>
    private IReadOnlyList<RemoteControlTransportKind> TransportKinds { get; } = Enum.GetValues<RemoteControlTransportKind>();
    /// <summary>
    /// Gets the HTTP methods collection maintained or exposed by this remote control instance for downstream processing.
    /// </summary>
    /// <value>The HTTP methods value exposed by <see cref="RemoteControl"/>.</value>
    private IReadOnlyList<RemoteControlHttpMethod> HttpMethods { get; } = Enum.GetValues<RemoteControlHttpMethod>();
    /// <summary>
    /// Gets the response formats collection maintained or exposed by this remote control instance for downstream processing.
    /// </summary>
    /// <value>The response formats value exposed by <see cref="RemoteControl"/>.</value>
    private IReadOnlyList<RemoteControlResponseFormat> ResponseFormats { get; } = Enum.GetValues<RemoteControlResponseFormat>();

    /// <summary>
    /// Handles the initialized async lifecycle or event notification for <see cref="RemoteControl"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try
        {
            ConnectorEdit = CreateConnector();
            LoadConnectorGuidedFields();
            PipelineEdit = CreatePipeline();
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Remote Control workbench initialization failed.");
            Notifier.ShowError(nameof(RemoteControl), T("RemoteControl.LoadFailed", "The Remote Control workbench could not be loaded. See local logs."), T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Performs reload for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ReloadAsync()
    {
        try
        {
            ConnectorRows = await Connectors.ListAsync().ConfigureAwait(true);
            PipelineRows = await Pipelines.ListAsync().ConfigureAwait(true);
            HistoryRows = await Connectors.GetHistoryAsync(100).ConfigureAwait(true);
            TargetEntries = await Pipelines.ListTargetsAsync().ConfigureAwait(true);
            Status = T("RemoteControl.Ready", "Remote Control definitions are synchronized with the local database.");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Reloading the Remote Control workbench failed.");
            Notifier.ShowError(nameof(RemoteControl), T("RemoteControl.LoadFailed", "The Remote Control workbench could not be loaded. See local logs."), T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Performs new connector for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    private void NewConnector()
    {
        try
        {
            ConnectorEdit = CreateConnector();
            LoadConnectorGuidedFields();
            LastWebhookToken = string.Empty;
            LastPayloadPreview = string.Empty;
            Status = T("RemoteControl.NewConnectorReady", "New connector ready. Network access is off until you explicitly enable it.");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Preparing a new Remote Control connector failed.");
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Performs select connector for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SelectConnectorAsync(string key)
    {
        try
        {
            ConnectorEdit = await Connectors.GetAsync(key).ConfigureAwait(true) ?? CreateConnector();
            LoadConnectorGuidedFields();
            LastWebhookToken = string.Empty;
            LastPayloadPreview = ConnectorEdit.LastPayloadPreview;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Selecting Remote Control connector {ConnectorKey} failed.", key);
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>Loads the persisted connector JSON fields into the guided host and header row editors.</summary>
    private void LoadConnectorGuidedFields()
    {
        try
        {
            AllowedHostRows = JsonText.Deserialize<List<string>>(ConnectorEdit.AllowedHostsJson ?? "[]")?
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            var headers = JsonText.Deserialize<Dictionary<string, string>>(ConnectorEdit.HeadersJson ?? "{}")
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            HeaderRows = headers
                .Where(item => !string.IsNullOrWhiteSpace(item.Key))
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(item => new RemoteControlHeaderEditRow { Name = item.Key, Value = item.Value ?? string.Empty })
                .ToList();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Loading guided Remote Control host/header fields failed; the stored JSON values were left unchanged until the user edits or saves the connector.");
            AllowedHostRows = [];
            HeaderRows = [];
        }
    }

    /// <summary>Serializes guided host and header rows back to the existing connector JSON fields before persistence.</summary>
    private void ApplyConnectorGuidedFields()
    {
        try
        {
            var hosts = AllowedHostRows
                .Select(item => item?.Trim() ?? string.Empty)
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            ConnectorEdit.AllowedHostsJson = JsonText.Serialize(hosts);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in HeaderRows)
            {
                var name = row.Name?.Trim() ?? string.Empty;
                if (name.Length == 0) continue;
                headers[name] = row.Value ?? string.Empty;
            }
            ConnectorEdit.HeadersJson = JsonText.Serialize(headers);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Serializing guided Remote Control host/header fields failed.");
            throw;
        }
    }

    /// <summary>Adds an empty allowed-host row to the guided connector editor.</summary>
    private void AddAllowedHost()
    {
        try { AllowedHostRows.Add(string.Empty); }
        catch (Exception exception) { Logger.LogError(exception, "Adding a Remote Control allowed-host row failed."); throw; }
    }

    /// <summary>Removes one allowed-host row from the guided connector editor.</summary>
    /// <param name="index">Zero-based row index.</param>
    private void RemoveAllowedHost(int index)
    {
        try
        {
            if (index >= 0 && index < AllowedHostRows.Count) AllowedHostRows.RemoveAt(index);
        }
        catch (Exception exception) { Logger.LogError(exception, "Removing a Remote Control allowed-host row failed."); throw; }
    }

    /// <summary>Adds the DNS host from the URL template to the guided allowlist when it can be resolved safely.</summary>
    private void UseUrlHost()
    {
        try
        {
            var template = ConnectorEdit.UrlTemplate?.Trim() ?? string.Empty;
            if (!Uri.TryCreate(template, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
            {
                Status = T("RemoteControl.UrlHostInvalid", "Enter a valid absolute URL template before using its host.");
                return;
            }

            var host = uri.Host.Trim().TrimEnd('.');
            if (!AllowedHostRows.Any(item => string.Equals(item?.Trim().TrimEnd('.'), host, StringComparison.OrdinalIgnoreCase)))
                AllowedHostRows.Add(host);
            Status = T("RemoteControl.UrlHostAdded", "The URL host was added to the connector allowlist.");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Adding the Remote Control URL host to the guided allowlist failed.");
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>Adds an empty request-header row to the guided connector editor.</summary>
    private void AddHeader()
    {
        try { HeaderRows.Add(new RemoteControlHeaderEditRow()); }
        catch (Exception exception) { Logger.LogError(exception, "Adding a Remote Control request-header row failed."); throw; }
    }

    /// <summary>Removes one request-header row from the guided connector editor.</summary>
    /// <param name="index">Zero-based row index.</param>
    private void RemoveHeader(int index)
    {
        try
        {
            if (index >= 0 && index < HeaderRows.Count) HeaderRows.RemoveAt(index);
        }
        catch (Exception exception) { Logger.LogError(exception, "Removing a Remote Control request-header row failed."); throw; }
    }

    /// <summary>Adds or updates a request-header row by case-insensitive header name.</summary>
    /// <param name="name">Header name.</param>
    /// <param name="value">Header value or template.</param>
    private void UpsertHeader(string name, string value)
    {
        try
        {
            var row = HeaderRows.FirstOrDefault(item => string.Equals(item.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase));
            if (row is null) HeaderRows.Add(new RemoteControlHeaderEditRow { Name = name, Value = value });
            else row.Value = value;
        }
        catch (Exception exception) { Logger.LogError(exception, "Applying a Remote Control request-header preset failed."); throw; }
    }

    /// <summary>Applies an <c>Accept: application/json</c> row to the guided Remote Control connector header editor.</summary>
    private void AddAcceptJsonHeader() => UpsertHeader("Accept", "application/json");

    /// <summary>Adds the bearer-token Authorization header preset using a LocalGPT template variable.</summary>
    private void AddBearerHeader() => UpsertHeader("Authorization", "Bearer {{var:API_TOKEN}}");

    /// <summary>Adds the API-key header preset using a LocalGPT template variable.</summary>
    private void AddApiKeyHeader() => UpsertHeader("X-API-Key", "{{var:API_KEY}}");

    /// <summary>
    /// Persists connector for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveConnectorAsync()
    {
        try
        {
            ApplyConnectorGuidedFields();
            ConnectorEdit = await Connectors.SaveAsync(ConnectorEdit).ConfigureAwait(true);
            var issuedWebhookToken = ConnectorEdit.Transport == RemoteControlTransportKind.Webhook
                ? ConnectorEdit.WebhookToken
                : string.Empty;
            LastPayloadPreview = ConnectorEdit.LastPayloadPreview;
            Status = T("RemoteControl.ConnectorSaved", "Connector saved.");
            Notifier.ShowSuccess(nameof(RemoteControl), Status, T("Common.Completed", "Completed"));
            await ReloadAsync().ConfigureAwait(true);
            await SelectConnectorAsync(ConnectorEdit.Key).ConfigureAwait(true);
            LastWebhookToken = issuedWebhookToken;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Saving a Remote Control connector from the workbench failed; secret-bearing values omitted.");
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Performs pull connector for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task PullConnectorAsync()
    {
        try
        {
            var payload = await Connectors.PullAsync(ConnectorEdit.Key, runPipelines: true, automaticInvocation: false).ConfigureAwait(true);
            LastPayloadPreview = payload.RawText;
            Status = T("RemoteControl.PullCompleted", "Pull completed. Matching pipelines were dispatched through the DXFunction registry.");
            Notifier.ShowSuccess(nameof(RemoteControl), Status, T("Common.Completed", "Completed"));
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Manual Remote Control pull failed for {ConnectorKey}; endpoint and payload omitted.", ConnectorEdit.Key);
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Performs rotate webhook token for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RotateWebhookTokenAsync()
    {
        try
        {
            LastWebhookToken = await Connectors.RotateWebhookTokenAsync(ConnectorEdit.Key).ConfigureAwait(true);
            Status = T("RemoteControl.TokenRotated", "Webhook token rotated. Copy the new value now.");
            Notifier.ShowSuccess(nameof(RemoteControl), Status, T("Common.Completed", "Completed"));
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Rotating a Remote Control webhook token from the workbench failed; token omitted.");
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Deletes connector for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DeleteConnectorAsync()
    {
        try
        {
            if (await Connectors.DeleteAsync(ConnectorEdit.Key).ConfigureAwait(true))
            {
                NewConnector();
                Status = T("RemoteControl.ConnectorDeleted", "Connector and dependent pipelines deleted.");
                await ReloadAsync().ConfigureAwait(true);
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Deleting a Remote Control connector from the workbench failed.");
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Performs new pipeline for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    private void NewPipeline()
    {
        try
        {
            PipelineEdit = CreatePipeline();
            EditingSteps = [];
            TriggerManual = true;
            TriggerPull = false;
            TriggerWebhook = false;
            ManualPayload = "{}";
            Status = T("RemoteControl.NewPipelineReady", "New action pipeline ready.");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Preparing a new Remote Control pipeline failed.");
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Performs select pipeline for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SelectPipelineAsync(string key)
    {
        try
        {
            PipelineEdit = await Pipelines.GetAsync(key).ConfigureAwait(true) ?? CreatePipeline();
            EditingSteps = Pipelines.ParseSteps(PipelineEdit.StepsJson).Select(CloneStep).ToList();
            TriggerManual = (PipelineEdit.Triggers & RemoteControlTriggerKind.Manual) != 0;
            TriggerPull = (PipelineEdit.Triggers & RemoteControlTriggerKind.Pull) != 0;
            TriggerWebhook = (PipelineEdit.Triggers & RemoteControlTriggerKind.Webhook) != 0;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Selecting Remote Control pipeline {PipelineKey} failed.", key);
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Adds step for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    private void AddStep()
    {
        try
        {
            var number = EditingSteps.Count + 1;
            EditingSteps.Add(new RemoteControlPipelineStepDefinition { Key = $"step{number}", DisplayName = $"Step {number}", ArgumentsTemplateJson = "{}" });
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Adding a Remote Control pipeline step failed.");
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Removes step for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the remote control operation and used when producing its result.</param>
    private void RemoveStep(int index)
    {
        try
        {
            if (index >= 0 && index < EditingSteps.Count) EditingSteps.RemoveAt(index);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Removing a Remote Control pipeline step failed.");
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Persists pipeline for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SavePipelineAsync()
    {
        try
        {
            PipelineEdit.Triggers = BuildTriggers();
            PipelineEdit.StepsJson = JsonText.Serialize(EditingSteps);
            PipelineEdit = await Pipelines.SaveAsync(PipelineEdit).ConfigureAwait(true);
            Status = T("RemoteControl.PipelineSaved", "Action pipeline saved.");
            Notifier.ShowSuccess(nameof(RemoteControl), Status, T("Common.Completed", "Completed"));
            await ReloadAsync().ConfigureAwait(true);
            await SelectPipelineAsync(PipelineEdit.Key).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Saving a Remote Control pipeline from the workbench failed; action templates omitted.");
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Executes pipeline for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ExecutePipelineAsync()
    {
        try
        {
            var payload = new RemoteControlPayload
            {
                ConnectorKey = "manual",
                Trigger = RemoteControlTriggerKind.Manual,
                ContentType = "application/json",
                RawText = ManualPayload,
                PayloadBytes = System.Text.Encoding.UTF8.GetByteCount(ManualPayload)
            };
            payload.Json = Templates.ParseSelectedJson(payload.RawText, payload.ContentType, RemoteControlResponseFormat.Auto, string.Empty);
            var result = await Pipelines.ExecuteAsync(PipelineEdit.Key, payload, automaticInvocation: false).ConfigureAwait(true);
            Status = result.Succeeded ? T("RemoteControl.ExecutionCompleted", "Pipeline execution completed.") : result.Error;
            if (result.Succeeded) Notifier.ShowSuccess(nameof(RemoteControl), Status, T("Common.Completed", "Completed"));
            else Notifier.ShowWarning(nameof(RemoteControl), Status, T("RemoteControl.ReviewRequired", "Review required"));
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Executing a Remote Control pipeline from the workbench failed; payload omitted.");
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Deletes pipeline for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DeletePipelineAsync()
    {
        try
        {
            if (await Pipelines.DeleteAsync(PipelineEdit.Key).ConfigureAwait(true))
            {
                NewPipeline();
                Status = T("RemoteControl.PipelineDeleted", "Action pipeline deleted.");
                await ReloadAsync().ConfigureAwait(true);
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Deleting a Remote Control pipeline from the workbench failed.");
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    /// <summary>
    /// Builds triggers for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <returns>The remote control trigger kind produced by the operation.</returns>
    private RemoteControlTriggerKind BuildTriggers()
    {
        try
        {
            var value = RemoteControlTriggerKind.None;
            if (TriggerManual) value |= RemoteControlTriggerKind.Manual;
            if (TriggerPull) value |= RemoteControlTriggerKind.Pull;
            if (TriggerWebhook) value |= RemoteControlTriggerKind.Webhook;
            return value;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Building Remote Control trigger flags failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs t for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the remote control operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string T(string key, string fallback)
    {
        try { return Localization.Get(key, fallback: fallback); }
        catch (Exception exception) { Logger.LogError(exception, "Resolving Remote Control localization key {LocalizationKey} failed.", key); return fallback; }
    }

    /// <summary>
    /// Creates connector for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <returns>The remote control connector definition produced by the operation.</returns>
    private RemoteControlConnectorDefinition CreateConnector()
    {
        try
        {
            return new RemoteControlConnectorDefinition
            {
                Key = "new-connector",
                DisplayName = "New connector",
                Transport = RemoteControlTransportKind.Rest,
                Method = RemoteControlHttpMethod.Get,
                HeadersJson = "{}",
                AllowedHostsJson = "[]",
                RequestContentType = "application/json",
                IsEnabled = false,
                NetworkEnabled = false,
                PollIntervalSeconds = 0,
                TimeoutSeconds = 30,
                MaxPayloadBytes = RemoteControlLimits.DefaultMaximumPayloadBytes
            };
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Creating the Remote Control connector edit model failed.");
            throw;
        }
    }

    /// <summary>
    /// Creates pipeline for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <returns>The remote control pipeline definition produced by the operation.</returns>
    private RemoteControlPipelineDefinition CreatePipeline()
    {
        try
        {
            return new RemoteControlPipelineDefinition
            {
                Key = "new-pipeline",
                DisplayName = "New pipeline",
                Triggers = RemoteControlTriggerKind.Manual,
                StepsJson = "[]",
                IsEnabled = false
            };
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Creating the Remote Control pipeline edit model failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs clone step for <see cref="RemoteControl"/>, keeping the operation consistent with the state and invariants of the surrounding remote control workflow.
    /// </summary>
    /// <param name="source">Source value supplied to the remote control operation and used when producing its result.</param>
    /// <returns>The remote control pipeline step definition produced by the operation.</returns>
    private RemoteControlPipelineStepDefinition CloneStep(RemoteControlPipelineStepDefinition source)
    {
        try
        {
            return new RemoteControlPipelineStepDefinition
            {
                Key = source.Key,
                DisplayName = source.DisplayName,
                TargetCatalogKey = source.TargetCatalogKey,
                FunctionName = source.FunctionName,
                ArgumentsTemplateJson = source.ArgumentsTemplateJson,
                ContinueOnFailure = source.ContinueOnFailure
            };
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Cloning a Remote Control pipeline step for editing failed.");
            throw;
        }
    }

    /// <summary>Represents one editable request-header row in the guided Remote Control connector editor.</summary>
    private sealed class RemoteControlHeaderEditRow
    {
        /// <summary>Stores the editable HTTP request-header name serialized into the connector header dictionary.</summary>
        /// <value>The user-entered header name, or an empty string for a new row.</value>
        public string Name { get; set; } = string.Empty;
        /// <summary>Stores the editable HTTP request-header value, including supported LocalGPT template variables.</summary>
        /// <value>The user-entered header value or template expression.</value>
        public string Value { get; set; } = string.Empty;
    }

}
