# Installer launcher policy

The source repository does not ship one-click `.cmd` launchers for installation, forced deletion, model downloads, learning-base imports, or startup. Those launchers made consequential operations too easy to trigger without reviewing their combined effects.

Run `LocalGPTInstallerConsole` manually with only the explicit options required for the current task. Review its help output first. Forced deletion, network downloads, model pulls, learning-base changes, and application startup are separate human decisions.
