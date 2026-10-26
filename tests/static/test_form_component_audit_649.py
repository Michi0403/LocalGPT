"""Component-extraction regressions after the all-form inventory."""
from pathlib import Path
import csv
import unittest

ROOT = Path(__file__).resolve().parents[2]
def read(relative): return (ROOT / relative).read_text(encoding="utf-8")

class FormComponentAudit649(unittest.TestCase):
    def test_tls_framework_enum_still_satisfies_mandatory_gate(self):
        self.assertIn('Enum.GetValues<System.Security.Cryptography.X509Certificates.X509KeyUsageFlags>()', read('src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor.cs'))
        self.assertIn('TLSCONTRACT0036', read('build/Assert-NetworkCertificateContract.ps1'))
    def test_hardware_editor_is_independent_child(self):
        host = read('src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor')
        child = read('src/LocalGPT/Components/Shared/InitialSetupHardwareForm.razor')
        code = read('src/LocalGPT/Components/Shared/InitialSetupHardwareForm.razor.cs')
        self.assertIn('<InitialSetupHardwareForm Hardware="@hardware"', host)
        for token in ('Hardware.GroupBy(device =>', '<DxFileInput FilesUploading="@((FilesUploadingEventArgs args) => OnUploadHwInfo.InvokeAsync(args))"', 'Click="@(() => OnSaveHardware.InvokeAsync())"', 'Click="@(() => OnDetectHardware.InvokeAsync())"'):
            self.assertIn(token, child)
        self.assertIn('EventCallback<FilesUploadingEventArgs>', code)
    def test_release_form_guard_is_wired(self):
        self.assertIn('AssertLocalGPTRazorFormEncapsulation',read('Directory.Build.targets'))
        self.assertIn('Assert-RazorFormEncapsulation.ps1',read('Build-Release.ps1'))
        self.assertIn('<Version>6.5.4</Version>',read('src/LocalGPT/LocalGPT.csproj'))
    def test_full_inventory_is_shipped(self):
        with (ROOT / 'FORM_AUDIT_ALL_COMPONENTS.csv').open(newline='',encoding='utf8') as f:
            rows=list(csv.DictReader(f))
        self.assertGreaterEqual(len(rows),90)
        self.assertIn('src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor',{r['file'] for r in rows})

if __name__ == '__main__': unittest.main()
