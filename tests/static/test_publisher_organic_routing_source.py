from pathlib import Path
root=Path(__file__).parents[2]
markup=(root/'src/LocalGPT/Components/Pages/CouncilTeams.razor').read_text()
source=(root/'src/LocalGPT/Components/Pages/CouncilTeams.PublisherOrganicRouting.razor.cs').read_text()
for token in ('PublisherStudio organic capability routing','TogglePreferredCapability','PublisherOrganicLiveModelOptions', 'BindPublisherOrganicSingleModel','RemoveUnavailablePublisherModels'):
    assert token in markup or token in source, token
for token in ('publisher.spreadsheet.inspect','publisher.openscad.generate','publisher.panel.module.invoke','candidate.IsInstalled && candidate.IsConfigured','CouncilRoleAiSelectionMode.AssignedModels','_confirmed = false','AssignedModelKeys.RemoveAll'):
    assert token in source, token
assert 'IReadOnlyList<LocalGptSelectionOption<string>>' in source
assert 'using LocalGPT.Components.Shared;' in source
print('PASS: saved team capability affinity and explicitly reviewed installed-model binder')
