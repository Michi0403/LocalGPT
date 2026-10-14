# LocalGPT 5.0.5 — DevExpress and localization retention

- Keeps the restored DevExpress `DxComboBox` language selector and protects the maintained Razor UI from being simplified back to native interactive HTML.
- Adds a source-controlled DevExpress component-retention baseline plus a build gate. The gate fails when a protected component loses DevExpress tags or gains native `button`, `input`, `select`, `option`, or `textarea` controls beyond its reviewed allowance. The only retained native interactive controls are the two circuit-independent reconnect actions in `App.razor`.
- Keeps Layout Studio on the supported DevExpress layout family (FormLayout, GridLayout, StackLayout, Tabs, Splitter and Carousel) and fixes the Razor `RZ9986` mixed-content `CssClass` regression by moving the carousel class composition into a property.
- Strengthens localization maintenance: all maintained culture catalogs must keep key parity with English, German may not gain new untranslated English-identical values unless explicitly reviewed as language-neutral, and required UI strings remain guarded.
- Expands localization coverage for the recently changed chat/install/layout surfaces while preserving existing InteractiveServer boundaries and code style.
- Advances the application version from 5.0.4 to 5.0.5 and refreshes the microphone module cache-buster.

This source handoff was prepared without running dotnet/MSBuild/NuGet or using GitHub.
