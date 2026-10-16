# LocalGPT 5.8.7 static validation

- Base archive: `LocalGPT-5.8.6-source.zip` provided within this conversation.
- Output archive: `LocalGPT-5.8.7-source.zip`, complete source repository.
- No online repository access, no .NET compilation, and no Windows or PowerShell runtime test.
- XML, source-retention, expected editing/safety-policy tokens, config/enum backward compatibility, strict-broker isolation, seeded tool access, and ZIP CRC are checked before delivery.

## Execution cases to run under Windows

| Workflow / policy | Native metadata | Broker availability | Expected behavior |
| --- | --- | --- | --- |
| General/Learning, no preference list | Broker | Yes | Policy-approved functions reachable by discovery and invocation |
| Ordinary, step or team preference list | Preferences + broker | Yes | Preferred tools callable directly; nonpreferred tools discoverable |
| Ordinary, AllPolicyApproved, saved preferences | Combined step/team preferences + broker | Yes | Same broad access with direct preferred tools |
| Ordinary, StrictStepAllowList / StrictTeamAllowList | Exclusively named nonbroker functions | No | All other names rejected, including direct broker names |
| `games.*` runtime, historical TeamAllowList/ExactAllowList | Exclusive historical list | No | No behavior regression for saved game runtime policies |
| Disabled/tool suppression | None | No | No automatic/native tools |
| Software maintenance | Authorized tools | According to policy | Diagnose automatically, use safe repairs, await required human approvals, verify effects |

See `CHANGELOG-v5.8.7-COUNCIL-TOOL-PREFERENCES.md` for the details and limitations.
