# Gesamtinventur der Razor-Formularbereiche — LocalGPT 6.4.9 / PublisherStudio 4.6.2

**Prüfart:** Quellcode-Inventur sämtlicher aktueller `.razor`-Dateien beider bereitgestellter Archive. Keine .NET-/Razor-Kompilierung, kein echter UI-Interaktionslauf. Zählungen sind Näherungen anhand deklarierter Razor-Tags; sie bestätigen nicht die Laufzeitvalidierung.

`DxFormLayout` strukturiert die Darstellung, führt aber **keine automatische DataAnnotations-Validierung** durch. Für eine automatisch validierte Interaktion sind `EditForm`/`EditContext` plus Validator/Model-Regeln und ggf. HTTP-Validierungsantwort erforderlich. Daher ist „kein EditForm“ ein Prüfhinweis, **kein pauschaler Defekt** (viele Bereiche sind rein darstellend, imperativ oder durch Services validiert).

| Projekt | Formular-/Layout-Komponenten | EditForm | FormLayout | DX-Editoren | Native Eingaben | Komponenten ≥ 1.000 Zeilen |
|---|---:|---:|---:|---:|---:|---:|
| LocalGPT | 59 | 1 | 180 | 748 | 0 | 9 |
| PublisherStudio | 37 | 1 | 154 | 122 | 672 | 18 |

## Bereits gekapselte Verantwortlichkeiten

- **LocalGPT:** `NetworkCertificateEditor` ist eigenständige Razor-Komponente mit `NetworkCertificateCreateRequest` (`DataAnnotations`, `IValidatableObject`), `INetworkCertificateService` und `NetworkCertificateController` (automatische HTTP-400-Validierung). `InitialSetupHardwareForm` übernimmt die hardwarebezogenen DX-Eingaben; Hardwareerkennung, Upload-Verarbeitung und Speichern bleiben vorerst als explizit übergebene Callbacks beim existierenden Setup-Service-Workflow (keine neue Parallelimplementierung).
- **PublisherStudio:** `OrganicCouncilRequestForm` ist eine eigene Subkomponente mit `EditForm`, DevExpress-Editoren, DataAnnotations-`OrganicCouncilRequestFormModel` als BusinessObject, getrenntem `OneWireCouncilRequest`-Transportvertrag, 1-Wire-Servicesubmission und vorhandenen Notifications. Die bestehende Controller-Route validiert dieselben Formularregeln und liefert HTTP 400/ValidationProblem bei ungültigen Werten. Die HTTP-Route für Council-Requests bleibt in `OrganicPluginController` bestehen. Zugriffs- und Zustimmungsschutz bleiben erhalten.

## Risikoprofil und empfohlenes weiteres Entkopplungsverfahren

1. Die größten Komponenten sind **nicht automatisch ein einzelnes Formular**. Vor Auslagerungen Renderzustand, `EditContext`, ererbten InteractiveServer-Circuit, EventCallbacks, Controller-Route und persistierte BusinessObjects identifizieren. Dies gilt besonders für `InspectorPanel`, `Editor`, `PublicationRibbon`, `PanelStudio`, `Database`, `TestLab`, `Projects`, `Install`.
2. Alle Eingabeabschnitte mit eigenem BusinessObject und klarer Aktion zuerst als eigenständig wiederverwendbare `.razor`/`.razor.cs`-Komponente aufteilen. HTTP-Controller kapseln den passenden API-Einstieg; unter InteractiveServer sind direkte DI-Serviceaufrufe zulässig, sofern dieselben Modell- und Sicherheitsregeln gelten.
3. Je Formular ein Regressionstest über EditContext/invalid/valid/HTTP 400/Notifier/Submit-Duplikate und ein echter Build/Browser-Test. Keine pauschale Umstellung aller Controls, da dies die etablierten DevExpress-Funktionen gefährden könnte.
4. Die **globale** Eins-zu-eins-Abdeckung aller öffentlichen Service-Methoden durch Controller wurde hier nicht abschließend bewiesen. Sie benötigt eine eigenständige semantische Methodensignatur-/Controller-Routen-Prüfung; aus bloßer Zahl von `DxFormLayout`-Tags kann das nicht abgeleitet werden.

## Dateiweise Inventur aller gefundenen Formular-/Layout-Komponenten

Legende: „EditForm=0“ weist nur auf fehlende deklarative EditForm-Grenze hin, nicht auf einen automatisch nachgewiesenen Validierungsfehler. Die vollständigen Rohdaten liegen in `FORM_AUDIT_ALL_COMPONENTS.csv`.

### LocalGPT

