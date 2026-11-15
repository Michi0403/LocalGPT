# 1-Wire-Abnahme in Edge

Diese Anleitung gilt für Codex und Claude. Den aktuellen Umfang liefert die gemeinsame [Arbeitsliste](WORKLOADS.md). Vor dem Test in beiden Arbeitskopien `AGENTS.md`, `CLAUDE.md` und `git status --short` prüfen. Änderungen des Eigentümers erhalten.

## Visual Studio und Edge

1. Beide Lösungen in Visual Studio öffnen, den aktuellen Quellstand mit **Build Solution** bauen und Build-Ausgabe und Warnungen prüfen. PublisherStudio.Web und LocalGPT mit **F5** starten. Für den Wiederaufbau eines laufenden Debug-Prozesses den betroffenen Prozess in Visual Studio beenden, sobald laufende Arbeit gesichert ist. Beide tatsächlichen Produktversionen und Startadressen notieren. Build- und PowerShell-Helfer dürfen Änderungen vorab prüfen. Eine Debug-Sitzung allein belegt keinen installierten Release.
2. Die gestarteten Seiten in Edge öffnen und eine leere PublisherStudio-Testpublikation verwenden. Bei mehreren Tabs URL, Dokument und aktiven Editor vor jeder Aktion prüfen. Im PublisherStudio-Dialog **Reconnect** für die bereits gekoppelte Testverbindung und normale Testeingaben sind durch den Eigentümer für diesen Ablauf gestattet. Bei neuer Kopplung, geänderten Berechtigungen, Browsersteuerungs- oder Dokumentfreigaben den konkreten Dialog und seine Optionen melden. Die Entscheidung bleibt beim Eigentümer. **Connected and linked** abwarten und prüfen, ob der Funktionskatalog verfügbar ist. Ein Transportstatus allein reicht für die Abnahme nicht aus.
3. In LocalGPT **Chat configuration → AI Council** öffnen und direkt ein als **Reachable** angezeigtes Ollama-Modell bis 4B auswählen, etwa `qwen3.5:4b` auf dem lokalen Host, wenn vorhanden. Andere Council-Modelle für den Test abwählen und in der Übersicht prüfen, dass genau eines aktiv ist. Den tatsächlichen Modellnamen im gestarteten Lauf abgleichen. Das jeweils passende Council-Team in der Oberfläche wählen. Präsenz und Bedienbarkeit jeder Schaltfläche vor dem nächsten Klick prüfen. Bei UI-Fehlern den betroffenen Dialog vor und nach der Korrektur in Edge öffnen. Im Spreadsheet-Team muss das Workbook-Raster unter dem KI-Formular sichtbar sein.

## Standardtests nach jedem neuen Build

| Test | Eingabe über die sichtbare Oberfläche | Erwarteter Beleg |
| --- | --- | --- |
| Excel aus LocalGPT | Im LocalGPT-Chat eine Excel-Arbeitsmappe `W1-Test` mit `Artikel,Anzahl`, `Apfel,2`, `Birne,3` in der offenen PublisherStudio-Testpublikation anfordern, mit XLSX-Download im Chat. | PublisherStudio zeigt die Arbeitsmappe im richtigen Editor. Der Chat enthält einen funktionierenden XLSX-Download. Die geöffnete Datei enthält die erwarteten Werte. |
| Excel aus PublisherStudio | Die Testmappe öffnen. Über **LocalGPT-KI → Spreadsheet Team** oder **AI Assist selected object** im Tabellenkontext anfordern, B2 auf 4 zu setzen und in B4 die Formel `=SUM(B2:B3)` einzutragen. | LocalGPT bearbeitet die sichtbare Tabelle im vorgesehenen Browser-Tab. Im Tabelleneditor stehen B2 = 4, B4 = 7 und die erwartete Formel. Eine zweite offene Publikation behält ihre Werte. |
| Bildauswahl | Im LocalGPT-Chat „Erstelle ein Bild von einem Elefanten“ eingeben. Beide angebotenen Wege auf getrennten Testläufen prüfen. | LocalGPT bietet Modellgenerierung und Zeichnen in PublisherStudio an. Für die Modellgenerierung erklärt es bei Bedarf, wie ein geeignetes lokales Modell eingerichtet wird. Das erzeugte Asset ist im gewählten Pfad sichtbar und lässt sich öffnen. |
| Zeichnen in PublisherStudio | Im Chat ausdrücklich „Zeichne einen Elefanten im geöffneten PublisherStudio-Bildeditor“ anfordern. | LocalGPT bedient die sichtbaren Werkzeuge im vorgesehenen Editor und die Zeichnung erscheint im Testdokument. |
| Weitere Medien und Formate | Den Live-Katalog der verbundenen Apps verwenden. Für jede angebotene Funktion die beabsichtigte Aktion sowie das unterstützte Zielformat über die Oberfläche prüfen. | Das Ergebnis lässt sich im erwarteten Editor anzeigen oder herunterladen und im gewählten Format öffnen. |

