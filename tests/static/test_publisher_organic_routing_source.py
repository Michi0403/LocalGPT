from pathlib import Path
root=Path(__file__).parents[2]
markup=(root/'src/LocalGPT/Components/Pages/CouncilTeams.razor').read_text()
source=(root/'src/LocalGPT/Components/Pages/CouncilTeams.PublisherOrganicRouting.razor.cs').read_text()
for token in ('PublisherStudio organic capability routing','TogglePreferredCapability','PublisherOrganicLiveModelOptions', 'BindPublisherOrganicSingleModel','RemoveUnavailablePublisherModels'):
    assert token in markup or token in source, token
for token in ('_publisherAdvertisedCapabilityKeys', '_teams.SelectMany(team => team.PreferredCapabilities)', '_defaultTemplates.SelectMany(template => template.PreferredCapabilities)', 'candidate.IsInstalled && candidate.IsConfigured','CouncilRoleAiSelectionMode.AssignedModels','_confirmed = false','AssignedModelKeys.RemoveAll'):
    assert token in source, token
assert 'IReadOnlyList<LocalGptSelectionOption<string>>' in source
assert 'using LocalGPT.Components.Shared;' in source
assert 'private static readonly IReadOnlyList<string> PublisherOrganicCapabilityKeys' not in source
assert 'WirePeers.GetPeers()' in (root/'src/LocalGPT/Components/Pages/CouncilTeams.razor.cs').read_text()
print('PASS: runtime-advertised and persisted team capability affinity; reviewed model binder')

# PublisherStudio namespace classification is service-owned; both component paths are passive projections.
text_service = (root / 'src/LocalGPT/Services/CouncilTextService.PublisherOrganicCapabilities.cs').read_text()
component_discovery = (root / 'src/LocalGPT/Components/Pages/CouncilTeams.razor.cs').read_text()
assert 'FilterPublisherOrganicCapabilityKeys(' in text_service
assert 'FilterPublisherOrganicCapabilityKeys(' in source
assert 'FilterPublisherOrganicCapabilityKeys(' in component_discovery
assert '.StartsWith("publisher."' in text_service
assert '.StartsWith("publisher."' not in source
assert '.StartsWith("publisher."' not in component_discovery
assert 'CouncilTextService CouncilText' in markup
