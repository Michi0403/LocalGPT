using LocalGPT.BusinessObjects;

namespace LocalGPT.Components.Pages
{
    public partial class Install
    {
        /// <summary>Stores a detached MCP gateway draft so editing /install never changes the live gateway policy before Save.</summary>
        private McpGatewayOptions McpGatewayModel = new();
        private IReadOnlyList<string> McpCacheScopes { get; } = ["private", "public"];
        private bool McpApiKeyVisible;

        /// <summary>Gets the dedicated MCP endpoint preview that becomes effective after restart.</summary>
        private string McpGatewayEndpointPreview
        {
            get
            {
                var model = NormalizeMcpGatewayDraft(McpGatewayModel);
                var scheme = string.IsNullOrWhiteSpace(model.CertificatePath) ? "http" : "https";
                var address = model.Address switch
                {
                    "0.0.0.0" or "*" => "<all-ipv4-interfaces>",
                    "::" => "[::]",
                    _ => model.Address
                };
                return model.DedicatedListenerEnabled
                    ? $"{scheme}://{address}:{model.Port}{model.Path}"
                    : "Dedicated MCP listener disabled";
            }
        }

        /// <summary>Gets the primary-loopback MCP endpoint preview.</summary>
        private string McpPrimaryEndpointPreview => McpGatewayModel.ExposeOnPrimaryEndpoint
            ? $"http://127.0.0.1:{LocalGPT.Program.Port}{NormalizeMcpGatewayPath(McpGatewayModel.Path)}"
            : "Primary LocalGPT endpoint exposure disabled";

        /// <summary>Gets the persisted remote-web listener preview shown beside the MCP port contract.</summary>
        private string McpRemoteEndpointPreview => NetworkModel.Enabled
            ? $"{(string.IsNullOrWhiteSpace(NetworkModel.CertificatePath) ? "http" : "https")}://{(string.IsNullOrWhiteSpace(NetworkModel.Address) ? "0.0.0.0" : NetworkModel.Address)}:{NetworkModel.Port}"
            : "disabled";

        /// <summary>Gets a human-readable MCP port conflict warning before settings are persisted.</summary>
        private string McpPortContractWarning
        {
            get
            {
                if (!McpGatewayModel.Enabled || !McpGatewayModel.DedicatedListenerEnabled)
                    return string.Empty;
                if (McpGatewayModel.Port == LocalGPT.Program.Port)
                    return $"MCP TCP {McpGatewayModel.Port} conflicts with the LocalGPT app/install listener.";
                if (McpGatewayModel.Port == LocalGPT.Program.OneWirePort)
                    return $"MCP TCP {McpGatewayModel.Port} conflicts with OneWire TCP.";
                if (McpGatewayModel.Port == LocalGPT.Program.OneWireDiscoveryPort)
                    return $"MCP TCP {McpGatewayModel.Port} uses the same numeric port as OneWire discovery UDP; LocalGPT keeps these port contracts distinct to avoid deployment ambiguity.";
                if (NetworkModel.Enabled && NetworkModel.Port > 0 && McpGatewayModel.Port == NetworkModel.Port)
                    return $"MCP TCP {McpGatewayModel.Port} conflicts with the optional remote web listener.";
                return string.Empty;
            }
        }

        /// <summary>Gets a deployment warning when remote MCP admission still depends on an external API-key environment value.</summary>
        private string McpAdmissionWarning => McpGatewayModel.AllowRemoteClients && string.IsNullOrWhiteSpace(McpGatewayModel.ApiKey)
            ? "Remote MCP access forces API-key authentication. Leave the field empty only when LOCALGPT_MCP_API_KEY will be supplied to the LocalGPT process."
            : string.Empty;

        /// <summary>Creates a new strong MCP API key in the detached install draft without persisting it until Save.</summary>
        private void GenerateMcpApiKey()
        {
            try
            {
            McpGatewayModel.ApiKey = McpGatewayPolicy.GenerateApiKey();
            McpApiKeyVisible = true;
        
            }
            catch (Exception __componentMethodException)
            {
                Logger.LogError(__componentMethodException, "Component method Install.McpGateway.GenerateMcpApiKey failed.");
                throw;
            }
        }

        /// <summary>Toggles masking of the detached MCP API key so the local operator can copy a newly generated value.</summary>
        private void ToggleMcpApiKeyVisibility()
        {
            try
            {
            McpApiKeyVisible = !McpApiKeyVisible;
        
            }
            catch (Exception __componentMethodException)
            {
                Logger.LogError(__componentMethodException, "Component method Install.McpGateway.ToggleMcpApiKeyVisibility failed.");
                throw;
            }
        }

        /// <summary>Creates a detached copy of MCP options for the install workbench.</summary>
        private McpGatewayOptions CloneMcpGatewayOptions(McpGatewayOptions? source)
        {
            try
            {
                return McpGatewayPolicy.Clone(source);
            }
            catch (Exception __componentMethodException)
            {
                Logger.LogError(__componentMethodException, "Component method Install.McpGateway.CloneMcpGatewayOptions failed.");
                throw;
            }
        }

        /// <summary>Normalizes and bounds one MCP draft before it is persisted.</summary>
        private McpGatewayOptions NormalizeMcpGatewayDraft(McpGatewayOptions source)
        {
            try
            {
                return McpGatewayPolicy.NormalizeDraft(source);
            }
            catch (Exception __componentMethodException)
            {
                Logger.LogError(__componentMethodException, "Component method Install.McpGateway.NormalizeMcpGatewayDraft failed.");
                throw;
            }
        }

        /// <summary>Normalizes one MCP HTTP route.</summary>
        private string NormalizeMcpGatewayPath(string? value)
        {
            try
            {
                return McpGatewayPolicy.NormalizePath(value);
            }
            catch (Exception __componentMethodException)
            {
                Logger.LogError(__componentMethodException, "Component method Install.McpGateway.NormalizeMcpGatewayPath failed.");
                throw;
            }
        }
    }
}
