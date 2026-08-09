# LocalGPT current documentation delivery contract

Release and GitHub Pages packaging must use the documentation generated for the exact application version being built.

The contract is intentionally fail-fast and non-repairing:

- generated `documentation-status.json` must report the exact source/application version;
- exactly one versioned product PDF may exist in the documentation payload, and it must match that version;
- the physical DocFX API entry point must exist before release packaging;
- release packaging replaces `wwwroot/help-docs` with the verified generated documentation cache before validation;
- the final publish folder is validated again immediately before archive creation;
- the tracked GitHub Pages snapshot accepts only a version-matched generated documentation root and rejects mixed-version PDF payloads.

No older documentation tree is used as a fallback for a newer build. A missing current documentation build fails instead of silently publishing a stale snapshot.
