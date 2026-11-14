# LocalGPT 7.2.0 - Create wire (W1): create documents in PublisherStudio, list sessions, no size floor

Pairs with PublisherStudio 4.8.1. Older PublisherStudio versions simply do not advertise the new capabilities.

**Observed (live test):** a team asked to "create an Excel spreadsheet and a Word text document in the connected PublisherStudio" could not do it. Only `publisher.spreadsheet.inspect` (needs an open workbook and a session id) and `publisher.text.proposal.request` existed, so the model asked the human for the PublisherStudio session id.

**Changed**
- New dedicated functions (same peer selection and deferred-approval flags as the existing publisher functions):
  - `publisher.document.create.request` - creates a new spreadsheet (CSV content) or text document; fresh LocalGPT approval and fresh PublisherStudio approval; result via `organic.plugin.work.read`.
  - `publisher.sessions.list` - read-only, automatic-safe; lists open PublisherStudio sessions and their spreadsheet/text objects so the model never asks for ids.
- `CouncilCapabilityNativeFunctionsJson` seed gains `publisher.document.create` and `publisher.sessions.list`. Readers (`CouncilAutomaticFunctionPolicyService`, `GoalExecutionService.Delivery`) now use the new `GetJsonListMapWithShippedDefaults`: stored entries win, missing shipped keys are merged in memory; the stored row is not rewritten.
- `GoalApprovalGatedCapabilityPrefixes` already contains `publisher.`, so a recorded create request counts as goal delivery evidence (no change needed).
- `OneWireCapabilityCatalog`: both keys added to the spreadsheet skill. Spreadsheet Team requests both capabilities and gets one instruction sentence; the General Council gets the same hint as an architecture contract. There is no separate writing/story team seed.
- Team seed version 54 -> 55: existing unmodified system-seed teams are refreshed on startup; user-modified teams keep their content (reset to template to receive the new text).
- Goal model fallback: `GoalFallbackMinimumModelBillionParameters` seed default 7 -> 0, and 0 means "no size floor".
- Bundled `PublisherStudio.json` add-on manifest lists the two capabilities. Version 7.1.9 -> 7.2.0.

**Runtime values:** default of `GoalFallbackMinimumModelBillionParameters` is now 0 for new databases.

**Files:** Services/OneWire/ListPublisherSessionsFunction.cs, CreatePublisherDocumentFunction.cs (new); OneWireCapabilityCatalog.cs; ILocalGptRuntimePolicyDataService.cs; LocalGptRuntimePolicyDataService.cs; CouncilAutomaticFunctionPolicyService.cs; GoalExecutionService.Delivery.cs; GoalExecutionService.FallbackModels.cs; LocalGptRuntimePolicySeedDataService.GoalDeliveryAndSteward.cs; OrganicCouncilBlueprintSeedDataService.cs; CouncilTeamConfigurationService.cs; Configuration/OrganicAddons/PublisherStudio.json; LocalGPT.csproj; maintenance/WORKLOADS.md.

**Not compiled here:** no .NET SDK or PowerShell; no build, guard or runtime check was run. The "Dismiss" button for AI help requests (W1 item G) is postponed and not part of this version.

**Check after update**
- Databases created by 7.1.9 still hold `GoalFallbackMinimumModelBillionParameters = 7` (existing rows are never overwritten). Set it to 0 under Runtime values if small models should be allowed as fallback.
- Connect PublisherStudio 4.8.1, ask a team to create a spreadsheet and a text document: expect a LocalGPT approval, then a PublisherStudio approval, then the new objects; the model must not ask for a session id.
