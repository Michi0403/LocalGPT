"""Release 6.3.6 source checks for architecture gates reported by Visual Studio."""
from pathlib import Path
import re, unittest, hashlib
ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / 'src/LocalGPT'
def read(rel): return (APP / rel).read_text(encoding='utf-8')
class ArchitectureRepair636(unittest.TestCase):
    def test_devexpress_chat_controls(self):
        view=read('Components/Pages/Chat.razor')
        for token in ('<DxCheckBox @bind-Checked="KeyaccountReviewEnabled"', '<DxSpinEdit @bind-Value="KeyaccountContinuationRounds"', '<DxSpinEdit @bind-Value="KeyaccountExpectedArtifacts"'):
            self.assertIn(token,view)
        self.assertNotRegex(view,r'<input[^>]+@bind="Keyaccount')
    def test_component_owner_remains_component_based(self):
        self.assertIn('<div class="initial-setup-physical-host"', read('Components/Shared/InitialSetupAssistantPanel.razor'))
        self.assertNotIn('<section class="initial-setup-physical-host"',read('Components/Shared/InitialSetupAssistantPanel.razor'))
    def test_text_formatting_service_owned(self):
        self.assertIn('public string FormatBlockedGoalRequirements',read('Services/CouncilTextService.cs'))
        for rel in ('Components/Pages/MinecraftModBuilder.razor','Components/Pages/ModelCouncil.razor.cs'):
            self.assertIn('CouncilText.FormatBlockedGoalRequirements(goal.MissingRequirements)',read(rel))
            self.assertNotIn('string.Join(", ", goal.MissingRequirements)',read(rel))
    def test_initializer_reload_diagnostics_and_xml(self):
        init=read('Services/Persistence/DatabaseInitializationService.cs')
        self.assertIn('runtimePolicy.Reload();',init)
        self.assertIn('the built-in seed remains active',init)
        bootstrap=read('Services/AiProviderBootstrapService.cs')
        for arg in ('databaseFactory','databaseInitializer'):
            self.assertIn(f'<param name="{arg}">',bootstrap)
    def test_static_arch_and_js_manifest_coverage(self):
        review=read('Services/GoalExecutionService.KeyaccountManager.cs')
        self.assertIn('private string ReadString(',review)
        self.assertNotIn('private static string ReadString(',review)
        relative='src/LocalGPT/wwwroot/js/localgpt-operational-feed.js'
        data=(ROOT/relative).read_text().replace('\r\n','\n').replace('\r','\n')
        digest=hashlib.sha256(data.encode()).hexdigest()
        manifest=(ROOT/'build/javascript-diagnostics-files.sha256').read_text()
        self.assertIn(f'{digest}  {relative}',manifest)
        self.assertIn('javascript-diagnostics: guarded',data)
        self.assertIn('catch (error)',data)
    def test_release_number(self):
        self.assertIn('<Version>6.3.7</Version>',read('LocalGPT.csproj'))
if __name__=='__main__': unittest.main()
