#!/usr/bin/env python3
"""Audit LocalGPT documentation accessibility and two-sided 1-Wire/OCR discoverability contracts."""
from pathlib import Path
import subprocess, sys, tempfile
ROOT=Path(__file__).resolve().parents[1]
failures=[]
def text(rel):
 p=ROOT/rel
 if not p.is_file(): failures.append(f"missing file: {rel}"); return ""
 return p.read_text(encoding="utf-8-sig")
def require(rel,*needles):
 data=text(rel)
 for n in needles:
  if n not in data: failures.append(f"{rel}: missing {n!r}")
def forbid(rel,*needles):
 data=text(rel)
 for n in needles:
  if n in data: failures.append(f"{rel}: forbidden {n!r}")

require('src/LocalGPT/BusinessObjects/DocumentationModels.cs','LocalGptDocumentationViewerRequest','LocalGptDocumentationProfile')
require('src/LocalGPT/BusinessObjects/OneWireProtocolModels.cs','OneWireProtocolProfile','OneWirePublicSettings','LocalVisionOcrRequest')
require('src/LocalGPT/Controller/DocumentationController.cs','[HttpGet("profile")]','ActionResult<LocalGptDocumentationProfile>','[HttpGet("/help-docs/{**relativePath}")]')
require('src/LocalGPT/Controller/OneWireHttpController.cs','[HttpGet("profile")]','ActionResult<OneWireProtocolProfile>')
require('src/LocalGPT/Components/Shared/DocumentationViewerHost.razor','<dialog','role="dialog"','aria-modal="true"','aria-labelledby','aria-label="Close documentation viewer"','target="_blank"','CloseFromBrowser')
require('src/LocalGPT/wwwroot/js/documentationViewer.js','showModal()','cancel','previous.focus()')
require('src/LocalGPT/Components/Pages/Help.razor','IDocumentationViewerService','Viewer.Open')
require('src/LocalGPT/Services/OneWire/OneWireCapabilityCatalog.cs','localgpt.documentation.profile','localgpt.vision.ocr','/api/documentation/profile')
require('src/LocalGPT/Services/OneWire/OneWireExecutionServices.cs','localgpt.documentation.profile','IDocumentationCatalogService','LocalVisionOcrRequest')
require('src/LocalGPT/Components/Pages/OneWireSecurity.razor','Active 1-Wire protocol surface','Method and route','Configuration','Runtime','MaximumMessageBytes','BroadcastIntervalSeconds','PeerExpirySeconds','localgpt.vision.ocr','publisher.documentation.profile','publisherstudio.picture.ocr','/api/onewire/http-json/profile')
require('build/Build-Documentation.ps1','@page { size: A4 portrait','Complete API page inventory','html-browser-compact-handbook','https://michi0403.github.io/LocalGPT/')
require('build/Update-GitHubPagesSnapshot.ps1','localgpt-kawaii-docs.zip','--expected-version')
forbid('build/Update-GitHubPagesSnapshot.ps1','BranchPagesRoot','docs mirror','branch-publishing mirror')
forbid('build/Build-Documentation.ps1','LocalGPTWebViewWrapper')
validator=ROOT/'.github/scripts/prepare-pages-artifact.py'
archive=ROOT/'.github/pages/localgpt-kawaii-docs.zip'
with tempfile.TemporaryDirectory(prefix='localgpt-contract-audit-') as tmp:
 result=subprocess.run([sys.executable,str(validator),'--archive',str(archive),'--output',tmp,'--expected-version','2.3.5'],capture_output=True,text=True)
 if result.returncode: failures.append(result.stderr.strip() or result.stdout.strip())
if failures:
 print('LocalGPT documentation/1-Wire contract audit failed:')
 for failure in failures: print(' -',failure)
 raise SystemExit(1)
print('LocalGPT documentation/1-Wire contract audit passed: modal access, tagged Pages/PDF, active peer methods/settings, documentation profile and DeepSeek-compatible OCR are discoverable and executable.')
