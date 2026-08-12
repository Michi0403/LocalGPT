# LocalGPT 2.7.1 source validation

- Source-only validation; no .NET build/restore/test/publish was executed.
- `audit_application_architecture.py --mode all`: passed.
- `audit_codegen_dxfunction_wiring.py`: passed.
- `audit_documentation_onewire_contracts.py`: passed.
- `audit_service_resilience.py`: passed for 1,763 service methods.
- `Assert-XmlDocumentationCoverage.py`: passed for 7,110 maintained declarations.
- `audit_async_continuations.py`: passed for 154 source files.
- Reported compiler-source defects were corrected at the exact C# literals (`.ps1` regex escaping and backslash character replacement).
- Live 1-Wire directory handling is present for `CapabilityResponse`, `SkillResponse`, and `SkillStateUpdate`.
- Application/installer/WebView versions are 2.7.1; wire protocol package remains 2.1.1.
