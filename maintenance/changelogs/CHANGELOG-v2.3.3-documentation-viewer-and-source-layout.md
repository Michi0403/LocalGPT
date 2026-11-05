# LocalGPT 2.3.3

- Opens shipped HTML, PDF, API, XML-comment, and status documentation inside a contained in-app viewer.
- Keeps the running LocalGPT route and interactive session intact while documentation is open.
- Publishes generated help documentation only to `src/LocalGPT/wwwroot/help-docs`.
- Removes active maintenance references to the obsolete pre-normalization source layout.
- Aligns application, installer, desktop wrapper, documentation, Pages, Council audit, provider audit, and 1-Wire-facing version metadata to 2.3.3.
- Limit documentation discovery to canonical shipped help-docs roots, correct in-app chapter routes, and stop stale or obsolete LocalGPTWebViewWrapper trees from being selected or recreated by documentation handling.
