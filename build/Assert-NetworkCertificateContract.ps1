# Mandatory, fail-closed source contract for correct TLS server certificates and resilient optional listeners.
# Compatible with Windows PowerShell 5.1 and PowerShell 7; never imports or writes user certificates.
param([string]$RepositoryRoot = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { $RepositoryRoot = Split-Path -Parent $PSScriptRoot }
$generator = Join-Path $RepositoryRoot 'src/LocalGPT/Services/NetworkCertificateService.cs'
$models = Join-Path $RepositoryRoot 'src/LocalGPT/BusinessObjects/NetworkCertificateModels.cs'
$razor = Join-Path $RepositoryRoot 'src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor'
$razorCode = Join-Path $RepositoryRoot 'src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor.cs'
$createHandler = Join-Path $RepositoryRoot 'src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor.cs'
$controller = Join-Path $RepositoryRoot 'src/LocalGPT/Controller/NetworkCertificateController.cs'
$install = Join-Path $RepositoryRoot 'src/LocalGPT/Components/Pages/Install.razor'
$remote = Join-Path $RepositoryRoot 'src/LocalGPT/Program.Hosting.cs'
$mcp = Join-Path $RepositoryRoot 'src/LocalGPT/Program.McpGateway.cs'
$targets = Join-Path $RepositoryRoot 'Directory.Build.targets'
$release = Join-Path $RepositoryRoot 'Build-Release.ps1'
foreach ($path in @($generator,$models,$razor,$razorCode,$createHandler,$controller,$install,$remote,$mcp,$targets,$release)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "TLSCONTRACT0001: Missing maintained source $path" }
}
$a = [IO.File]::ReadAllText($generator)
$modelText = [IO.File]::ReadAllText($models)
$uiText = [IO.File]::ReadAllText($razor)
$uiCodeText = [IO.File]::ReadAllText($razorCode)
$handlerText = [IO.File]::ReadAllText($createHandler)
$controllerText = [IO.File]::ReadAllText($controller)
$installText = [IO.File]::ReadAllText($install)
$b = [IO.File]::ReadAllText($remote)
$c = [IO.File]::ReadAllText($mcp)
$d = [IO.File]::ReadAllText($targets)
$e = [IO.File]::ReadAllText($release)
$expected = @(
    @{ Name='TLSCONTRACT0002'; Text=$a; Fragment='new Oid("1.3.6.1.5.5.7.3.1", "TLS Web Server Authentication")'; Reason='Certificate generator must issue the real Server Authentication EKU.' },
    @{ Name='TLSCONTRACT0003'; Text=$a; Fragment='Generated network certificate is not valid for TLS Server Authentication'; Reason='Generated certificate must be checked before replacing existing PFX.' },
    @{ Name='TLSCONTRACT0004'; Text=$b; Fragment='X509CertificateLoader.LoadPkcs12FromFile('; Reason='Kestrel remote TLS certificate must be verified before host binding.' },
    @{ Name='TLSCONTRACT0005'; Text=$b; Fragment='if (!File.Exists(certificatePath))'; Reason='A missing optional remote PFX must be recognized.' },
    @{ Name='TLSCONTRACT0006'; Text=$b; Fragment='certificatePath = string.Empty;'; Reason='Missing or unusable optional remote PFX must not crash LocalGPT.' },
    @{ Name='TLSCONTRACT0007'; Text=$b; Fragment='supportsServerAuthentication'; Reason='Configured optional remote HTTPS certificate must have Server Authentication EKU.' },
    @{ Name='TLSCONTRACT0008'; Text=$c; Fragment='X509CertificateLoader.LoadPkcs12FromFile('; Reason='Dedicated MCP TLS certificate must be verified before binding.' },
    @{ Name='TLSCONTRACT0009'; Text=$c; Fragment='options.CertificatePath = string.Empty;'; Reason='Missing/unusable MCP certificate must not fail startup.' },
    @{ Name='TLSCONTRACT0010'; Text=$d; Fragment='AssertLocalGptNetworkCertificateContract'; Reason='The TLS source guard must be mandatory during builds.' },
    @{ Name='TLSCONTRACT0011'; Text=$e; Fragment='build/Assert-NetworkCertificateContract.ps1'; Reason='The TLS source guard must run during release preflight.' },
    @{ Name='TLSCONTRACT0013'; Text=$modelText; Fragment='public IEnumerable<X509KeyUsageFlags> KeyUsages'; Reason='Certificate request must retain selectable combined X.509 key usages.' },
    @{ Name='TLSCONTRACT0014'; Text=$modelText; Fragment='public IEnumerable<NetworkCertificateExtendedKeyUsage> ExtendedKeyUsages'; Reason='Certificate request must retain independently selectable EKU purposes.' },
    @{ Name='TLSCONTRACT0015'; Text=$uiText; Fragment='@bind-Values="CertificateRequest.KeyUsages"'; Reason='The DevExpress certificate editor must bind multi-select key usages.' },
    @{ Name='TLSCONTRACT0016'; Text=$uiText; Fragment='@bind-Values="CertificateRequest.ExtendedKeyUsages"'; Reason='The DevExpress certificate editor must bind multi-select EKUs.' },
    @{ Name='TLSCONTRACT0017'; Text=$uiText; Fragment='SelectionMode="ListBoxSelectionMode.Multiple"'; Reason='Certificate usage lists must remain multi-select.' },
    @{ Name='TLSCONTRACT0018'; Text=$uiText; Fragment='ShowCheckboxes="true"'; Reason='Users must be able to combine flags with DevExpress list checkboxes.' },
    @{ Name='TLSCONTRACT0019'; Text=$a; Fragment='new X509KeyUsageExtension(keyUsage, true)'; Reason='Generator must apply selected key-usage flags to the issued certificate.' },
    @{ Name='TLSCONTRACT0020'; Text=$a; Fragment='extendedKeyUsages.Add(MapExtendedKeyUsage(role))'; Reason='Generator must add all selected EKU roles, not just a fixed server purpose.' },
    @{ Name='TLSCONTRACT0021'; Text=$a; Fragment='request.RequireServerAuthentication &&'; Reason='TLS endpoint use must still reject certificates lacking server authentication.' },
    @{ Name='TLSCONTRACT0022'; Text=$handlerText; Fragment='if (!CertificateEditContext.Validate())'; Reason='Certificate validation must account for whether the user assigns it to Kestrel.' },
    @{ Name='TLSCONTRACT0023'; Text=$uiCodeText; Fragment='Enum.GetValues<NetworkCertificateExtendedKeyUsage>()'; Reason='EKU roles in the enum must be presented, rather than an incomplete manual list.' },
    @{ Name='TLSCONTRACT0024'; Text=$modelText; Fragment='[NetworkCertificateExtendedKeyUsage.ServerAuthentication]'; Reason='A newly created HTTPS certificate must preselect server authentication.' },
    @{ Name='TLSCONTRACT0025'; Text=$modelText; Fragment='[X509KeyUsageFlags.DigitalSignature, X509KeyUsageFlags.KeyEncipherment]'; Reason='Default RSA HTTPS key usages must stay compatible with Kestrel.' },
    @{ Name='TLSCONTRACT0026'; Text=$modelText; Fragment='= NetworkCertificateKeySize.Rsa3072;'; Reason='RSA key size defaults must remain secure.' },
    @{ Name='TLSCONTRACT0027'; Text=$uiText; Fragment='@bind-Value="CertificateRequest.KeySize"'; Reason='RSA size selection must remain editable.' },
    @{ Name='TLSCONTRACT0028'; Text=$uiText; Fragment='@bind-Value="CertificateRequest.Hash"'; Reason='Hash selection must remain editable.' },
    @{ Name='TLSCONTRACT0029'; Text=$uiText; Fragment='@bind-Value="CertificateRequest.StoreLocation"'; Reason='Store location enum must remain editable.' },
    @{ Name='TLSCONTRACT0030'; Text=$uiText; Fragment='@bind-Value="CertificateRequest.StoreName"'; Reason='Store name enum must remain editable.' },
    @{ Name='TLSCONTRACT0031'; Text=$a; Fragment='request.IsCertificateAuthority != ((keyUsage & X509KeyUsageFlags.KeyCertSign) != 0)'; Reason='Invalid CA/end-entity flag combinations must be rejected.' },
    @{ Name='TLSCONTRACT0032'; Text=$a; Fragment='certificateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension('; Reason='BasicConstraints must track an explicit CA choice.' },
    @{ Name='TLSCONTRACT0036'; Text=$uiCodeText; Fragment='Enum.GetValues<System.Security.Cryptography.X509Certificates.X509KeyUsageFlags>()'; Reason='All framework-defined X509KeyUsageFlags must remain selectable.' },
    @{ Name='TLSCONTRACT0037'; Text=$modelText; Fragment='= NetworkCertificateHash.Sha256;'; Reason='SHA-256 must remain the safe interoperable default hash.' },
    @{ Name='TLSCONTRACT0038'; Text=$modelText; Fragment='= StoreLocation.CurrentUser;'; Reason='Certificate store default must not require administrator rights.' },
    @{ Name='TLSCONTRACT0039'; Text=$modelText; Fragment='= StoreName.My;'; Reason='Default certificate store must be the personal certificate store.' },
    @{ Name='TLSCONTRACT0040'; Text=$modelText; Fragment='public bool InstallToStore { get; set; }'; Reason='Unrequested certificate store installation must remain disabled.' },
    @{ Name='TLSCONTRACT0041'; Text=$a; Fragment='LocalGptApplicationDataPaths.ResolveUserPath("certificates")'; Reason='Generated certificate path must use the canonical LocalGPT application-data location.' },
    @{ Name='TLSCONTRACT0042'; Text=$uiText; Fragment='ShowSelectAllCheckbox="true"'; Reason='Combined enum selection must remain discoverable with Select All support.' },
    @{ Name='TLSCONTRACT0043'; Text=$modelText; Fragment='NetworkCertificateCreateRequest : IValidatableObject'; Reason='Request must provide DataAnnotations cross-field rules for the certificate form.' },
    @{ Name='TLSCONTRACT0044'; Text=$uiText; Fragment='<EditForm EditContext="@CertificateEditContext" OnValidSubmit="CreateNetworkCertificateAsync" Context="certificateFormContext">'; Reason='The DevExpress certificate FormLayout must be hosted by a validated EditForm.' },
    @{ Name='TLSCONTRACT0045'; Text=$uiText; Fragment='<DataAnnotationsValidator />'; Reason='Form must use DataAnnotations validation.' },
    @{ Name='TLSCONTRACT0046'; Text=$uiText; Fragment='SubmitFormOnClick="true"'; Reason='Certificate creation must only submit a valid DevExpress form.' },
    @{ Name='TLSCONTRACT0047'; Text=$uiText; Fragment='Enabled="@(!IsCertificateBusy && !CertificateEditContext.GetValidationMessages().Any())"'; Reason='Invalid certificate selections must disable submission before reaching the service.' },
    @{ Name='TLSCONTRACT0048'; Text=$uiCodeText; Fragment='private void OnCertificateAuthorityChanged(bool enabled)'; Reason='Switch CA modes using safe, explicit editor presets.' },
    @{ Name='TLSCONTRACT0049'; Text=$modelText; Fragment='A Certificate Authority cannot contain end-entity EKUs.'; Reason='Mutually exclusive CA/EKU purposes must be rejected by the form model.' },
    @{ Name='TLSCONTRACT0050'; Text=$uiText; Fragment='<ValidationMessage For="@(() => CertificateRequest.ExtendedKeyUsages)" />'; Reason='Show invalid EKUs alongside their DevExpress list editor.' },
    @{ Name='TLSCONTRACT0051'; Text=$uiText; Fragment='<ValidationSummary />'; Reason='The DevExpress certificate form must display cross-field errors.' },
    @{ Name='TLSCONTRACT0052'; Text=$uiText; Fragment='ShowValidationSuccessState="true"'; Reason='DevExpress editor validation success/invalid states must remain enabled.' },
    @{ Name='TLSCONTRACT0053'; Text=$uiText; Fragment='<ValidationMessage For="@(() => CertificateRequest.RequireServerAuthentication)" />'; Reason='Server authentication incompatibility must appear beside its form field.' },
    @{ Name='TLSCONTRACT0054'; Text=$handlerText; Fragment='if (IsCertificateBusy) return;'; Reason='Double form submission must never issue overlapping certificates.' },
    @{ Name='TLSCONTRACT0055'; Text=$modelText; Fragment='[CertificateExtendedKeyUsageSelection]'; Reason='DevExpress must validate EKU selection using a field-level DataAnnotation.' },
    @{ Name='TLSCONTRACT0056'; Text=$modelText; Fragment='class CertificateExtendedKeyUsageSelectionAttribute : ValidationAttribute'; Reason='Certificate form incompatibilities must use standard Blazor validation attributes.' },
    @{ Name='TLSCONTRACT0057'; Text=$controllerText; Fragment='[ApiController]'; Reason='Certificate HTTP entry points must perform automatic BusinessObject validation.' },
    @{ Name='TLSCONTRACT0058'; Text=$controllerText; Fragment='ValidationProblem(ModelState)'; Reason='Additional certificate validation failures must return structured HTTP 400.' },
    @{ Name='TLSCONTRACT0059'; Text=$controllerText; Fragment='certificates.CreateAsync(request, cancellationToken)'; Reason='Controller operations must delegate to the DI-owned service.' },
    @{ Name='TLSCONTRACT0060'; Text=$installText; Fragment='<NetworkCertificateEditor Request="@CertificateRequest"'; Reason='Certificate editing must stay in its own reusable Razor component.' },
    @{ Name='TLSCONTRACT0061'; Text=$uiText; Fragment='Context="certificateFormContext"'; Reason='Razor nested child content must not share implicit context names.' },
    @{ Name='TLSCONTRACT0062'; Text=$uiText; Fragment='CheckedChanged="@((bool value) => OnCertificateAuthorityChanged(value))"'; Reason='DevExpress callback must bind with an explicitly typed event delegate.' },
    @{ Name='TLSCONTRACT0063'; Text=$controllerText; Fragment='[HumanApprovalRequired('; Reason='Certificate creation must remain explicitly approved when invoked through HTTP.' }
)
foreach ($rule in $expected) {
    if (-not $rule.Text.Contains([string]$rule.Fragment)) { throw "$($rule.Name): $($rule.Reason)" }
}
if ($a.Contains('1.3.6.2.5.5.7.3.1')) {
    throw 'TLSCONTRACT0012: Invalid server-authentication EKU OID reintroduced.'
}
$multipleUsageLists = [regex]::Matches($uiText, 'SelectionMode="ListBoxSelectionMode\.Multiple"').Count
if ($multipleUsageLists -lt 2) { throw 'TLSCONTRACT0033: Two independent multiple-selection DXListBox components are required.' }
foreach ($locale in @('de-DE','en-US','es-ES','fr-FR','ja-JP','uk-UA')) {
    $catalog = Join-Path $RepositoryRoot ('src/LocalGPT/Localization/' + $locale + '.json')
    if (-not (Test-Path -LiteralPath $catalog -PathType Leaf)) { throw "TLSCONTRACT0034: Missing localization $catalog" }
    $catalogText = [IO.File]::ReadAllText($catalog)
    foreach ($localizationKey in @('Install.Certificate.KeySize','Install.Certificate.Hash','Install.Certificate.KeyUsages','Install.Certificate.ExtendedKeyUsages','Install.Certificate.CaMode')) {
        if (-not $catalogText.Contains(('"' + $localizationKey + '"'))) {
            throw "TLSCONTRACT0035: Missing certificate UI localization key $localizationKey in $catalog"
        }
    }
}
Write-Host 'TLSCONTRACT: selected KU/EKU enum combinations, safe TLS defaults, DataAnnotations FormLayout validation, CA presets, DevExpress list boxes, PFX preflight and build/release guards verified.'
