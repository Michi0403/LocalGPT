# LocalGPT 5.7.4 – Theme Fusion und Pages-Pfadkorrektur

- Behebt den plattformfremden Pfadliteral in `build/Assert-PagesArtifact.ps1` durch hostabhängige Verzeichnistrenner; `Assert-PowerShellCompatibility.ps1` überprüft den Vertrag beim Release-Preflight.
- Hinterlegt für alle 21 Shell-Themes konkrete, nicht an das separat ausgewählte DevExpress-Komponententheme gebundene Farbwerte, einschließlich Accent-Kontrast. Die Drawer-Navigation nutzt diese Shell-Palette.
- Korrigiert das Theme-Fusion-Popup: zentriert und viewportbegrenzt; die vorhandenen drei Spalten bleiben bestehen. Die Rasterbreiten werden an den tatsächlich gerenderten `DxFormLayout`-Umhüllungen gesetzt, und umbrochene Theme-Einträge werden anhand ihrer Razor-/DevExpress-Wrapper dimensioniert.
- Stellt die sichtbare Mikrofonmaske und den Options-Button bei gemischten hellen Shell- und dunklen Komponenten-Themes durch stabile Vorder-/Hintergrundkontraste sicher.
- Erweitert `Assert-ThemeArchitecture.ps1` um die vollständige Shell-Palettenabdeckung und den Mikrofon-Kontrastvertrag.
- Unverändert: DevExpress 25.2.10, .NET-Pakete, Python-Laufzeit, Controller, Datenbank-/Projektarchitektur, PublisherStudio.

**Validierungsgrenze:** CSS-Stylesheet-Parsing, 21 Palette-Verträge, MSBuild-XML und Quellverweise wurden geprüft. PowerShell 5.1/pwsh, echte Windows-Builds, interaktiver Browser-Themewechsel und Publish wurden hier **nicht** ausgeführt. Ein begonnener Chromium-Headless-Lauf ist abgelaufen und wird nicht als Erfolg gewertet.