| Datei | Zeilen (Razor / Code-behind) | Route | FormLayout / EditForm | DX / native Eingaben | Validierungs-Status |
|---|---:|---|---:|---:|---|
| `src/LocalGPT/Components/Pages/Install.razor` | 1718 / 1276 | `/install` | 22 / 0 | 153 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor` | 1645 / 8 | `child` | 2 / 0 | 8 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/Database.razor` | 1507 / 358 | `/database` | 3 / 0 | 40 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/TestLab.razor` | 1481 / 8 | `/test-lab` | 5 / 0 | 27 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/ProjectMaintenance.razor` | 1339 / 8 | `/project-maintenance` | 2 / 0 | 38 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/Projects.razor` | 1331 / 8 | `/projects` | 4 / 0 | 62 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/DxFunctionCatalog.razor` | 1118 / 8 | `/dx-functions` | 9 / 0 | 19 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor` | 1094 / 8 | `child` | 7 / 0 | 10 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/Chat.razor` | 1056 / 1825 | `/chat` | 12 / 0 | 34 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/OneWireSecurity.razor` | 956 / 8 | `/onewire-security` | 6 / 0 | 7 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Shared/ProviderRuntimeManagementPanel.razor` | 895 / 8 | `child` | 3 / 0 | 18 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/CouncilTeams.razor` | 814 / 1178 | `/council-teams` | 6 / 0 | 114 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/MinecraftModBuilder.razor` | 804 / 8 | `/minecraft-mod-builder` | 6 / 0 | 14 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Shared/ProviderModelPanel.razor` | 777 / 8 | `child` | 2 / 0 | 6 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Shared/UploadProcessingAdvisor.razor` | 725 / 8 | `child` | 2 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/UserDxFunctionEditor.razor` | 626 / 8 | `child` | 2 / 0 | 19 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/ModelCouncil.razor` | 599 / 966 | `/model-council` | 12 / 0 | 19 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Shared/ProviderModelBenchmarkCouncilPanel.razor` | 579 / 8 | `child` | 2 / 0 | 6 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Layout/ThemeSwitcherContainer.razor` | 571 / 8 | `child` | 4 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/ChatGameConsole.razor` | 504 / 9 | `child` | 5 / 0 | 12 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Shared/CouncilHardwareRoadEditor.razor` | 439 / 8 | `child` | 2 / 0 | 12 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Shared/ChatMicrophone.razor` | 402 / 6 | `child` | 1 / 0 | 3 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Shared/ProviderModelBenchmarkEvidenceHistory.razor` | 380 / 8 | `child` | 2 / 0 | 2 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Shared/LocalPathExplorer.razor` | 369 / 8 | `child` | 1 / 0 | 1 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/RemoteControl.razor` | 339 / 762 | `/remote-control` | 5 / 0 | 33 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/Help.razor` | 325 / 8 | `/help` | 4 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/LocalGptFormLayoutSurface.razor` | 320 / 8 | `child` | 2 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/LayoutStudio.razor` | 309 / 8 | `child` | 4 / 0 | 15 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/DataManagement.razor` | 302 / 798 | `/data-management` | 2 / 0 | 16 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Layout/CouncilSpoolerPanel.razor` | 270 / 8 | `child` | 2 / 0 | 1 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/Index.razor` | 270 / 8 | `/` | 4 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/ProviderModelBenchmarkTaskEvidence.razor` | 248 / 8 | `child` | 5 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Layout/MainLayout.razor` | 240 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/OperationalActivityFeed.razor` | 212 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/ToolchainEnvironmentEditor.razor` | 195 / 8 | `child` | 1 / 0 | 4 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/GameStudio.razor` | 190 / 684 | `/game-studio` | 1 / 0 | 26 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Layout/ThemeSwitcher.razor` | 171 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/DocumentationViewerHost.razor` | 165 / 8 | `child` | 2 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/LocalConsoleFeed.razor` | 159 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/ProviderModelBenchmarkReportAuditSummary.razor` | 159 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/App.razor` | 150 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Layout/Drawer.razor` | 146 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Layout/LanguageSwitcher.razor` | 135 / 9 | `child` | 1 / 0 | 1 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Shared/BoundedNumberEditor.razor` | 133 / 8 | `child` | 1 / 0 | 1 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Layout/SafeErrorBoundary.razor` | 120 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Routes.razor` | 119 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/NetworkCertificateEditor.razor` | 103 / 172 | `child` | 2 / 1 | 14 / 0 | EditForm + DataAnnotations |
| `src/LocalGPT/Components/Shared/HumanCollaborationRequestCard.razor` | 102 / 147 | `child` | 2 / 0 | 4 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Pages/Error.razor` | 93 / 8 | `/Error` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/ConfigurationWorkbenchNav.razor` | 85 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/InvariantDoubleEditor.razor` | 85 / 8 | `child` | 1 / 0 | 1 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Layout/ToastWrapper.razor` | 77 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/ConfigurationWorkbenchPanel.razor` | 71 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/InitialSetupHardwareForm.razor` | 64 / 101 | `child` | 1 / 0 | 8 / 0 | Layout/editors; no explicit EditForm |
| `src/LocalGPT/Components/Layout/NavMenu.razor` | 60 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Layout/ThemeSwitcherItem.razor` | 57 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/WorkbenchHeader.razor` | 55 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Shared/OperationStatusStrip.razor` | 39 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/LocalGPT/Components/Layout/MenuIsland.razor` | 20 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |

