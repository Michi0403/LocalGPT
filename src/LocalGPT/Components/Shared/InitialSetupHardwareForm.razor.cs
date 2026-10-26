using DevExpress.Blazor;
using LocalGPT.BusinessObjects;
using Microsoft.AspNetCore.Components;

namespace LocalGPT.Components.Shared;

/// <summary>Reusable physical-host hardware editor; retains the parent-owned provisioning service workflow.</summary>
public partial class InitialSetupHardwareForm
{
    /// <summary>Editable physical host hardware rows shared with the setup workflow.</summary>
    /// <value>Mutable reviewed hardware draft.</value>
    [Parameter, EditorRequired] public List<InitialSetupHardwareDevice> Hardware { get; set; } = [];

    /// <summary>Selected discovery and provisioning endpoint.</summary>
    /// <value>Current hardware endpoint.</value>
    [Parameter] public string HardwareEndpoint { get; set; } = string.Empty;

    /// <summary>Propagates endpoint edits to the parent provisioning workflow.</summary>
    /// <value>Endpoint change callback.</value>
    [Parameter] public EventCallback<string> HardwareEndpointChanged { get; set; }

    /// <summary>Current optional HWiNFO text input.</summary>
    /// <value>Report draft.</value>
    [Parameter] public string HwInfoText { get; set; } = string.Empty;

    /// <summary>Propagates report edits to the parent provisioning workflow.</summary>
    /// <value>Report change callback.</value>
    [Parameter] public EventCallback<string> HwInfoTextChanged { get; set; }

    /// <summary>True when the setup wizard is executing a command.</summary>
    /// <value>Current wizard busy state.</value>
    [Parameter] public bool Busy { get; set; }

    /// <summary>True when the hardware workstream is executing.</summary>
    /// <value>Hardware command busy state.</value>
    [Parameter] public bool HardwareBusy { get; set; }

    /// <summary>Current human-readable operation status.</summary>
    /// <value>Busy status text.</value>
    [Parameter] public string BusyText { get; set; } = string.Empty;

    /// <summary>Localized guidance for checking the shared operation console.</summary>
    /// <value>Status detail text.</value>
    [Parameter] public string StatusDetail { get; set; } = string.Empty;

    /// <summary>Adds a hardware row using the parent draft workflow.</summary>
    /// <value>Row-add callback.</value>
    [Parameter] public EventCallback OnAddHardware { get; set; }

    /// <summary>Removes a reviewed hardware row.</summary>
    /// <value>Row-removal callback.</value>
    [Parameter] public EventCallback<InitialSetupHardwareDevice> OnRemoveHardware { get; set; }

    /// <summary>Detects local host hardware through the parent service workflow.</summary>
    /// <value>Detection callback.</value>
    [Parameter] public EventCallback OnDetectHardware { get; set; }

    /// <summary>Persists the reviewed hardware list through the parent service workflow.</summary>
    /// <value>Save callback.</value>
    [Parameter] public EventCallback OnSaveHardware { get; set; }

    /// <summary>Imports reviewed HWiNFO text through the parent service workflow.</summary>
    /// <value>Import callback.</value>
    [Parameter] public EventCallback OnImportHwInfo { get; set; }

    /// <summary>Receives bounded DevExpress HWiNFO upload streams.</summary>
    /// <value>Upload callback.</value>
    [Parameter] public EventCallback<FilesUploadingEventArgs> OnUploadHwInfo { get; set; }

    /// <summary>Permitted plaintext HWiNFO MIME types.</summary>
    /// <value>Accepted media types.</value>
    private List<string> HwInfoAcceptedTypes { get; } = ["text/plain"];

    /// <summary>Permitted HWiNFO file extensions.</summary>
    /// <value>Accepted file suffixes.</value>
    private List<string> HwInfoAllowedExtensions { get; } = [".txt"];

    /// <summary>Updates one physical host endpoint for all related accelerator rows without splitting the host.</summary>
    /// <param name="previousHostKey">Current physical-host grouping key.</param>
    /// <param name="endpoint">New edited endpoint.</param>
    private void UpdateHardwareGroupEndpoint(string previousHostKey, string endpoint)
    {
        try
        {
            var normalized = (endpoint ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized)) return;
            foreach (var device in Hardware.Where(device =>
                string.Equals(string.IsNullOrWhiteSpace(device.HostKey) ? device.Endpoint : device.HostKey,
                    previousHostKey, StringComparison.OrdinalIgnoreCase)))
            {
                device.Endpoint = normalized;
                device.HostKey = string.Empty;
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Editing a physical host endpoint failed.");
            throw;
        }
    }
}
