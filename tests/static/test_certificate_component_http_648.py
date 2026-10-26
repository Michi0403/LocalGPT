"""Certificate editor/HTTP endpoints maintain a single validated BusinessObject and real controller coverage."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
def read(path): return (ROOT/path).read_text()

class CertificateComponentHttp648(unittest.TestCase):
    def test_subform_owns_edit_context_and_creation(self):
        install=read('src/LocalGPT/Components/Pages/Install.razor')
        form=read('src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor')
        code=read('src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor.cs')
        self.assertIn('<NetworkCertificateEditor Request="@CertificateRequest"',install)
        self.assertNotIn('<EditForm EditContext="@CertificateEditContext"',install)
        self.assertIn('Context="certificateFormContext"',form)
        self.assertIn('CheckedChanged="@((bool value) => OnCertificateAuthorityChanged(value))"',form)
        self.assertIn('OnCertificateCreated',code)
        self.assertIn('NetworkCertificates.CreateAsync(CertificateRequest)',code)
        self.assertIn('CertificateEditContext = new EditContext(Request);',code)
        self.assertIn('DataAnnotationsValidator',form)
        self.assertIn('ILogger<NetworkCertificateEditor>',form)
        self.assertIn('INotificationService',form)
        self.assertIn('IComponentActivityService',form)
    def test_controller_maps_both_public_certificate_service_methods(self):
        interface=read('src/LocalGPT/Interfaces/INetworkCertificateService.cs')
        controller=read('src/LocalGPT/Controller/NetworkCertificateController.cs')
        request=read('src/LocalGPT/BusinessObjects/NetworkCertificateModels.cs')
        for operation in ('CreateDefaultRequest()', 'CreateAsync('): self.assertIn(operation,interface)
        self.assertIn('certificates.CreateDefaultRequest()',controller)
        self.assertIn('certificates.CreateAsync(request, cancellationToken)',controller)
        self.assertIn('[ApiController]',controller)
        self.assertIn('[FromBody] NetworkCertificateCreateRequest request',controller)
        self.assertIn('ValidationProblem(ModelState)',controller)
        self.assertIn('HttpPost',controller)
        self.assertIn('HumanApprovalRequired',controller)
        self.assertIn('NetworkCertificateCreateRequest : IValidatableObject',request)
    def test_build_contract_strong_and_no_extra_service(self):
        ps=read('build/Assert-NetworkCertificateContract.ps1')
        for i in range(57,64): self.assertIn('TLSCONTRACT%04d' % i,ps)
        reg=read('src/LocalGPT/Program.ServiceRegistration.cs')
        self.assertEqual(reg.count('AddSingleton<INetworkCertificateService, NetworkCertificateService>()'),1)
    def test_release_version(self):
        self.assertIn('<Version>6.5.2</Version>',read('src/LocalGPT/LocalGPT.csproj'))
if __name__=='__main__': unittest.main()
