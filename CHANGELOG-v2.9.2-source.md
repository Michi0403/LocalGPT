# LocalGPT 2.9.2 source changes

- Restores the established in-message live Council status presentation that 2.9.1 unintentionally removed.
- Demotes only the separate live-session rejoin notice to a quiet non-boxed inline information row.
- Fixes Council autoscroll by recalculating the actual inner scroll-container bottom throughout the smooth-follow animation and settling once more at the latest bottom.
- Keeps 2.9.0/2.9.1 rejoin durability, role coordination, provider traces, and session persistence intact.
- Wire protocol remains 2.1.1.
