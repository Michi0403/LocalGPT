"""Protect the original DevExpress FormLayout with DataAnnotations validations."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
def read(name): return (ROOT/name).read_text()
class CertificateAttributeForm647(unittest.TestCase):
    def test_interdependent_validation_is_model_owned(self):
        model=read('src/LocalGPT/BusinessObjects/NetworkCertificateModels.cs')
        for text in ('NetworkCertificateCreateRequest : IValidatableObject', '[Required(', '[Range(1, 3650',
                     'IEnumerable<ValidationResult> Validate(ValidationContext validationContext)',
                     '[CertificateExtendedKeyUsageSelection]',
                     'CertificateExtendedKeyUsageSelectionAttribute : ValidationAttribute',
                     'A Certificate Authority cannot contain end-entity EKUs.',
                     'HTTPS requires the ServerAuthentication EKU.',
                     'Certificate authorities must select KeyCertSign.'):
            self.assertIn(text,model)
    def test_validated_devexpress_form_has_one_owner(self):
        ui=read('src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor')
        cert=ui.split('<div id="tls-certificate"',1)[1]
        self.assertIn('<EditForm EditContext="@CertificateEditContext" OnValidSubmit="CreateNetworkCertificateAsync" Context="certificateFormContext">',cert)
        self.assertEqual(cert.count('<EditForm'),1)
        self.assertEqual(cert.count('</EditForm>'),1)
        for item in ('<DataAnnotationsValidator />','<DxFormLayout>','<ValidationSummary />','SubmitFormOnClick="true"',
                     '<ValidationMessage For="@(() => CertificateRequest.KeyUsages)" />',
                     '<ValidationMessage For="@(() => CertificateRequest.ExtendedKeyUsages)" />',
                     'CheckedChanged="@((bool value) => OnCertificateAuthorityChanged(value))"',
                     'ShowValidationSuccessState="true"',
                     '<ValidationMessage For="@(() => CertificateRequest.RequireServerAuthentication)" />'):
            self.assertIn(item,cert)
    def test_no_invalid_request_reaches_service(self):
        code=read('src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor.cs')
        handler=read('src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor.cs')
        self.assertIn('CertificateEditContext = new EditContext(Request);',code)
        self.assertIn('CertificateRequest.ExtendedKeyUsages = enabled',code)
        self.assertIn('CertificateRequest.RequireServerAuthentication = !enabled;',code)
        self.assertLess(handler.index('if (!CertificateEditContext.Validate())'),handler.index('NetworkCertificates.CreateAsync(CertificateRequest)'))
        self.assertIn('if (IsCertificateBusy) return;', handler)
    def test_build_and_release_guards_remain_fail_closed(self):
        guard=read('build/Assert-NetworkCertificateContract.ps1')
        for key in ('TLSCONTRACT0043','TLSCONTRACT0044','TLSCONTRACT0045','TLSCONTRACT0046','TLSCONTRACT0047','TLSCONTRACT0048','TLSCONTRACT0049','TLSCONTRACT0050','TLSCONTRACT0051','TLSCONTRACT0052','TLSCONTRACT0053','TLSCONTRACT0054','TLSCONTRACT0055','TLSCONTRACT0056'):
            self.assertIn(key,guard)
        self.assertIn('AssertLocalGptNetworkCertificateContract',read('Directory.Build.targets'))
    def test_version(self):
        self.assertIn('<Version>6.5.4</Version>',read('src/LocalGPT/LocalGPT.csproj'))
if __name__=='__main__': unittest.main()
