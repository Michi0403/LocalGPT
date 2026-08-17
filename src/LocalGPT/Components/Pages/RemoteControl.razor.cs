using LocalGPT.BusinessObjects;
using Microsoft.AspNetCore.Components;

namespace LocalGPT.Components.Pages;

/// <summary>Hosts the user-owned Remote Control connector and action-pipeline workbench.</summary>
public partial class RemoteControl : ComponentBase
{
    private IReadOnlyList<RemoteControlConnectorDefinition> ConnectorRows { get; set; } = [];
    private IReadOnlyList<RemoteControlPipelineDefinition> PipelineRows { get; set; } = [];
    private IReadOnlyList<RemoteControlExecutionRecord> HistoryRows { get; set; } = [];
    private IReadOnlyList<DxAiFunctionCatalogEntry> TargetEntries { get; set; } = [];
    private RemoteControlConnectorDefinition ConnectorEdit { get; set; } = new();
    private RemoteControlPipelineDefinition PipelineEdit { get; set; } = new();
    private List<RemoteControlPipelineStepDefinition> EditingSteps { get; set; } = [];
    private string ManualPayload { get; set; } = "{}";
    private string LastPayloadPreview { get; set; } = string.Empty;
    private string LastWebhookToken { get; set; } = string.Empty;
    private string Status { get; set; } = string.Empty;
    private bool TriggerManual { get; set; } = true;
    private bool TriggerPull { get; set; }
    private bool TriggerWebhook { get; set; }
    private IReadOnlyList<RemoteControlTransportKind> TransportKinds { get; } = Enum.GetValues<RemoteControlTransportKind>();
    private IReadOnlyList<RemoteControlHttpMethod> HttpMethods { get; } = Enum.GetValues<RemoteControlHttpMethod>();
    private IReadOnlyList<RemoteControlResponseFormat> ResponseFormats { get; } = Enum.GetValues<RemoteControlResponseFormat>();

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try
        {
            ConnectorEdit = CreateConnector();
            PipelineEdit = CreatePipeline();
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Remote Control workbench initialization failed.");
            Notifier.ShowError(nameof(RemoteControl), T("RemoteControl.LoadFailed", "The Remote Control workbench could not be loaded. See local logs."), T("Common.Error", "Error"));
        }
    }

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

    private void NewConnector()
    {
        try
        {
            ConnectorEdit = CreateConnector();
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

    private async Task SelectConnectorAsync(string key)
    {
        try
        {
            ConnectorEdit = await Connectors.GetAsync(key).ConfigureAwait(true) ?? CreateConnector();
            LastWebhookToken = string.Empty;
            LastPayloadPreview = ConnectorEdit.LastPayloadPreview;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Selecting Remote Control connector {ConnectorKey} failed.", key);
            Notifier.ShowError(nameof(RemoteControl), exception.Message, T("Common.Error", "Error"));
        }
    }

    private async Task SaveConnectorAsync()
    {
        try
        {
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

    private string T(string key, string fallback)
    {
        try { return Localization.Get(key, fallback: fallback); }
        catch (Exception exception) { Logger.LogError(exception, "Resolving Remote Control localization key {LocalizationKey} failed.", key); return fallback; }
    }

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

}
