"""Source-only regression checks for the retained LocalGPT 5.5.1 data-management/Council repairs.

This intentionally does not need .NET, services, a database, or model endpoints.
The repository's regular build-breaking architecture guards remain authoritative.
"""

from pathlib import Path
import json
import re


ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "src" / "LocalGPT"


def read(relative: str) -> str:
    return (SRC / relative).read_text(encoding="utf-8")


def require(ok: bool, explanation: str) -> None:
    if not ok:
        raise AssertionError(explanation)


version = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", read("LocalGPT.csproj"))
require(version is not None and tuple(map(int, version.groups())) == (5, 6, 1), "LocalGPT release version must be 5.6.1")
require(all(int(slot) < 10 for slot in version.groups()[1:]), "Minor/patch slots must remain single digits")

page = read("Components/Pages/DataManagement.razor")
code = read("Components/Pages/DataManagement.razor.cs")
catalog = read("Services/DataManagementCatalogService.cs")
relation_service = read("Services/SqliteTableEditorService.cs")
approval_service = read("Services/HumanCollaborationService.cs")
approval_contract = read("Interfaces/IHumanCollaborationService.cs")
council = read("Services/MultiModelCouncilService.LiveInputAndHealth.cs")
chat = read("Components/Pages/Chat.razor")
chat_code = read("Components/Pages/Chat.LiveCouncil.razor.cs")

for required in ('@page "/data-management"', '@rendermode @(new InteractiveServerRenderMode(prerender: false))',
                 '<DxFormLayout', '<ConfigurationWorkbenchNav', '<DxGrid', '<DxComboBox',
                 '<DxMemo', '<DxSpinEdit', '<DxDateEdit', '<DxCheckBox',
                 'Parent', 'Children', 'DeleteConfirmed', 'ReleaseHealthExclusionAsync'):
    require(required in page, f"Missing maintained DevExpress workbench feature: {required}")
require("/data-management" in read("Components/Layout/NavMenu.razor"), "Data Management navigation is missing")
require("GetTablesAsync" in catalog and "PRAGMA foreign_key_list" in catalog, "Live SQLite relation catalog is required")
require("SqliteOpenMode.ReadOnly" in catalog, "Catalog should inspect metadata without write access")
require("GetRelatedRowsAsync" in relation_service and "$relationValue" in relation_service,
        "Relational slices must use validated, parameterized SQLite reads")
require("TableEditor.InsertRowAsync" in code and "TableEditor.UpdateRowAsync" in code and "TableEditor.DeleteRowAsync" in code,
        "Keep the maintained SQLite editor instead of an independent write path")
require("EditorPreferences.GetOverridesAsync" in code and "EditorPreferences.InferEditorKind" in code,
        "Persisted DevExpress editor preferences must be respected")
require("HumanAmbientContext.PushHumanInteraction" in code,
        "Releasing a human-approved model decision must carry a trusted local UI interaction")
for method in ("GetActiveModelHealthExclusionsAsync", "ReleaseModelHealthExclusionAsync"):
    require(method in approval_service and method in approval_contract, f"Business-service/contract mismatch: {method}")
require('EnsureTrustedHumanInteraction("release an approved Council model-health exclusion")' in approval_service,
        "The model-health release must enforce the existing trusted-human gate")
require('request.ConsumeApproval = oneRunModelHealth ||' in approval_service,
        "New model-health decisions must be consumed after one run")
require('existing.ConsumeApproval || normalizedOperationKey.StartsWith("council.model.exclude-next-run.' in approval_service,
        "Previously approved persistent health decisions must consume on reuse")
require("GetActiveModelHealthExclusionsAsync(cancellationToken)" in council,
        "Council eligibility lookup must not be restricted by the generic inbox page size")
require('class="localgpt-live-participant-heading"' in chat and
        'ToggleCompletedCouncilActivityEvidence(activity.ActivityKey)' in chat and
        'else if (revealedCompletedCouncilActivityEvidence.Contains(activity.ActivityKey))' in chat,
        "Completed graphical Council lanes must have an always-reachable DevExpress evidence action")
require("private void ToggleCompletedCouncilActivityEvidence" in chat_code,
        "The UI must explicitly toggle lazy evidence rendering")
require('<details class="localgpt-live-participant-card"' not in chat,
        "Do not hide the graphical evidence action in a closed native disclosure")

catalog_files = list((SRC / "Localization").glob("*.json"))
require(len(catalog_files) == 6, "Retain all six maintained UI cultures")
catalogs = {file.stem: json.loads(file.read_text(encoding="utf-8")) for file in catalog_files}
keys = set(catalogs["en-US"])
for culture, translations in catalogs.items():
    require(set(translations) == keys, f"Catalog key mismatch: {culture}")
    require(translations.get("Nav.DataManagement") and translations.get("DataManagement.Title"),
            f"Data Management route/title translation missing: {culture}")
for match in re.finditer(r'T\("(DataManagement\.[^"]+)"', page + code):
    require(match.group(1) in keys, f"Missing Data Management localization key: {match.group(1)}")

learning = read("Components/Pages/ProjectMaintenance.razor")
require('Learning checklist per file' in learning and '<DxGrid Data="@trackedFiles"' in learning,
        "Preserve the per-file learning checklist and its DevExpress editor")
policy = json.loads((ROOT / "build/async-continuation-policy.json").read_text(encoding="utf-8"))
helpers = policy["rendererAffineHelperMethods"].get("Components/Pages/DataManagement.razor.cs", [])
require("ReleaseHealthExclusionAsync" in helpers and "LoadSelectedTableCoreAsync" in helpers,
        "Render-affine Data Management handlers must be explicitly reviewed, not exempted globally")

print("LocalGPT 5.6.1 retained Data Management/Council source regression contract passed: domain relations, DevExpress UI, safe one-run approvals, graphical evidence, per-file checklist and all locale keys.")
