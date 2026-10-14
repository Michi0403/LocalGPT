#!/usr/bin/env python3
"""Source-only contract guard for the 5.4.2 upload-workspace and Council durability repair.

No .NET runtime or platform-specific filesystem behavior is claimed by this guard.
"""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parent.parent
source = root / 'src' / 'LocalGPT'
read = lambda name: (source / name).read_text(encoding='utf-8')
models = read('BusinessObjects/ChatUploadWorkspaceModels.cs')
interface = read('Interfaces/IChatUploadWorkspaceService.cs')
service = read('Services/ChatUploadWorkspaceService.cs')
functions = read('Services/ChatUploadWorkspaceDxAiFunctions.cs')
resolver = read('Services/CouncilRuntimeService.PromptAndTextRuntime.cs')
logging = read('Services/LoggingConfigurationService.cs')
run = read('Services/MultiModelCouncilService.RunOrchestration.cs')
checkpoints = [read(name) for name in (
    'Services/MultiModelCouncilService.ChangeReviewAndHardware.cs',
    'Services/MultiModelCouncilService.WorkspaceAndResearchGates.cs',
    'Services/MultiModelCouncilService.ConfiguredWorkflowExecution.cs')]

checks = {
    'page DTO and discoverable exact project paths': ('record ChatUploadWorkspaceFilePage(' in models and 'IReadOnlyList<string> ProjectFiles' in models),
    'typed paging interface implemented in service': ('ChatUploadWorkspaceFilePage QueryFiles(' in interface and 'ChatUploadWorkspaceFilePage QueryFiles(' in service),
    'project files not limited by paging': (service.index('var projectFiles = paths.Where(') < service.index('var pageFiles = matches.Skip(')),
    'archive roots independent of filtering': (service.index('var archiveRoots = originals.Where(') < service.index('var matches = paths.Where(')),
    'real exact paths from workspace root': 'Path.GetRelativePath(root, path)' in service,
    'both Windows/POSIX path fragment separators': "pathContains.Trim().Replace('\\\\', '/')" in service,
    'server-side substring and extension filtering': ('pathContains' in service and 'Path.GetExtension(file.RelativePath)' in service),
    'pagination continuation': ('hasMore ? pageOffset + pageFiles.Count : null' in service),
    'original file metadata remains': 'OriginalUploadBytes = page.OriginalUploads.Sum(file => file.Length)' in functions,
    'generated provenance remains': 'GeneratedWorkspaceArtifacts' in functions and 'not additional user uploads' in functions,
    'root constrained absolute file paths': 'Path.IsPathRooted(normalized) ? normalized : Path.Combine(root, normalized)' in resolver and 'IsInsideRoot(root, file, logger)' in resolver,
    'cross-platform slash handling': "Replace('\\\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)" in resolver,
    'misspelled paths provide grounded candidates': ('CandidatePaths = candidates' in functions and 'QueryFiles(workspaceName, 25, 0, fileName)' in functions),
    'low first-read count normalized': ('Math.Max(64_000, requestedCharacters)' in functions),
    'file logger provider-specific filter': 'loggingBuilder.AddFilter<FileLoggerProvider>((_, _) => true)' in logging,
    'cancelled Council has durable log': ('catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)' in run and 'result.LogPath = await WriteLogAsync(result, CancellationToken.None, logger)' in run),
    'Council steps checkpoint before completion': all('result.LogPath = await WriteLogAsync(result, CancellationToken.None, logger)' in source_text for source_text in checkpoints),
}
# Precisely target both DXFunction descriptors, not the unrelated context-reader schema.
for name, min_value in [('chat.upload_workspace_files', None), ('chat.upload_workspace_file', 1)]:
    start = functions.find(f'"{name}",')
    if start < 0:
        checks[f'{name} registered'] = False
        continue
    marker = 'ParameterSchemaJson: """'
    start = functions.find(marker, start)
    end = functions.find('"""', start + len(marker))
    schema = json.loads(functions[start + len(marker):end])
    checks[f'{name} JSON schema valid'] = bool(schema.get('properties'))
    if min_value is not None:
        checks['file reader accepts small model first-read request'] = schema['properties']['maxCharacters']['minimum'] == min_value
    else:
        checks['list schema exposes search and paging'] = {'take', 'offset', 'pathContains', 'extension'} <= set(schema['properties'])

fails = [key for key, passes in checks.items() if not passes]
for fail in fails:
    print('FAIL:', fail)
if fails:
    raise SystemExit(1)
print(f'Upload-workspace DXFunction/council durability source audit passed: {len(checks)} checks.')
