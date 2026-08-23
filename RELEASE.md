# LocalGPT 3.2.9

LocalGPT 3.2.9 is a database-workbench, relationship-model and lifecycle-hardening release. It keeps the 3.2.8 DX Functions render repair and applies the same lessons to the broader application without changing PublisherStudio.

The SQLite Database page now uses the newer responsive workbench layout with separate **Knowledge & relationships** and **SQLite tables** panels. Knowledge and generic database records use semantic selection labels instead of making raw row identifiers the primary way to find data. Council knowledge can now be explicitly related to reusable RegEx patterns with a purpose and human-readable meaning, and the previously difficult-to-reach project/topic knowledge links are editable from the same knowledge workbench.

The EF object graph now restores reverse navigation for persisted foreign keys that were already present in the database, while deliberately leaving correlation-style and currently soft identifiers unchanged. The supplied database passed SQLite integrity and foreign-key checks; the release therefore repairs application navigation/accessibility rather than inventing a broad destructive relationship migration.

Lifecycle review found another post-await first-render update in the responsive drawer and hardened the narrow asynchronous-disposal paths that can legitimately race with browser/circuit shutdown. The user's `IJSObjectReference.IsDisposed()` guard in `ThemeJsChangeDispatcher` is retained; expected disconnect, cancellation and already-disposed teardown outcomes no longer become noisy failures there or in the ASCII game-console teardown. Streaming enumerator cleanup is also tolerant of requested cancellation/already-disposed races while unexpected cleanup faults remain visible.

See `CHANGELOG-v3.2.9-DATABASE-KNOWLEDGE-RELATIONSHIPS-LIFECYCLE-HARDENING.md`, `DATABASE-RELATIONSHIP-ANALYSIS-v3.2.9.md`, `HISTORICAL-CAPABILITY-REVIEW-v3.2.9.md`, and `VALIDATION-v3.2.9-source.md`.

PublisherStudio remains at **2.9.7** and is unchanged by this release.
