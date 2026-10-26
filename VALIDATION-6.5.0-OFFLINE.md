# LocalGPT 6.5.0 offline validation

`python -m unittest discover -s tests/static -p 'test_*.py' -q`: 114 tests passed.

Core source contracts include checked-expression binding for the certificate CA field, separately marked computed-checkbox states, fresh public pairing ticket when entering the Pairing section, explicit/no automatic MFA disclosure, removed Install scroll controls, and a remote transport configuration preview that does not claim TLS is active.

Build and release both invoke the new PowerShell guard; earlier PowerShell guards remain unmodified.

The test environment has no `dotnet`, `pwsh`, or Windows PowerShell. A Windows build, restored DevExpress components, live HTTPS inspection, and a running browser session were **not** tested. Confirm all of these before installing over an existing release.
