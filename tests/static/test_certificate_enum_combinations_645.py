"""Regression contract for restored certificate enum DXListBoxes and secure presets."""
import json
import re
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
def source(path): return (ROOT / path).read_text()
class CertificateEnumCombinations645(unittest.TestCase):
    def test_all_certificate_selections_use_devexpress_listboxes(self):
        ui = source('src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor')
        for selection in ('KeySize','Hash','StoreLocation','StoreName'):
            self.assertRegex(ui, r'<DxListBox[^>]+@bind-Value="CertificateRequest\.' + selection + r'"')
        for selection in ('KeyUsages','ExtendedKeyUsages'):
            self.assertRegex(ui, r'<DxListBox[^>]+@bind-Values="CertificateRequest\.' + selection + r'"[^>]+SelectionMode="ListBoxSelectionMode\.Multiple"[^>]+ShowCheckboxes="true"')
        self.assertIn('Enum.GetValues<NetworkCertificateExtendedKeyUsage>()', source('src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor.cs'))
    def test_presets_are_purpose_specific(self):
        model = source('src/LocalGPT/BusinessObjects/NetworkCertificateModels.cs')
        self.assertIn('= NetworkCertificateKeySize.Rsa3072;', model)
        self.assertIn('= NetworkCertificateHash.Sha256;', model)
        self.assertIn('[X509KeyUsageFlags.DigitalSignature, X509KeyUsageFlags.KeyEncipherment]', model)
        self.assertIn('[NetworkCertificateExtendedKeyUsage.ServerAuthentication]', model)
        self.assertIn('public bool IsCertificateAuthority { get; set; }', model)
        self.assertIn('public bool RequireServerAuthentication { get; set; } = true;', model)
    def test_all_enums_have_real_issuance_effects(self):
        model = source('src/LocalGPT/BusinessObjects/NetworkCertificateModels.cs')
        service = source('src/LocalGPT/Services/NetworkCertificateService.cs')
        roles = re.search(r'public enum NetworkCertificateExtendedKeyUsage\s*\{([^}]+)\}', model).group(1)
        role_names = set(re.findall(r'^    ([A-Za-z]+),?$', roles, re.M))
        for role in role_names:
            self.assertIn('NetworkCertificateExtendedKeyUsage.' + role + ' => new Oid(', service)
        self.assertIn('new X509KeyUsageExtension(keyUsage, true)', service)
        self.assertIn('selectedRoles.Contains(NetworkCertificateExtendedKeyUsage.ServerAuthentication)', service)
        self.assertIn('request.IsCertificateAuthority != ((keyUsage & X509KeyUsageFlags.KeyCertSign) != 0)', service)
        self.assertLess(service.index('Generated network certificate is not valid for TLS Server Authentication'),service.index('File.Move(temporaryPath, outputPath, true)'))
    def test_endpoint_requires_server_certificate(self):
        handler = source('src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor.cs')
        self.assertLess(handler.index('if (!CertificateEditContext.Validate())'),handler.index('NetworkCertificates.CreateAsync(CertificateRequest)'))
    def test_localized_all_catalogs(self):
        for language in ('de-DE','en-US','es-ES','fr-FR','ja-JP','uk-UA'):
            data = json.loads(source('src/LocalGPT/Localization/' + language + '.json'))
            for key in ('KeySize','Hash','KeyUsages','KeyUsageHelp','ExtendedKeyUsages','EkuHelp','CaMode','CaHelp'):
                self.assertIn('Install.Certificate.'+key, data)
    def test_guard_wired_to_normal_and_release_builds(self):
        self.assertIn('AssertLocalGptNetworkCertificateContract',source('Directory.Build.targets'))
        self.assertIn('build/Assert-NetworkCertificateContract.ps1', source('Build-Release.ps1'))
        guard = source('build/Assert-NetworkCertificateContract.ps1')
        for number in ('0015','0016','0024','0025','0031','0033','0035'):
            self.assertIn('TLSCONTRACT'+number, guard)
    def test_version(self):
        self.assertIn('<Version>6.5.2</Version>',source('src/LocalGPT/LocalGPT.csproj'))
if __name__=='__main__': unittest.main()
