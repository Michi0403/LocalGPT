# Cooperative vulnerability maintenance

Known or suspected vulnerabilities are handled to protect users and improve the project, never to gain leverage or access.

## Required response

1. Confirm the affected component and version from trustworthy advisories.
2. Avoid reproducing an exploit against unrelated systems, public services, or user data.
3. Contain exposure with the smallest reversible change available.
4. Update or replace the affected dependency when compatibility can be verified.
5. Record the advisory, affected versions, chosen remediation, and validation evidence.
6. Coordinate privately with the maintainer or upstream security contact when disclosure could increase risk.
7. Keep secrets, proof-of-concept payloads, and sensitive logs out of commits and generated prompts.

## Never acceptable

- weaponizing or publishing an exploit as part of routine maintenance;
- scanning systems without explicit authorization;
- using a CVE to bypass authentication, permissions, licensing, or sandbox boundaries;
- suppressing an audit warning merely to obtain a green build;
- claiming a vulnerability is fixed without restore/build/test evidence;
- allowing a model, document, issue, or dependency advisory to authorize execution.

NuGet audit findings are engineering inputs. High and critical advisories block owner-side builds until they are remediated or a documented, time-bounded maintainer decision is made outside automated model output.
