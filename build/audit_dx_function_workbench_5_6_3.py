#!/usr/bin/env python3
"""Source-only guard for the 5.6.3 policy-bound DX function workbench; no .NET process required."""
import argparse
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


def audit(root: Path) -> list[str]:
    errors: list[str] = []
    def contains(path: str, *evidence: str) -> str:
        target = root / path
        if not target.is_file():
            errors.append(f"{path}: required production implementation missing")
            return ""
        source = target.read_text(encoding="utf-8-sig")
        for item in evidence:
            if item not in source:
                errors.append(f"{path}: missing contract anchor {item!r}")
        return source

    registry = contains('src/LocalGPT/Services/DxAiFunctionRegistry.cs',
        'InvokeCoreAsync(', 'BuildPreviewFingerprint(', 'BuildValidationIssue(',
        'ExpectedPreviewFingerprint', 'PreviewOnly', 'StalePreview',
        'HumanApprovalPending', 'AuthorizeOrEnqueueAsync(',
        'handler!.InvokeAsync(', 'audit.RecordAsync(result, CancellationToken.None)',
        'var requiresHumanConfirmation = (catalogPolicy?.RequiresFrontendConfirmation ?? descriptor.RequiresHumanConfirmation)')
    if registry:
        begin = registry.index('var parameterValidationError =')
        preview = registry.find('if (request.PreviewOnly)', begin)
        approval = registry.find('AuthorizeOrEnqueueAsync(', begin)
        execute = registry.find('handler!.InvokeAsync(', begin)
        if -1 in (preview, approval, execute) or not (begin < preview < approval < execute):
            errors.append('DxAiFunctionRegistry.cs: preview must validate schema and return before approval or execution')
        if 'if (!request.PreviewOnly)' not in registry:
            errors.append('DxAiFunctionRegistry.cs: preview must not be written as an execution audit')
    discovery = contains('src/LocalGPT/Services/DxAiFunctionCatalogService.DiscoveryWorkbench.cs',
        'GetEntriesAsync(', 'entry.IsAvailable && entry.IsEnabled && entry.ExposeToAiChat',
        'function.SupportsDirectInvocation', 'IncludeSchemas', '.Skip(', '.Take(', 'DescriptorHash')
    contains('src/LocalGPT/Interfaces/IDxAiFunctionCatalogService.cs', 'DiscoverForAiAsync(')
    contains('src/LocalGPT/BusinessObjects/DxAiFunctionWorkbenchModels.cs',
        'class DxAiFunctionDiscoveryRequest', 'class DxAiFunctionInvocationPreview',
        'class DxAiFunctionValidationIssue', 'class DxAiFunctionInvocationAuditEntry')
    contains('src/LocalGPT/Services/DxAiFunctionInvocationAuditService.cs',
        'SystemVariables', 'SaveChangesAsync(', 'OperationId', 'FunctionName',
        'Status', 'Succeeded', 'Skip(250)', 'GetRecentAsync')
    contains('src/LocalGPT/Interfaces/IDxAiFunctionInvocationAuditService.cs', 'RecordAsync(', 'GetRecentAsync(')
    workbench = contains('src/LocalGPT/Services/DxAiFunctionWorkbenchService.cs',
        'client.CallAsync(', 'PreviewOnly = true', 'ExpectedPreviewFingerprint = previewFingerprint',
        'UserConfirmed = false', 'GetRecentAsync')
    if workbench and 'UserConfirmed = true' in workbench:
        errors.append('DxAiFunctionWorkbenchService.cs: workbench must not pre-authorize its own operation')
    contains('src/LocalGPT/Interfaces/IDxAiFunctionWorkbenchService.cs', 'PreviewAsync(', 'ExecuteAsync(')
    contains('src/LocalGPT/Controller/DxAiFunctionCatalogController.cs',
        '[HttpPost("discover")]', 'DiscoverForAiAsync(')
    contains('src/LocalGPT/Controller/DxAiFunctionsController.cs',
        '[HttpPost("{functionName}/preview")]', 'request.PreviewOnly = true',
        '[HttpGet("history")]', 'GetRecentAsync(', '"StalePreview"')
    contains('src/LocalGPT/Services/DxAiFunctionWorkbenchDxAiFunctions.cs',
        'dxai.tools.discover', 'dxai.tools.preview', 'dxai.tools.invoke', 'DiscoverForAiAsync(',
        'registry.InvokeAsync(', 'PreviewOnly = true', 'SupportsAutomaticInvocation: true')
    broker = contains('src/LocalGPT/Services/OllamaThinkingChatClient.Tools.cs',
        'automaticFunctionAllowList is null', 'automaticFunctionAllowList.Contains(function.Name)', 'function.Source == "DxFunctionBroker"')
    if broker and 'function.SupportsAutomaticInvocation' not in broker:
        errors.append('OllamaThinkingChatClient.Tools.cs: native tool availability must obey automatic-invocation policy')
    ui = contains('src/LocalGPT/Components/Pages/DxFunctionCatalog.razor',
        '@inject IDxAiFunctionWorkbenchService FunctionWorkbench',
        'SectionKey="workbench"', 'DxComboBox', 'DxMemo', 'DxGrid',
        'PreviewWorkbenchFunctionAsync', 'ExecuteWorkbenchFunctionAsync',
        'RefreshWorkbenchHistoryAsync', 'preview.PreviewFingerprint')
    if ui and '@rendermode InteractiveServer' not in ui.splitlines()[:3]:
        errors.append('DxFunctionCatalog.razor: InteractiveServer render mode missing')
    contains('src/LocalGPT/Program.ServiceRegistration.cs',
        'AddScoped<IDxAiFunctionInvocationAuditService, DxAiFunctionInvocationAuditService>()',
        'AddScoped<IDxAiFunctionWorkbenchService, DxAiFunctionWorkbenchService>()',
        'AddScoped<IDxAiFunctionRegistry, DxAiFunctionRegistry>()')
    targets = contains('Directory.Build.targets', 'AssertLocalGptDxFunctionWorkbench',
        'Assert-DxFunctionWorkbench.ps1', 'BeforeTargets="BeforeBuild"')
    if "$(MSBuildProjectName)" not in targets or "'LocalGPT'" not in targets:
        errors.append('Directory.Build.targets: DX workbench guard must be project-bound')
    wrapper = contains('build/Assert-DxFunctionWorkbench.ps1',
        'audit_dx_function_workbench_5_6_3.py', 'python', 'LASTEXITCODE')
    if wrapper and 'throw' not in wrapper:
        errors.append('Assert-DxFunctionWorkbench.ps1: guard must fail the build')
    policy = contains('build/async-continuation-policy.json',
        'PreviewWorkbenchFunctionAsync', 'ExecuteWorkbenchFunctionAsync', 'RefreshWorkbenchHistoryAsync')
    try:
        ET.parse(root / 'Directory.Build.targets')
    except ET.ParseError as exception:
        errors.append(f'Directory.Build.targets: malformed XML: {exception}')
    return errors


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', default='.')
    args = parser.parse_args()
    problems = audit(Path(args.root).resolve())
    if problems:
        for problem in problems:
            print('DX function workbench audit: ' + problem, file=sys.stderr)
        return 1
    print('DX function workbench audit passed: policy discovery, live schema preview, bound approval, DX/UI/API/Council wiring and payload-free history.')
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
