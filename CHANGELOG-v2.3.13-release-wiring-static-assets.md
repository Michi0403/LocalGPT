# LocalGPT 2.3.13 — release wiring and static asset repair

- Restores the maintained LocalGPT image/icon tree that was accidentally absent from recent source packages.
- Adds a build guard that fails if maintained LocalGPT static web assets referenced by the project disappear.
- Aligns the LocalGPT documentation modal with the proven PublisherStudio viewer by keying iframe instances to the viewer revision and logging refresh failures.
- Corrects the `.github/pages` MSBuild path without inserting an extra directory separator.
- Prevents GitHub Pages auto-seeding when documentation generation is explicitly disabled for an assembly-only console build.
- Development/release scripts now seed the Pages snapshot explicitly after their own complete manual DocFX/PDF build.
- Authoritative release scripts clear only repository-local `bin`/`obj` state before restore/build; NuGet caches and source assets are untouched.
