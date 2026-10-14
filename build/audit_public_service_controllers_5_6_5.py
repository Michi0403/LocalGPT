#!/usr/bin/env python3
"""Regression guard: controller discovery, local approval, independent AI policy and DI domain ownership."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / 'src' / 'LocalGPT'
errors = []

def read(rel, *markers):
    path = SRC / rel
    if not path.is_file():
        errors.append(f'{rel}: required file missing')
        return ''
    text = path.read_text(encoding='utf-8-sig')
    for marker in markers:
        if marker not in text:
            errors.append(f'{rel}: missing preserved contract {marker!r}')
    return text

read('Services/PythonTaskOrchestrationService.cs', 'IOptionsMonitor<LocalGPT.BusinessObjects.ConfigurationRoot>', 'await embedded.ExecuteAsync(')
read('BusinessObjects/PublicServiceControllerModels.cs', 'PublicServiceControllerPage', 'PublicServicePropertyDescriptor')
read('Interfaces/IPublicServiceMethodDirectoryService.cs', 'SearchAsync(', 'GroupsAsync(', 'FindAsync(')
read('Services/PublicServiceMethodDirectoryService.cs', 'catalog.GetEntriesAsync(', 'service:', 'MethodCount', 'GetProperties(', 'Methods = ordered.Skip', 'SetExposureAsync(', 'catalog.SavePolicyAsync(')
read('Services/PublicServiceDirectoryDxAiFunctions.cs', 'localgpt.public_service.discover', 'directory.SearchAsync(', 'IsReadOnly: true')
read('Program.ServiceRegistration.cs', 'AddScoped<IPublicServiceMethodDirectoryService, PublicServiceMethodDirectoryService>()',
     'AddScoped<IPublicServiceMethodInvoker, PublicServiceMethodInvoker>()', 'IsAssignableFrom(type.AsType())')
read('Controller/PublicServiceMethodsController.cs', '[Route("api/dxai/public-service")]',
     '[HttpGet]', '[HttpGet("groups")]', '[HttpPost("invoke")]',
     'directory.SearchAsync(', 'directory.GroupsAsync(', 'InvokeLocallyApprovedAsync(', 'directory.SetExposureAsync(',
     'HumanApprovalRequired("localgpt.public_service.invoke"', 'HumanApprovalRequired("localgpt.public_service.exposure"')
service_invoker = read('Services/PublicServiceMethodInvoker.cs',
    'InvokeLocallyApprovedAsync(', 'HasHumanApproval(vocabulary.Get())',
    'Controller approval: localgpt.public_service.invoke',
    'if (!localApproved && !entry.AllowRemoteInvocation && !entry.ExposeToAiChat)',
    'entry.IsEnabled', 'method.Invoke(instance, arguments)')
if 'public async Task<object?> InvokeLocallyApprovedAsync' not in service_invoker:
    errors.append('Local approval must be a separately typed API, not a caller-settable boolean.')
read('Interfaces/IDxAiFunctionCatalogService.cs', 'InvokeLocallyApprovedAsync(')
for controller, routes in {
    'RuntimePluginsController.cs': ['[HttpGet]', '[HttpPost]', '[HttpDelete', 'BuildAndLoadAsync(', 'plugins.SaveAsync(', 'plugins.DeleteAsync(', 'HumanApprovalRequired'],
    'KnowledgeRegexLinksController.cs': ['[HttpGet]', '[HttpPut', '[HttpDelete', 'TestRecognitionAsync(', 'links.SaveAsync(', 'links.DeleteAsync(', 'HumanApprovalRequired'],
    'CouncilTeamsController.cs': ['GetTeamsAsync(', 'GetDefaultTemplatesAsync(', 'FindTeamAsync(', 'teams.SaveAsync(', 'teams.DeleteAsync(', 'ResetToTemplateAsync(', 'HumanApprovalRequired'],
}.items():
    read('Controller/' + controller, *routes)
read('Controller/DxAiFunctionCatalogController.cs', '[HttpPost("discover")]', '[HttpPut("policy")]', 'catalog.SavePolicyAsync(', 'HumanApprovalRequired("dxai.catalog.policy.save"')
read('Controller/LocalAiRuntimeController.cs', 'pythonJobs.ListJobs(', 'pythonJobs.CancelJob(')
read('Controller/ProjectSourceNavigationController.cs', 'navigation.PublishApprovedScriptAsync(', 'HumanApprovalRequired(')
targets = (ROOT/'Directory.Build.targets').read_text(encoding='utf-8-sig')
for x in ['AssertLocalGptPublicServiceControllers', 'Assert-PublicServiceControllers.ps1', 'BeforeTargets="BeforeBuild"', "'$(MSBuildProjectName)' == 'LocalGPT'"]:
    if x not in targets: errors.append(f'Directory.Build.targets missing guard wiring: {x}')
if errors:
    print('Public service controller architecture FAILED:')
    for error in errors: print(' - ' + error)
    sys.exit(1)
print('Public service controller architecture passed: qualified ConfigRoot; user-approved API; catalogued service discovery; separated AI exposure; purpose-owned plugin/team/knowledge routes.')
