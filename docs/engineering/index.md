# Engineering guidance

This section collects the rules that keep LocalGPT maintainable after the architecture has become interesting enough to bite back.

- [Build and validation](build-validation.md)
- [Release and documentation pipeline](release-and-docs.md)
- [Maintenance status](maintenance-status.md)

## Working principle

Use the strongest available evidence and label weaker evidence honestly:

```
real build/test > parser/static validation > focused inspection > assumption
```

A capability gap should report the requested outcome, missing dependency or source, evidence inspected, safe next step, and whether owner-side tooling or approval is required.

## Open work

Current owner-side verification tasks live on the [maintenance status](maintenance-status.md) page. Historical task lists and pass manifests remain under `docs/internal-notes` and are excluded from the reader site.
