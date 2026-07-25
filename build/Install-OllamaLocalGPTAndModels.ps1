[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# DisabledLegacyBootstrap
#
# This historical all-in-one script previously downloaded software and repositories,
# modified user environment settings, pulled models, extracted archives, and started
# local processes. Those side effects are too broad for a repository helper and are
# intentionally disabled.
#
# Use LocalGPTInstallerConsole with explicit command-line options, or follow the
# documented manual installation steps. Review every requested network, filesystem,
# process, and persistence effect before approving it.

Write-Warning 'This legacy bootstrap is disabled. Use LocalGPTInstallerConsole or the documented manual installation flow.'
exit 2
