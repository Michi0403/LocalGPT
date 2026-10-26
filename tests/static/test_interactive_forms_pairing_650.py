"""Regression contracts for the Windows-reported Blazor circuit and TLS UI defects."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
def source(relative):
    return (ROOT / relative).read_text(encoding='utf-8')

class InteractiveFormsAndPairingTests(unittest.TestCase):
    def test_certificate_ca_checkbox_is_model_validation_aware(self):
        view = source('src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor')
        self.assertIn('CheckedExpression="@(() => CertificateRequest.IsCertificateAuthority)"', view)
        self.assertIn('<EditForm EditContext="@CertificateEditContext"',view)
        self.assertIn('<DataAnnotationsValidator />',view)

    def test_other_calculated_checked_state_does_not_crash_dxvalidation(self):
        for component in (ROOT/'src/LocalGPT/Components').rglob('*.razor'):
            view = component.read_text(encoding='utf8')
            self.assertNotIn('= ValidationEnabled="false">',view,str(component))
            for checkbox in re.findall(r'<DxCheckBox\b.*?/>',view,flags=re.S):
                if 'Checked=' in checkbox and '@bind-Checked' not in checkbox:
                    self.assertTrue('CheckedExpression=' in checkbox or 'ValidationEnabled="false"' in checkbox,
                                    f'No field expression or calculated-control opt-out in {component}')

    def test_pairing_generates_fresh_public_ticket_without_auto_disclosing_enrollment(self):
        view=source('src/LocalGPT/Components/Pages/OneWireSecurity.razor')
        self.assertIn('Security.CreatePairingTicketAsync(TimeSpan.FromMinutes(15), lifetimeCts.Token)',view)
        self.assertIn('if (string.Equals(key, "pairing", StringComparison.OrdinalIgnoreCase))',view)
        self.assertIn('if (!string.IsNullOrWhiteSpace(_otpUri))',view)
        self.assertIn('CreatePairingMaterialAsync',view)

    def test_unwanted_install_scroll_buttons_are_gone(self):
        layout=source('src/LocalGPT/Components/Layout/MainLayout.razor')
        for text in ('localgpt-page-scroll-assist','href="#install-scroll-top"','href="#install-scroll-bottom"'):
            self.assertNotIn(text,layout)
        view=source('src/LocalGPT/Components/Pages/Install.razor')
        self.assertNotIn('id="install-scroll-top"',view)
        self.assertNotIn('id="install-scroll-bottom"',view)

    def test_ui_does_not_equate_saved_pfx_path_with_active_https(self):
        view=source('src/LocalGPT/Components/Pages/Install.razor')
        self.assertIn('This is a configuration preview, not the running listener.',view)
        cs=source('src/LocalGPT/Components/Pages/Install.razor.cs')
        self.assertIn('HTTPS requested; certificate must pass startup validation',cs)
        self.assertNotIn('"https")}://',cs)

    def test_mandatory_powershell_guard_on_build_and_release(self):
        guard='build/Assert-InteractiveFormsAndPairing.ps1'
        self.assertTrue((ROOT/guard).is_file())
        targets=source('Directory.Build.targets')
        self.assertIn('AssertLocalGptInteractiveFormsAndPairing', targets)
        self.assertIn('InteractiveFormsAndPairingGuardScript', targets)
        self.assertIn(guard,source('Build-Release.ps1'))

    def test_versions_obey_one_digit_minor_and_patch(self):
        version=re.search(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>',source('src/LocalGPT/LocalGPT.csproj'))
        self.assertIsNotNone(version)
        self.assertLessEqual(len(version.group(2)),1)
        self.assertLessEqual(len(version.group(3)),1)
        self.assertEqual(version.group(0),'<Version>6.5.2</Version>')

if __name__=='__main__':unittest.main()
