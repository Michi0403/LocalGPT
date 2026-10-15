# LocalGPT 5.7.3 — PowerShell-Gates und Shell-/Theme-Darstellung

## Ausgangsstand

Vollständiges LocalGPT 5.7.2 Source-ZIP; keine Überschreibung des früheren manuell wiederhergestellten Quellstandes.

## Build-/Architekturkorrekturen

- `build/Assert-AsyncContinuationPolicy.ps1`: Der dedizierte **synchrone** `FileLoggerSink.ProcessLogQueue`-Thread darf seinen einzigen, auf drei Versuche begrenzten `Thread.Sleep(25 * (attempt + 1))`-I/O-Retry behalten. Ausnahme **nur für exakt diesen Aufruf mit nachgewiesenem dediziertem Thread**. Andere `Thread.Sleep`-, `Task.WaitAll`- und `Task.WaitAny`-Aufrufe werden weiter als Fehler behandelt. Der Logger selbst bleibt unverändert.
- `build/Invoke-ArchitectureAudit.ps1`: `Services/RegexEngineService.cs` ist der fachlich vorgesehene Regex-Compiler und darf `new Regex` erzeugen. Dazu ist sein `IRegexEngineService`-/`RegexRuntimeParameters`-Vertrag mit Begrenzungen für Mustersize und Timeout weiter verpflichtend. Andere Regex-Aufrufstellen bleiben durch die bestehende Guard-Liste eingeschränkt.
- `CS0006` / `WMC1006` in `LocalGPTWebviewWrapper` waren in den gelieferten Logs Folgefehler fehlender `LocalGPT.dll` nach Abbruch durch die beiden Guards; keine Wrapper-Abhängigkeit geändert.

## Theme-UI-Korrekturen

- `localgpt-theme-contract.css`: Die fünf Navigationsfarbvariablen folgen dem **gewählten Shell-Theme**, nicht mehr einer festen dunkelblauen Farbe. Office White übernimmt so helle Navigation/Dunkelschrift, dunkle Shell-Themes entsprechend ihre Dunkelpalette. Die separat ausgewählte DevExpress-Komponentenpalette bleibt unabhängig.
- `theme-switcher.css`: Der Theme-Knopf hängt nicht mehr absolut bei `top:15px` mit Nullhöhe am Fensterrand. Theme Fusion bleibt vorhanden, öffnet ein auf 960px bzw. Viewport begrenztes, Shell-themefarbenes Popup mit korrekt verstecktem Ruhezustand.
- `ThemeSwitcher.razor`: Öffnungszustand benutzt eine eindeutige `is-open`-CSS-Klasse statt der literal gerenderten Werte `True`/`False`.
- `build/Assert-ThemeArchitecture.ps1` prüft jetzt die Theme-gebundene Navigation und die beibehaltene responsive Darstellung. DevExpress 25.2.10 unverändert.

## Unverändert

- Python ist keine Voraussetzung für den Build-Architekturcheck; keine neuen Python-Audits.
- Der fachliche Python.NET-Laufzeitcode, Logger, RegexEngine und die Wrapperprojekte wurden nicht bearbeitet.
- Vollständige 5.7.2-Dokumentationsquellen bleiben im Source-ZIP enthalten.

## Testgrenze

21 statische Quell-/CSS-/MSBuild-Prüfungen bestanden; ZIP-Integrität geprüft. PowerShell 5.1, MSBuild, Razor-/C#-Compiler und realer Windows-Theme-Render wurden hier nicht ausgeführt. Ein Hard-Reload kann nötig sein, wenn Browser/DevExpress die alte Theme-CSS aus dem Cache verwenden.
