# LocalGPT 2.4.3 — UI/documentation repair build fix

- Preserves the 2.4.2 first-start styling, compact Chat configuration, saved-chat recall and in-app documentation viewer repair.
- Uses `ConfigureAwait(false)` in the two non-renderer-affine saved-conversation helpers so the existing async-continuation audit remains authoritative.
- GitHub Pages and documentation generation/deployment scripts are unchanged from 2.4.2.
