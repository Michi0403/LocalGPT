# LocalGPT 3.4.0

LocalGPT 3.4.0 is the **Cross-Platform Backend Boundaries** release.

This release removes unused Windows-only application dependencies and moves host-sensitive filesystem, console, hardware, and secret-file behavior behind dependency-injected Windows/Unix services. Physical-path security checks now follow the actual host filesystem semantics instead of assuming Windows-style case-insensitivity.

The release also adds a source-level cross-platform guard to prevent common services from reintroducing direct OS branching, Windows executable/environment selection, or known unsafe path-containment patterns.

Documentation build/runtime improvements from 3.3.3 through 3.3.9 remain intact.

This handoff is source-only and was not built with .NET or executed with PowerShell in the packaging environment. See `CHANGELOG-v3.4.0-CROSS-PLATFORM-BACKEND-BOUNDARIES.md` and `VALIDATION-v3.4.0-source.md`.
