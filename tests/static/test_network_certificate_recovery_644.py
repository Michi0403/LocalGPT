"""Offline guards for the October 2026 Kestrel certificate/startup regression."""
from pathlib import Path
import unittest
import re
ROOT = Path(__file__).resolve().parents[2]
GENERATOR = (ROOT/'src/LocalGPT/Services/NetworkCertificateService.cs').read_text()
HOSTING = (ROOT/'src/LocalGPT/Program.Hosting.cs').read_text()
MCP = (ROOT/'src/LocalGPT/Program.McpGateway.cs').read_text()
GUARD = (ROOT/'build/Assert-NetworkCertificateContract.ps1').read_text()
TARGETS = (ROOT/'Directory.Build.targets').read_text()
INSTALLER_GUARD = (ROOT/'build/Assert-InstallerLayout.ps1').read_text()

class NetworkCertificateRecovery644(unittest.TestCase):
    def test_real_server_authentication_oid_is_used_and_checked(self):
        self.assertIn('new Oid("1.3.6.1.5.5.7.3.1", "TLS Web Server Authentication")', GENERATOR)
        self.assertNotIn('1.3.6.2.5.5.7.3.1', GENERATOR)
        self.assertIn('Generated network certificate is not valid for TLS Server Authentication', GENERATOR)
        self.assertLess(GENERATOR.index('Generated network certificate is not valid for TLS Server Authentication'), GENERATOR.index('File.Move(temporaryPath, outputPath, true)'))
        self.assertIn('X509EnhancedKeyUsageExtension', GENERATOR)
    def test_missing_optional_web_certificate_does_not_prevent_startup(self):
        self.assertIn('if (!File.Exists(certificatePath))', HOSTING)
        self.assertNotIn('throw new FileNotFoundException("Configured LocalGPT network TLS certificate was not found.', HOSTING)
        self.assertIn('certificatePath = string.Empty;', HOSTING)
        self.assertIn('Continuing the optional network endpoint over HTTP', HOSTING)
        self.assertIn('options.Listen(IPAddress.Loopback, port)', HOSTING)
        self.assertIn('X509CertificateLoader.LoadPkcs12FromFile', HOSTING)
        self.assertIn('supportsServerAuthentication', HOSTING)
    def test_mcp_optional_certificate_is_preflighted(self):
        self.assertIn('X509CertificateLoader.LoadPkcs12FromFile', MCP)
        self.assertIn('options.CertificatePath = string.Empty;', MCP)
        self.assertIn('1.3.6.1.5.5.7.3.1', MCP)
    def test_mandatory_powershell_guards_not_rebased(self):
        self.assertIn('AssertLocalGptNetworkCertificateContract', TARGETS)
        self.assertIn('build/Assert-NetworkCertificateContract.ps1', (ROOT/'Build-Release.ps1').read_text())
        self.assertIn('TLSCONTRACT0012', GUARD)
        self.assertIn('AssertLocalGptInstallerLayout', TARGETS)
        self.assertIn('INSTALLLAYOUT0004', INSTALLER_GUARD)
    def test_version_progression(self):
        self.assertIn('<Version>6.5.2</Version>', (ROOT/'src/LocalGPT/LocalGPT.csproj').read_text())
        self.assertIn('<Version>4.9.9</Version>', (ROOT/'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj').read_text())

if __name__=='__main__': unittest.main()
