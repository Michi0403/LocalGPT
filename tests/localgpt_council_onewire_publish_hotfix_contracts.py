#!/usr/bin/env python3
from pathlib import Path
import json
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"
PROTOCOL = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT.WireProtocolVersion"


def text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def main() -> None:
    program = text(APP / "Program.cs")
    registry = text(APP / "Services" / "DxAiFunctionRegistry.cs")
    directory = text(APP / "Services" / "Council" / "RuntimeCapabilityDirectoryService.cs")
    catalog = text(APP / "Services" / "DxAiFunctionCatalogService.cs")
    transport = text(APP / "Services" / "OneWire" / "OneWireTransportHostedServices.cs")
    execution = text(APP / "Services" / "OneWire" / "OneWireExecutionServices.cs")
    app_project = text(APP / "LocalGPT.csproj")
    protocol_project = text(PROTOCOL / "LocalGPT.WireProtocolVersion.csproj")
    protocol = text(PROTOCOL / "OneWireProtocolContracts.cs")
    settings = json.loads(text(APP / "appsettings.json"))

    assert "AddSingleton<IHardwareInventoryService, HardwareInventoryService>()" in program
    assert "IServiceProvider serviceProvider" in registry
    assert "Lazy<IReadOnlyDictionary<string, IDxAiFunctionHandler>>" in registry
    assert "serviceProvider.GetServices<IDxAiFunctionHandler>()" in registry

    gate_position = directory.index("SynchronizationGate.WaitAsync")
    catalog_position = directory.index("functionCatalog.SynchronizeAsync")
    assert gate_position < catalog_position, "The full capability refresh must be serialized."
    assert "CreateDbContextAsync" in directory
    assert "DbUpdateConcurrencyException" in directory
    assert "Council execution continues" in directory
    assert "ExecuteUpdateAsync" in directory
    assert "private static readonly SemaphoreSlim Gate" in catalog
    assert "await Gate.WaitAsync" in catalog

    discovery_type = transport[transport.index("public sealed class OneWireDiscoveryHostedService"):]
    assert "MaximumDiscoveryBytes" in discovery_type
    assert "IPAddress.Loopback" in discovery_type
    assert "GetLocalCapabilitiesAsync" not in discovery_type
    assert "Capabilities =" not in discovery_type

    direct_ack = execution[execution.index("case OneWireMessageType.Hello:"):execution.index("case OneWireMessageType.CapabilityRequest:")]
    assert '"CapabilityDirectoryTransport"' in direct_ack
    assert '"Capabilities"' not in direct_ack
    capability_response = execution[execution.index("case OneWireMessageType.CapabilityRequest:"):execution.index("case OneWireMessageType.SkillRequest:")]
    for token in ['["Capabilities"]', '["Skills"]', '["UiFeatures"]', '["Hardware"]']:
        assert token in capability_response

    assert "MaximumMessageBytes = 8 * 1024 * 1024" in protocol
    assert "MaximumDiscoveryBytes = 32 * 1024" in protocol
    assert settings["OneWire"]["MaximumMessageBytes"] == 8 * 1024 * 1024
    assert settings["LoggingCore"]["DatabaseCore"]["CoreLogLevel"] == 3

    ET.fromstring(app_project)
    ET.fromstring(protocol_project)
    assert "GlobalPropertiesToRemove=\"Platform;PlatformTarget;RuntimeIdentifier;RuntimeIdentifiers;SelfContained" in app_project
    assert "BeforeTargets=\"ComputeFilesToPublish\"" in app_project
    assert "PackageReference Include=\"LocalGPT.WireProtocolVersion\"" in app_project
    assert "<GeneratePackageOnBuild>false</GeneratePackageOnBuild>" in protocol_project

    print("LocalGPT Council, compact 1-Wire discovery, DI, and RID-safe publish source contracts passed.")


if __name__ == "__main__":
    main()
