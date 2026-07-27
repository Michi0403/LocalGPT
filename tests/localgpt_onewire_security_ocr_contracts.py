import json
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]

def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")

class LocalGptOneWireSecurityOcrContracts(unittest.TestCase):
    def test_runtime_identity_is_user_resettable_and_not_compiled(self):
        service = read("LocalGPTWebviewWrapper/LocalGPT/Services/OneWire/OneWireRuntimeSecurityService.cs")
        page = read("LocalGPTWebviewWrapper/LocalGPT/Components/Pages/OneWireSecurity.razor")
        self.assertIn("RandomNumberGenerator.GetBytes", service)
        self.assertIn("RegenerateAsync", service)
        self.assertIn("DeleteAsync", service)
        self.assertIn("onewire-secret.json", service)
        self.assertIn("Create identity", page)
        self.assertIn("Delete identity", page)
        self.assertIn("localGptOneWireSecurity.renderQr", page)
        self.assertIn("qrcode-generator.js", read("LocalGPTWebviewWrapper/LocalGPT/Components/App.razor"))
        self.assertNotIn("MfaSeed", read("LocalGPTWebviewWrapper/LocalGPT.WireProtocolVersion/OneWireProtocolContracts.cs").split("OneWirePairingTicket", 1)[1].split("OneWireRuntimeSecurityStatus", 1)[0])

    def test_protocol_supports_external_transports_and_compact_security(self):
        protocol = read("LocalGPTWebviewWrapper/LocalGPT.WireProtocolVersion/OneWireProtocolContracts.cs")
        for value in ("Http", "Mqtt", "Uart", "Spi", "Custom", "EncryptedPayload", "EncryptionNonce", "AuthenticationTag", "Signature"):
            self.assertIn(value, protocol)
        controller = read("LocalGPTWebviewWrapper/LocalGPT/Controller/OneWireHttpController.cs")
        self.assertIn('api/onewire/http-json', controller)
        self.assertIn('MaximumMessageBytes', controller)
        security = read("LocalGPTWebviewWrapper/LocalGPT/Services/OneWire/OneWireRuntimeSecurityService.cs")
        self.assertIn("HkdfSha256", security)
        self.assertIn("orderedFingerprints", security)
        self.assertIn("orderedPeers", security)
        self.assertIn("HMACSHA256", security)
        self.assertIn("ECDiffieHellman", security)
        self.assertIn("AesGcm", security)

    def test_text_screen_web_and_ocr_capabilities_are_teachable(self):
        functions = read("LocalGPTWebviewWrapper/LocalGPT/Services/OneWire/OrganicPluginDxAiFunctions.cs")
        for key in ("publisher.text.feedback.request", "publisher.screen.capture.request", "publisher.screen.record.request", "publisher.website.content.request"):
            self.assertIn(key, functions)
        catalog = read("LocalGPTWebviewWrapper/LocalGPT/Services/OneWire/OneWireCapabilityCatalog.cs")
        self.assertIn("localgpt.vision.ocr", catalog)
        self.assertIn("SuggestedCouncilRoles", catalog)
        ocr = read("LocalGPTWebviewWrapper/LocalGPT/Services/OneWire/LocalVisionOcrService.cs")
        self.assertIn("DeepSeek OCR", ocr)
        self.assertIn("NeedsHumanReview = true", ocr)
        preflight = read("LocalGPTWebviewWrapper/LocalGPT/Services/Council/CouncilPreflightService.cs")
        self.assertIn("CapabilityTeachings", preflight)
        self.assertIn("handle ApprovalRequired without reissuing", preflight)
        self.assertIn("publisher.website.content.request", functions)
        self.assertIn("same correlation", read("docs/ORGANIC_AI_COUNCIL_BLUEPRINT_2_1.md").lower())

    def test_build_is_first_run_ordered_and_protocol_21(self):
        build = read("Build-LocalDevelopment.ps1")
        self.assertIn('"2.1.0"', build)
        self.assertLess(build.index('Building the authoritative RID-neutral protocol project first'), build.index('Building LocalGPT in deterministic project order'))
        self.assertNotIn('-p:Platform=AnyCPU") + $appProperties', build)
        self.assertIn('--force-evaluate', build)
        self.assertIn('-maxcpucount:1', build)
        self.assertIn('-p:BuildProjectReferences=false', build)
        cmd = read("Build-LocalDevelopment.cmd")
        self.assertIn('pushd "%~dp0"', cmd)
        self.assertIn('pause', cmd)
        self.assertNotIn('SetPlatform="AnyCPU"', read("LocalGPTWebviewWrapper/LocalGPT/LocalGPT.csproj"))
        self.assertNotIn('SetPlatform="AnyCPU"', read("LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj"))


    def test_security_contract_aliases_and_ocr_settings_compile_surface(self):
        aliases = read("LocalGPTWebviewWrapper/LocalGPT/GlobalUsings.OneWire.cs")
        self.assertIn("global using LocalGPT.WireProtocol;", aliases)
        protocol = read("LocalGPTWebviewWrapper/LocalGPT.WireProtocolVersion/OneWireProtocolContracts.cs")
        for name in (
            "OneWireRuntimeSecurityStatus",
            "OneWireSecurityDescriptor",
            "OneWirePairingTicket",
            "OneWireTrustEstablishmentRequest",
            "OneWireTrustedPeerDescriptor",
        ):
            self.assertIn(name, protocol)
        ocr = read("LocalGPTWebviewWrapper/LocalGPT/Services/OneWire/LocalVisionOcrService.cs")
        self.assertIn("IOptionsMonitor<LocalGPT.BusinessObjects.ConfigurationRoot>", ocr)
        http_controller = read("LocalGPTWebviewWrapper/LocalGPT/Controller/OneWireHttpController.cs")
        self.assertIn("using LocalGPT.Services.OneWire;", http_controller)
        self.assertIn("OneWireMessageDispatcher.LocalAdvertisement()", http_controller)
        execution_services = read("LocalGPTWebviewWrapper/LocalGPT/Services/OneWire/OneWireExecutionServices.cs")
        approval_ctor = execution_services.split("public sealed class OneWireCouncilApprovalProcessorHostedService(", 1)[1].split(") : BackgroundService", 1)[0]
        self.assertNotIn("IOneWireCapabilityCatalog capabilities", approval_ctor)
        release_cmd = read("Build-Release.cmd")
        self.assertIn('pushd "%~dp0"', release_cmd)
        self.assertIn("pause", release_cmd)

    def test_docs_define_runtime_not_compiled_secret_and_esp_flow(self):
        docs = read("docs/ONEWIRE_RUNTIME_SECURITY_HTTP_JSON.md")
        self.assertIn("No private key", docs)
        self.assertIn("ESP32", docs)
        self.assertIn("same `CorrelationId`", docs)

if __name__ == '__main__':
    unittest.main()
