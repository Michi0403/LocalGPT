"""Source-only acceptance contract for the compact, service-owned Data Management workbench, retained in 5.6.3."""

from pathlib import Path
import json
import re


ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "src" / "LocalGPT"


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8-sig")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"Data Management 5.6.3 regression: {message}")


project = read("src/LocalGPT/LocalGPT.csproj")
require("<Version>5.6.3</Version>" in project, "version must advance to 5.6.3")

page = read("src/LocalGPT/Components/Pages/DataManagement.razor")
code = read("src/LocalGPT/Components/Pages/DataManagement.razor.cs")
style = read("src/LocalGPT/Components/Pages/DataManagement.razor.css")
service = read("src/LocalGPT/Services/SqliteTableEditorService.cs")
contract = read("src/LocalGPT/Interfaces/ISqliteTableEditorService.cs")
policy = json.loads(read("build/async-continuation-policy.json"))

for component in (
    "DxTreeView", "DxTreeViewNode", "DxSplitter", "DxSplitterPane", "DxListBox",
    "DxFormLayout", "DxFormLayoutItem", "DxAccordion", "DxMemo", "DxSpinEdit",
    "DxDateEdit", "DxComboBox", "DxGrid",
):
    require(f"<{component}" in page, f"expected DevExpress {component} navigation/editor control")
require('FieldName="@column.Name"' not in page, "dynamic every-column grid would recreate the unusable wide-row layout")
require("FieldName=\"@nameof(SqliteRowSnapshot.DisplayName)\"" in page, "retain only a narrow optional record grid")
require('Height="680px"' in page and "data-management-editor-scroll" in page, "record editor must remain viewport-bounded")
require("data-management-record-editor{display:flex" in style, "record editor scroll/toolbar ownership missing")
require('Click="SaveRowAsync"' in page and 'Click="DeleteRowAsync"' in page and 'Click="NewRow"' in page,
        "create/update/delete commands must remain visible beside the editor")
require('Click="NextPageAsync"' in page and 'Click="PreviousPageAsync"' in page,
        "all stored rows need accessible paging beyond the former latest-only page")
require("ActivePageSize" in code and "RowOffset + ActivePageSize" in code,
        "paging must retain its active page size when the uncommitted row-limit input changes")
require("GetTablePageAsync" in code and "GetRelatedRowsPageAsync" in code, "use the existing database-service boundary for pages")
require("GetTablePageAsync" in contract and "GetRelatedRowsPageAsync" in contract,
        "service contracts must include the paged read capability")
require("OFFSET $skip" in service and service.count('command.Parameters.AddWithValue("$skip", skip)') >= 2,
        "both ordinary and related-page SQL must bind the offset parameter")
require("SqliteTableEditorMaximumRows" in service, "existing bounded-query runtime policy must remain active")
for method in ("TableEditor.InsertRowAsync", "TableEditor.UpdateRowAsync", "TableEditor.DeleteRowAsync",
               "EditorPreferences.GetOverridesAsync", "EditorPreferences.SaveOverrideAsync",
               "HumanCollaboration.ReleaseModelHealthExclusionAsync"):
    require(method in code, f"existing business/editor service use missing: {method}")
require("RequiresProtectedFieldApproval" in code and "ProtectedFieldsConfirmed" in page,
        "persisted field confirmation policy must be enforced by the editor")
require("PreferenceSaveConfirmed" in page and "userConfirmed: true" in code,
        "persistent editor preference changes must have a fresh explicit UI confirmation")
for method in ("OpenTableFromTreeAsync", "NextPageAsync", "PreviousPageAsync", "SavePreferenceAsync"):
    require(method in policy["rendererAffineHelperMethods"]["Components/Pages/DataManagement.razor.cs"],
            f"new renderer-affine handler lacks a reviewed async policy entry: {method}")

keys = {file.stem: json.loads(file.read_text(encoding="utf-8")) for file in (SOURCE / "Localization").glob("*.json")}
require(len(keys) == 6, "all six localization catalogs must remain available")
english = set(keys["en-US"])
for culture, translations in keys.items():
    require(set(translations) == english, f"{culture} localization keys drifted")
for match in re.finditer(r'T\("(DataManagement\.[^"]+)"', page + code):
    require(match.group(1) in english, f"new UI string is missing from localization: {match.group(1)}")

print("LocalGPT 5.6.3 Data Management layout, complete paged reads, CRUD services, editor preferences, approvals and localization audit passed.")
