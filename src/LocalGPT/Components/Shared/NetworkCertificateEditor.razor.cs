using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace LocalGPT.Components.Shared;

/// <summary>Owns the TLS certificate DevExpress form, validation and certificate-creation service operation.</summary>
public partial class NetworkCertificateEditor
{
    /// <summary>Mutable certificate draft supplied by the parent installation page.</summary>
    /// <value>The validated request shared with the parent installation draft.</value>
    [Parameter, EditorRequired]
    public NetworkCertificateCreateRequest Request { get; set; } = new();

    /// <summary>Called once after a certificate is issued successfully.</summary>
    /// <value>The parent callback for certificate issue results.</value>
    [Parameter]
    public EventCallback<NetworkCertificateCreateResult> OnCertificateCreated { get; set; }

    /// <summary>Notifies the parent when certificate generation begins or completes.</summary>
    /// <value>The parent operation-progress callback.</value>
    [Parameter]
    public EventCallback<bool> BusyChanged { get; set; }

    /// <summary>Tracks the form request instance currently bound to EditContext.</summary>
    private NetworkCertificateCreateRequest? BoundRequest;

    /// <summary>Form validation and field changes are owned entirely by the subcomponent.</summary>
    private EditContext CertificateEditContext = new(new NetworkCertificateCreateRequest());

    /// <summary>Current draft used by the bound DevExpress editors.</summary>
    /// <value>The current externally-provided request.</value>
    private NetworkCertificateCreateRequest CertificateRequest => Request;

    /// <summary>True during a single certificate creation transaction.</summary>
    private bool IsCertificateBusy;

    /// <summary>Rendered status of the last certificate action.</summary>
    private string CertificateStatus = string.Empty;

    /// <summary>RSA key sizes exposed without removing enum choices.</summary>
    /// <value>All supported key sizes.</value>
    private IReadOnlyList<NetworkCertificateKeySize> CertificateKeySizes { get; } = Enum.GetValues<NetworkCertificateKeySize>();

    /// <summary>Supported certificate hashing algorithms.</summary>
    /// <value>All supported hash algorithms.</value>
    private IReadOnlyList<NetworkCertificateHash> CertificateHashes { get; } = Enum.GetValues<NetworkCertificateHash>();

    /// <summary>All selectable, nonzero framework X.509 usage flags.</summary>
    /// <value>Individual flags available to the multi-select editor.</value>
    private IReadOnlyList<X509KeyUsageFlags> CertificateKeyUsageOptions { get; } = Enum.GetValues<System.Security.Cryptography.X509Certificates.X509KeyUsageFlags>().Where(flag => flag != X509KeyUsageFlags.None).ToArray();

    /// <summary>All supported extended-key-usage purposes.</summary>
    /// <value>All supported EKUs available for multi-selection.</value>
    private IReadOnlyList<NetworkCertificateExtendedKeyUsage> CertificateExtendedKeyUsageOptions { get; } = Enum.GetValues<NetworkCertificateExtendedKeyUsage>();

    /// <summary>Framework certificate-store locations.</summary>
    /// <value>Available certificate store locations.</value>
    private IReadOnlyList<StoreLocation> CertificateStoreLocations { get; } = Enum.GetValues<StoreLocation>();

    /// <summary>Framework certificate-store names.</summary>
    /// <value>Available certificate store names.</value>
    private IReadOnlyList<StoreName> CertificateStoreNames { get; } = Enum.GetValues<StoreName>();

    /// <summary>Rebinds the EditContext when the installation page loads or replaces its request.</summary>
    protected override void OnParametersSet()
    {
        try
        {
            if (!ReferenceEquals(BoundRequest, Request))
            {
                BoundRequest = Request;
                CertificateEditContext = new EditContext(Request);
                CertificateEditContext.Validate();
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Binding the certificate request to its editor failed.");
            throw;
        }
    }

    /// <summary>Evaluates cross-field DataAnnotations as DevExpress selections change.</summary>
    private void ValidateCertificateForm()
    {
        try
        {
            CertificateEditContext.Validate();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Validating certificate form selections failed.");
            throw;
        }
    }

    /// <summary>Selects valid certificate-authority or TLS-end-entity presets without restricting future edits.</summary>
    /// <param name="enabled">Whether certificate-authority mode is selected.</param>
    private void OnCertificateAuthorityChanged(bool enabled)
    {
        try
        {
            CertificateRequest.IsCertificateAuthority = enabled;
            CertificateRequest.KeyUsages = enabled
                ? [X509KeyUsageFlags.KeyCertSign, X509KeyUsageFlags.CrlSign]
                : [X509KeyUsageFlags.DigitalSignature, X509KeyUsageFlags.KeyEncipherment];
            CertificateRequest.ExtendedKeyUsages = enabled ? [] : [NetworkCertificateExtendedKeyUsage.ServerAuthentication];
            CertificateRequest.RequireServerAuthentication = !enabled;
            ValidateCertificateForm();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Changing certificate-authority form presets failed.");
            throw;
        }
    }

    /// <summary>Resolves persisted localized text for each certificate editor label and hint.</summary>
    /// <param name="key">Localization resource key.</param>
    /// <param name="fallback">Visible fallback for missing translations.</param>
    /// <returns>Localized label text.</returns>
    private string T(string key, string fallback)
    {
        try
        {
            return Localization.Get(key, fallback: fallback);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Certificate editor localization failed.");
            throw;
        }
    }

    /// <summary>Executes only a valid, user-submitted certificate creation and reports the result to its parent.</summary>
    /// <returns>A task completing when creation and parent state propagation finish.</returns>
    private async Task CreateNetworkCertificateAsync()
    {
        try
        {
            if (IsCertificateBusy) return;
            if (!CertificateEditContext.Validate())
            {
                CertificateStatus = "Correct the certificate form validation errors before creating a certificate.";
                return;
            }
            IsCertificateBusy = true;
            await BusyChanged.InvokeAsync(true).ConfigureAwait(true);
            CertificateStatus = "Creating certificate...";
            var result = await NetworkCertificates.CreateAsync(CertificateRequest).ConfigureAwait(true);
            await OnCertificateCreated.InvokeAsync(result).ConfigureAwait(true);
            CertificateStatus = $"Created {result.Thumbprint} at {result.PfxPath}; {result.StoreDescription}; valid until {result.NotAfter:O}.";
            Notifier.ShowSuccess(nameof(NetworkCertificateEditor), "Certificate created. Save setup settings and restart LocalGPT to activate HTTPS.", "TLS certificate ready");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "TLS certificate creation failed.");
            CertificateStatus = exception.Message;
            Notifier.ShowError(nameof(NetworkCertificateEditor), "Certificate creation failed. Review form status and application logs.", "TLS certificate failed");
        }
        finally
        {
            IsCertificateBusy = false;
            await BusyChanged.InvokeAsync(false).ConfigureAwait(true);
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
    }
}
