# Mandatory maintained subform ownership contract. No implementation writes or baselines.
param([string]$RepositoryRoot = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { $RepositoryRoot = Split-Path -Parent $PSScriptRoot }
$contracts = @(
    @{ Path='src/LocalGPT/Components/Pages/Install.razor'; Fragment='<NetworkCertificateEditor Request="@CertificateRequest"' },
    @{ Path='src/LocalGPT/Components/Pages/Install.razor'; Fragment='<InitialSetupAssistantPanel' },
    @{ Path='src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor'; Fragment='<InitialSetupHardwareForm Hardware="@hardware"' },
    @{ Path='src/LocalGPT/Components/Shared/InitialSetupHardwareForm.razor'; Fragment='<DxFormLayout CssClass="razor-component-layout-owner InitialSetupHardwareForm"' },
    @{ Path='src/LocalGPT/Components/Shared/InitialSetupHardwareForm.razor'; Fragment='OnUploadHwInfo' },
    @{ Path='src/LocalGPT/Components/Shared/InitialSetupHardwareForm.razor.cs'; Fragment='[Parameter] public EventCallback<FilesUploadingEventArgs> OnUploadHwInfo' },
    @{ Path='src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor'; Fragment='<EditForm EditContext="@CertificateEditContext"' },
    @{ Path='src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor.cs'; Fragment='Enum.GetValues<System.Security.Cryptography.X509Certificates.X509KeyUsageFlags>()' },
    @{ Path='src/LocalGPT/Controller/NetworkCertificateController.cs'; Fragment='[ApiController]' },
    @{ Path='src/LocalGPT/Controller/NetworkCertificateController.cs'; Fragment='ValidationProblem(ModelState)' }
)
foreach ($contract in $contracts) {
    $path = Join-Path $RepositoryRoot ([string]$contract.Path)
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "FORMCONTRACT0001: Missing required component/contract $path" }
    $source = [IO.File]::ReadAllText($path)
    if (-not $source.Contains([string]$contract.Fragment)) {
        throw "FORMCONTRACT0002: $path no longer contains its required component/validation/service contract: $($contract.Fragment)"
    }
}
Write-Host "FORMCONTRACT: LocalGPT reusable Razor subforms, existing service/HTTP routes and validation boundaries verified."
