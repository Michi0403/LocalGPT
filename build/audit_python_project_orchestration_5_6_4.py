#!/usr/bin/env python3
"""Build-breaking source contract for Python.NET, approved Python processes and project-owned source curation."""
import argparse
import ast
from pathlib import Path


def audit(root: Path) -> list[str]:
    problems: list[str] = []

    def read(path: str, *required: str) -> str:
        source_path = root / path
        if not source_path.is_file():
            problems.append(f'Missing required implementation: {path}')
            return ''
        data = source_path.read_text(encoding='utf-8-sig')
        for fragment in required:
            if fragment not in data:
                problems.append(f'{path}: missing implementation contract {fragment!r}')
        return data

    coord = read('src/LocalGPT/Services/PythonNetRuntimeCoordinator.cs', 'Python.Runtime.Py.GIL', 'Channel', 'SingleReader = true')
    python_jobs = read('src/LocalGPT/Services/PythonTaskOrchestrationService.cs',
        'IPythonNetRuntimeCoordinator', 'IPythonJobOrchestrationService',
        'await embedded.ExecuteAsync(', 'await ReserveProcessCapacityAsync(',
        'ReadProcessCapacityAsync(', 'GetProfileAsync("localai.runtime-policy"',
        'processCapacityChanged.WaitAsync(', 'device.WaitAsync(',
        'RedirectStandardOutput = true', 'RedirectStandardError = true',
        'ReadBoundedAsync(process.StandardOutput', 'ReadBoundedAsync(process.StandardError',
        'process.Kill(entireProcessTree: true)', 'timeout.CancelAfter(', 'CancellationRequested')
    if python_jobs and 'Task.Run(() => embedded' in python_jobs:
        problems.append('Embedded Python.NET must not be run concurrently through Task.Run')
    if python_jobs and not (python_jobs.index('ReadBoundedAsync(process.StandardOutput') < python_jobs.index('process.StandardInput.WriteAsync(')):
        problems.append('Drain Python subprocess stdout/stderr before writing stdin to avoid pipe deadlock')
    read('src/LocalGPT/Program.ServiceRegistration.cs',
        'AddSingleton<PythonNetRuntimeCoordinator>()',
        'AddSingleton<PythonTaskOrchestrationService>()',
        'AddSingleton<IPythonNetRuntimeCoordinator>', 'AddSingleton<IPythonJobOrchestrationService>',
        'AddScoped<IProjectSourceNavigationService, ProjectSourceNavigationService>()')
    read('src/LocalGPT/Runtime/Plugins/RuntimePluginExecutionHost.cs',
        'ValidatePythonRuntimeAsync(', 'InvokePythonAsync(', 'pythonJobs.RunApprovedPythonPluginAsync(')
    read('src/LocalGPT/Services/RuntimePluginService.cs',
        'RuntimePluginKind.Python', 'ResolveCompilerAsync(', 'executionHost.ValidatePythonRuntimeAsync(')
    read('src/LocalGPT/BusinessObjects/RuntimePluginModels.cs', 'Python = 3')
    python_bridge = read('src/LocalGPT/Runtime/Python/localgpt_runtime_bridge.py',
        'memory_strategy', 'model_cpu_offload', 'sequential_cpu_offload', 'device_map="balanced"',
        'torch.cuda.device_count() < 2', 'pipe.to(device)')
    if python_bridge:
        try:
            ast.parse(python_bridge)
        except SyntaxError as error:
            problems.append(f'Python diffusion bridge syntax: {error}')
    read('src/LocalGPT/Services/ProjectSourceNavigationService.cs',
        'GetTrackedFilesAsync(', 'GetWorkspaceRootsAsync(', 'GetCompilerInstallationsAsync(',
        'ListApprovedPatternsAsync(', 'ListAsync(', 'compilation.Compile(',
        'safeText.ReadAsync(', 'LearningReviewedHash', 'LearningReadComplete',
        'SHA256.HashData(bytes)', 'Source changed after curation',
        'GetAttributes(', 'SaveArtifactAsync(', 'plugins.SaveAsync(',
        'preferredCompilerIds.Contains(item.Id)', 'TargetCompatibilityVerified = false')
    read('src/LocalGPT/Interfaces/IProjectSourceNavigationService.cs', 'NavigateAsync(', 'PublishApprovedScriptAsync(')
    read('src/LocalGPT/Controller/ProjectSourceNavigationController.cs',
        '[HttpPost("navigate")]', '[HttpPost("publish-plugin")]',
        '[HumanApprovalRequired(')
    read('src/LocalGPT/Services/ProjectSourceNavigationDxAiFunctions.cs',
        'project.source.navigate', 'project.source.publish-plugin',
        'RequiresHumanConfirmation: true', 'navigation.PublishApprovedScriptAsync(')
    read('src/LocalGPT/Services/PythonOrchestrationDxAiFunctions.cs',
        'localai.python.jobs.list', 'localai.python.jobs.cancel',
        'runtime.plugins.save', 'runtime.plugins.build', 'UseProjectSource',
        'RequiresHumanConfirmation: true')
    html = read('src/LocalGPT/Components/Pages/Install.razor',
        'DxHtmlEditor', 'RuntimePluginSourceMarkupChanged', 'PublishRuntimePluginProjectSourceAsync',
        'PythonTaskRows', 'DxGrid', 'RefreshPythonTaskRows')
    if html and 'DxMemo @bind-Text="RuntimePluginDraft.SourceCode"' in html:
        problems.append('Runtime plugin editor was replaced with a memo: preserve the DevExpress HTML Editor')
    read('src/LocalGPT/Components/Pages/Install.RuntimePlugins.razor.cs',
        'FromEditorMarkup(RuntimePluginSourceMarkup)', 'ToEditorMarkup(')
    read('src/LocalGPT/Components/Pages/Install.ProjectSource.razor.cs',
        'ProjectSourceNavigation.NavigateAsync(', 'ProjectSourceNavigation.PublishApprovedScriptAsync(')
    read('src/LocalGPT/Components/Pages/Install.PythonTasks.razor.cs',
        'PythonJobs.ListJobs(', 'PythonJobs.CancelJob(')
    read('src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs',
        'CouncilEvidencePopupVisible', 'ToggleCompletedCouncilActivityEvidence(')
    chat = read('src/LocalGPT/Components/Pages/Chat.razor',
        'ToggleCompletedCouncilActivityEvidence(activity)',
        '<DxPopup @bind-Visible="CouncilEvidencePopupVisible"',
        'InspectedCouncilActivity', 'ChatContentRenderer.Render(inspectedActivity.FinalContent)')
    if chat and chat.index('</DxAIChat>') > chat.index('<DxPopup @bind-Visible="CouncilEvidencePopupVisible"'):
        problems.append('The Council evidence popup must be outside the DxAIChat template cache')
    targets = read('Directory.Build.targets', 'AssertLocalGptPythonProjectOrchestration',
        'Assert-PythonProjectOrchestration.ps1', 'BeforeTargets="BeforeBuild"')
    if targets and "'$(MSBuildProjectName)' == 'LocalGPT'" not in targets:
        problems.append('Python/project build guard must target LocalGPT builds')
    return problems


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parent.parent)
    args = parser.parse_args()
    errors = audit(args.root.resolve())
    if errors:
        print('LocalGPT Python/project orchestration audit failed:')
        for error in errors:
            print(' - ' + error)
        raise SystemExit(1)
    print('LocalGPT Python/project orchestration passed: one GIL worker, bounded subprocesses, tracked project files, curator evidence, DevExpress editor, Council popup, approvals.')
