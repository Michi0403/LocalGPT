# Logging and diagnostics

Structured diagnostics are part of application behavior. Refactoring may simplify logging, but it should preserve enough information to understand failures, cancellations, and important state transitions without exposing prompts, generated source, credentials, or private data.

Use stable event wording and safe identifiers. Expected cancellation should be logged at an appropriate level rather than presented as a user fault. Exceptions that affect an operation should reach both the local diagnostic log and a clear user-facing notification where applicable.

Validation scripts under `build/` are optional developer tools and are not injected automatically into normal restore, build, or publish operations.
