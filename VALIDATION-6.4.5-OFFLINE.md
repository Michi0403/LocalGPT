# LocalGPT 6.4.5 offline validation

- LocalGPT full source static regression suite: 87 tests passed (`python -m unittest discover -s tests/static -p test*.py`).
- Version: LocalGPT 6.4.5, installer 4.9.9 (legacy payload folder recovery).
- Two independent `DxListBox` multi-selector controls for X.509 Key Usage and Extended Key Usage, each supports checkbox selection.
- Key size, hash, optional store location and store name all exposed as single-selection `DxListBox` controls.
- Request preseeded with RSA-3072 / SHA-256, DigitalSignature + KeyEncipherment, ServerAuthentication, CurrentUser/My, no automatic store install, and application-data certificate path.
- Service maps every extended-usage enum to a standards-defined OID, aggregates selected key-usage flags, rejects invalid CA and TLS-listener usage combinations before writing a PFX.
- Added/extended a fail-closed PowerShell certificate editor and issuance guard, still executed from normal `Directory.Build.targets` and `Build-Release.ps1`.
- Added localization keys to all six language catalogs.

## Not verified

- No .NET or PowerShell runtime is installed in this container; **C# compile**, **PowerShell execution**, **real certificate issuance/inspection**, **actual Windows installer**, and **InteractiveServer DevExpress UI rendering** were not performed. This is an offline source-level release candidate.

- Source regression added: legacy wrapper recovery requires complete canonical install, exact generated names plus payload markers, only top-level directory enumeration and lossless quarantine move.
- Runtime Windows installer execution remains unverified in this Linux environment.
