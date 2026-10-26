# LocalGPT 6.4.7 — offline verification

- 97/97 Python `tests/static` assertions passed, including attribute-form and certificate-contract checks.
- Model uses DataAnnotations + IValidatableObject with field-scoped errors; validated `EditForm` contains the retained DXListBoxes.
- This environment has no `dotnet` or PowerShell runtime; none of the MSBuild, PowerShell, installer or Windows tests were run.
- Existing physical certificate issuance and live UI interactions require acceptance testing after a Windows build.
- No GitHub or external project repository accessed.
