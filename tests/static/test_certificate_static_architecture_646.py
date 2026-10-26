"""Regression: no certificate helpers may bypass the mandatory application-static gate.

The normal mandatory PowerShell build gate remains authoritative. This offline
regression covers the exact failure from the 6.4.5 Windows build log.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

class CertificateStaticArchitecture646(unittest.TestCase):
    def test_network_certificate_eku_mapper_is_instance_owned(self):
        service = (ROOT/'src/LocalGPT/Services/NetworkCertificateService.cs').read_text(encoding='utf-8')
        self.assertIn('private Oid MapExtendedKeyUsage(NetworkCertificateExtendedKeyUsage role) => role switch', service)
        self.assertNotRegex(service, r'(?m)^\s*(?:public|private|protected|internal)\s+(?:\w+\s+)*static\s+[^\n]*MapExtendedKeyUsage')
        self.assertIn('extendedKeyUsages.Add(MapExtendedKeyUsage(role));', service)
        # Map must still enumerate the same EKU OIDs.
        for oid in ('1.3.6.1.5.5.7.3.1', '1.3.6.1.5.5.7.3.2', '1.3.6.1.5.5.7.3.9'):
            self.assertIn(oid, service)

    def test_network_certificate_di_lifetime_is_owned_by_service_registration(self):
        registrations = (ROOT/'src/LocalGPT/Program.ServiceRegistration.cs').read_text(encoding='utf-8')
        service = (ROOT/'src/LocalGPT/Services/NetworkCertificateService.cs').read_text(encoding='utf-8')
        # Keep the established lifetime in DI registration; never introduce a self-managed singleton.
        declaration = 'builder.Services.AddSingleton<INetworkCertificateService, NetworkCertificateService>();'
        self.assertEqual(1, registrations.count(declaration))
        self.assertIn('public sealed class NetworkCertificateService(ILogger<NetworkCertificateService> logger) : INetworkCertificateService', service)
        self.assertNotIn('static class NetworkCertificateService', service)

    def test_static_audit_still_build_breaking_and_never_weakened(self):
        guard = (ROOT/'build/Invoke-ArchitectureAudit.ps1').read_text(encoding='utf-8')
        caller = (ROOT/'build/Assert-ApplicationStaticPolicy.ps1').read_text(encoding='utf-8')
        targets = (ROOT/'Directory.Build.targets').read_text(encoding='utf-8')
        self.assertIn('Invoke-StaticFallback', guard)
        self.assertIn('Application static policy validation passed without a legacy baseline.', guard)
        self.assertIn('Invoke-ArchitectureAudit.ps1', caller)
        self.assertIn('Assert-ApplicationStaticPolicy.ps1', targets)

    def test_static_declaration_pattern_no_longer_matches_certificate_service(self):
        text = (ROOT/'src/LocalGPT/Services/NetworkCertificateService.cs').read_text(encoding='utf-8')
        # Match the PowerShell fallback declaration regex, including multiline file positions.
        pattern = r'(?m)^\s*(?:public|private|protected|internal)\s+(?:(?:sealed|partial|new|unsafe|readonly|abstract)\s+)*static\s+[^\r\n]+'
        self.assertEqual([], [m.group(0).strip() for m in re.finditer(pattern,text)])

if __name__ == '__main__':
    unittest.main()
