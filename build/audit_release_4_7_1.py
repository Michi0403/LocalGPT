#!/usr/bin/env python3
from pathlib import Path
import re, sys
ROOT=Path(__file__).resolve().parents[1]; VERSION='4.7.1'
def text(rel):
 p=ROOT/rel
 if not p.is_file(): raise SystemExit(f'LocalGPT {VERSION} audit failed: missing {rel}')
 return p.read_text(encoding='utf-8',errors='strict')
def req(rel,*markers):
 s=text(rel)
 for m in markers:
  if m not in s: raise SystemExit(f'LocalGPT {VERSION} audit failed: {rel} missing {m!r}')
def forbid(rel,*markers):
 s=text(rel)
 for m in markers:
  if m in s: raise SystemExit(f'LocalGPT {VERSION} audit failed: {rel} contains forbidden {m!r}')
def main():
 parts=[int(x) for x in VERSION.split('.')]
 if len(parts)!=3 or parts[1]>=10 or parts[2]>=10: raise SystemExit('version-slot policy failed')
 for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']: req(rel,f'<Version>{VERSION}</Version>')
 req('RELEASE.md',f'# LocalGPT {VERSION}'); req('VALIDATION.md',f'# LocalGPT {VERSION} source validation')
 req('CHANGELOG-v4.7.1-BUILD-POLICY-REPAIR.md',f'# LocalGPT {VERSION}')
 req('VALIDATION-v4.7.1-source.md',f'# LocalGPT {VERSION} source validation')
 req('src/LocalGPT/Components/App.razor','localgpt-game-console.js?v=4.7.1','localgpt-chat-ui.js?v=4.7.1')
 req('src/LocalGPT/Services/InitialSetupAssistantService.cs','LocalGPT/4.7.1'); req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/4.7.1')
 req('docs/index.md','**Version 4.7.1**'); req('docs/docfx.json','"localgptVersion": "4.7.1"'); req('docs/pdf/toc.yml','LocalGPT-4.7.1.pdf')
 # idle-invisible upload contract
 a='src/LocalGPT/Components/Shared/UploadProcessingAdvisor.razor'
 req(a,'localGptChatUi.registerWorkspaceDropTarget','ExternalWorkspaceDropAcceptedAsync','<DxPopup')
 forbid(a,'<DxFileInput','<div class="upload-processing-advisor"','Select File','UploadMode.OnButtonClick')
 req('src/LocalGPT/wwwroot/js/localgpt-chat-ui.js','registerWorkspaceDropTarget','Workspace analysis ready','2600')
 # repository recognition and review-gated learning
 c='src/LocalGPT/Services/ProjectEvidenceClassifierService.cs'; req(c,'src/LocalGPT/LocalGPT.csproj','src/PublisherStudio.Web/PublisherStudio.Web.csproj','.git/config','GitRepository')
 req('src/LocalGPT/BusinessObjects/ProjectIngestionModels.cs','Repositories')
 p='src/LocalGPT/Services/ProjectIngestionService.cs'; req(p,'IProjectRepositoryLearningService','SynchronizeAsync','StageCandidatesAsync')
 r='src/LocalGPT/Services/ProjectRepositoryLearningService.cs'; req(r,'NeedsUserReview','ParentRevisionId','MarkSuggestedAsync','UserApproved = false')
 req('src/LocalGPT/Services/LearningProjectWorkspaceSyncService.cs','.git/config','ParentRevisionId')
 # Publisher-aware format routing
 f='src/LocalGPT/Services/UploadFileProcessingCapabilityService.cs'; req(f,'x-publisher-format-families','publisher.file.formats','publisher.media.capabilities','localgpt.vision.ocr')
 # bounded OCR architecture
 req('src/LocalGPT/Services/WorkspaceVisionOcrService.cs','ResolveWorkspacePath','ResolveWorkspaceFile','deepseek-ocr','RecognizeAsync','var normalizedHost = host.EndsWith','var baseAddress = new Uri(normalizedHost, UriKind.Absolute)','BaseAddress = baseAddress')
 forbid('src/LocalGPT/Services/WorkspaceVisionOcrService.cs','BaseAddress = new Uri(host.TrimEnd')
 req('src/LocalGPT/Controller/VisionOcrController.cs','IWorkspaceVisionOcrService')
 req('src/LocalGPT/Services/VisionOcrDxAiFunctions.cs','localgpt.vision.ocr.capability','localgpt.vision.ocr.workspace')
 req('src/LocalGPT/Program.ServiceRegistration.cs','IWorkspaceVisionOcrService','IUploadFileProcessingCapabilityService','IProjectRepositoryLearningService')
 # additive controller modes and existing context system
 js='src/LocalGPT/wwwroot/js/localgpt-context-menu.js'; req(js,"localgpt.controllerMode","'cursor'","'control'",'controllerContextAction','native mouse/touchpad stays active','controllerButtonEdge(gamepad, 8)','controllerButtonEdge(gamepad, 4)')
 forbid(js,'document.body.style.pointerEvents = \'none\'')
 req('src/LocalGPT/wwwroot/js/localgpt-game-console.js','localGptControllerInput?.shouldConsumeGameInput?.() === false')
 # no native Razor file selector regression
 bad=[]
 for path in (ROOT/'src/LocalGPT').rglob('*.razor'):
  s=path.read_text(encoding='utf-8',errors='strict')
  if '<InputFile' in s or re.search(r'<input\b[^>]*type=["\']file["\']',s,re.I): bad.append(path.relative_to(ROOT).as_posix())
 if bad: raise SystemExit(f'LocalGPT {VERSION} audit failed: native Razor file inputs: {bad}')
 print('LocalGPT 4.7.1 release audit passed: 4.7.0 features remain wired and the OCR base-address initialization now satisfies the maintained build-policy boundary.')
 return 0
if __name__=='__main__': sys.exit(main())