| Word in beiden Richtungen | Ein DOCX mit Überschrift und zwei Absätzen im LocalGPT-Chat und separat im PublisherStudio-Editor anfordern. | Das Dokument öffnet sich im vorgesehenen Editor. Der Download im Chat öffnet sich als DOCX und enthält den erwarteten Text. |
| Bild zurück an PublisherStudio | In PublisherStudio ein lokal generiertes Bild für die offene Testpublikation anfordern. Den Lauf und die Ausgabe in LocalGPT prüfen und in PublisherStudio zur Testpublikation zurückkehren. | Das generierte Bild erscheint in der vorgesehenen Publikation, öffnet sich im Bildeditor und bleibt nach erneutem Öffnen sichtbar. |
| Video zurück an PublisherStudio | In PublisherStudio einen kurzen lokal generierten Testclip anfordern. Den Lauf und die Ausgabe in LocalGPT prüfen und in PublisherStudio zur Testpublikation zurückkehren. | Der Clip erscheint in der vorgesehenen Publikation und lässt sich im Videoeditor abspielen. |
| Vorschau im Chat | Ein Bild, ein Video und eine kurze Text- oder Quelldatei im Chat anzeigen. | Die Vorschau lässt sich öffnen und schließen. Das Video startet durch Bedienung. Quelltext erscheint als Text. |
| Kernel Creature Tournament | In LocalGPT das Turnierteam wählen, erreichbare leistungsfähige lokale Modelle für die Rollen auswählen und ein Turnier starten. | Team- und Modellwahl bleiben zugänglich. Das Terminal öffnet sich beim Start. Die Sprites und Paletten sind sichtbar, eine Runde lässt sich spielen und das Ergebnis wird angezeigt. |

Ein Testlauf verwendet harmlose Daten und überprüft genau einen Arbeitsablauf. Beschreibungen, Vorschläge und Modellbehauptungen sind Hinweise. Sichtbare Ergebnisse, tatsächliche Downloads und geprüfte Dateiinhalte sind die Abnahmebelege. Build-Ausgabe, Debugger und 1-Wire-Journal stützen die Diagnose. API-Aufrufe ersetzen den sichtbaren Ablauf nicht.

## Delegation und Protokoll

Klar begrenzte Tests, Quellsuche und Logauswertung an die günstigsten verfügbaren geeigneten Modelle delegieren. Bei gemeinsamer Maus und Tastatur einen Agenten pro Edge-Sitzung einplanen. Parallel dazu können andere Agenten gezielt lesen und prüfen. Im Auftrag jeweils den konkreten Testfall, die Testdaten und den erwarteten Beleg angeben. Der Hauptagent prüft Modellwahl, Freigabegrenzen und Ergebnisse.

Je Fall Build- und Produktversion, Team, tatsächliches Modell, sichtbaren UI-Status und bei Bedarf die 1-Wire-Korrelations-ID festhalten. `PASS` steht für einen vollständig belegten Ablauf, `FAIL` für eine beobachtete Abweichung und `BLOCKED` für eine konkret benannte fehlende Voraussetzung. Sensible Nutzdaten, Schlüssel und vollständige Prompt- oder Log-Dumps aus dem Protokoll heraushalten.

## Gemma-Codingworkflow in Edge

1. Build the LocalGPT solution in Visual Studio with the repository PowerShell gates enabled. Start the LocalGPT debug profile and open Local Chat in Edge.
2. In Chat configuration, select AI Council and only the local Ollama model gemma4:26b. Enable Council downloadable artifacts. For the scratch test, disable Council memory and set the context to 16,384 tokens, the answer budget to about 4,096 tokens, and peer review rounds to zero. Confirm the actual model and context in the live lane.
3. Ask for a downloadable, dependency-free .NET console project named EfDatabaseIdentitySmoke. Request an injectable instance service for the SQLite database name and absolute Data Source path, with explicit handling of :memory:. Ask LocalGPT to build and run self-tests for relative paths, absolute paths and :memory:, then deliver the sources and actual execution status. Use a scratch project.
4. If the source curator or compiler operator requests council.x.revisit, use Approvals & team to review the exact action. Approve the compiler-generate repeat once. Confirm the originating step records the deferred execution outcome and the workflow enters a new Generation revision. To exercise declining, use a separate run and confirm the request closes without executing the repeat.
5. A passing coding run displays successful artifact verification, actual build and test results, and a working ZIP download from the chat. Record the run ID, actual model settings, first failure if any, and whether a repeat executed. A scratch project checks the coding workflow. Use the live installation information in Edge to check LocalGPT's own configured database path.