### PublisherStudio

| Datei | Zeilen (Razor / Code-behind) | Route | FormLayout / EditForm | DX / native Eingaben | Validierungs-Status |
|---|---:|---|---:|---:|---|
| `src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor` | 6570 / 8 | `child` | 25 / 0 | 0 / 126 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Pages/Editor.razor` | 6249 / 8 | `/` | 3 / 0 | 4 / 16 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/PageSurface.razor` | 6226 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor` | 6130 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Editor/PanelStudio.razor` | 4495 / 8 | `child` | 1 / 0 | 62 / 0 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/StreamingStudio.razor` | 3033 / 8 | `child` | 16 / 0 | 0 / 82 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/DevExtremeComponentEditor.razor` | 2786 / 8 | `child` | 10 / 0 | 0 / 181 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/StoryEditor.razor` | 2609 / 8 | `child` | 3 / 0 | 0 / 6 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/MediaConverterStudio.razor` | 2496 / 8 | `child` | 3 / 0 | 4 / 33 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/DataVisualEditor.razor` | 1812 / 8 | `child` | 2 / 0 | 28 / 0 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/DataManager.razor` | 1703 / 8 | `child` | 7 / 0 | 1 / 25 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/PublicationTimeline.razor` | 1685 / 8 | `child` | 2 / 0 | 0 / 1 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/AnimationPanel.razor` | 1584 / 8 | `child` | 7 / 0 | 0 / 29 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor` | 1480 / 8 | `/organic-plugins` | 12 / 0 | 2 / 17 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/PictureEditor.razor` | 1280 / 7710 | `child` | 14 / 0 | 0 / 68 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/BarcodeEditor.razor` | 1174 / 8 | `child` | 1 / 0 | 0 / 11 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/SpreadsheetEditor.razor` | 1161 / 8 | `child` | 5 / 0 | 1 / 4 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/MediaStudio.razor` | 1077 / 8 | `child` | 13 / 0 | 0 / 63 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/WordArtView.razor` | 939 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Editor/PrintPublication.razor` | 814 / 8 | `child` | 2 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Editor/PageNavigator.razor` | 578 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Editor/PageEffectStudio.razor` | 551 / 8 | `child` | 1 / 0 | 14 / 0 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/OrganicPlugins/OrganicApprovalBar.razor` | 494 / 8 | `child` | 1 / 0 | 0 / 1 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Editor/WordArtPathEditor.razor` | 492 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/OrganicPlugins/OrganicSecurityPanel.razor` | 412 / 8 | `child` | 2 / 0 | 0 / 6 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Pages/Help.razor` | 379 / 8 | `/help` | 3 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Pages/Localization.razor` | 286 / 8 | `/localization` | 3 / 0 | 0 / 3 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/Shared/UserNotificationHost.razor` | 196 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Editor/NewPublicationDialog.razor` | 194 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Editor/PanelLibrary.razor` | 193 / 8 | `child` | 2 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Shared/DocumentationViewerHost.razor` | 191 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Editor/DevExpressColorPicker.razor` | 131 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Editor/SystemFontPicker.razor` | 66 / 8 | `child` | 1 / 0 | 1 / 0 | Layout/editors; no explicit EditForm |
| `src/PublisherStudio.Web/Components/OrganicPlugins/OrganicCouncilRequestForm.razor` | 41 / 71 | `child` | 2 / 1 | 5 / 0 | EditForm + DataAnnotations |
| `src/PublisherStudio.Web/Components/Pages/Error.razor` | 41 / 8 | `/error` | 1 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Layout/MainLayout.razor` | 36 / 8 | `child` | 2 / 0 | 0 / 0 | Layout/display only |
| `src/PublisherStudio.Web/Components/Routes.razor` | 21 / 8 | `child` | 1 / 0 | 0 / 0 | Layout/display only |
