# Offline validation LocalGPT 6.4.4
- Python `unittest discover -s tests/static -p 'test_*.py'`: 80 tests passed.
- Mandatory PowerShell installer-layout and TLS contract scripts are wired into Directory.Build.targets and Build-Release.ps1.
- App and installer project versions updated.
- Tested archive ZIP integrity; no .NET, Kestrel or Windows installer execution in this environment.
