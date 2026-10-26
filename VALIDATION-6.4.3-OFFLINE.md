# Validation — LocalGPT 6.4.3

- Based on LocalGPT 6.4.2 source ZIP; 6.0.6 source compared for original installer contract.
- `python -m unittest discover -s tests/static -p 'test_*.py'`: 75/75 passed.
- Installer fixture tests cover versioned ZIP, canonical ZIP, legacy flat ZIP and rejected nested executable.
- Source guard added and wired to both normal builds and release preflight.
- Windows PowerShell, .NET build, actual Windows installation, live publisher trust and Council testing **NOT performed** in this environment.
- The PowerShell guard was source-checked but could not be executed without PowerShell.
