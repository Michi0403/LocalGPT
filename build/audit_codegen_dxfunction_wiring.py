#!/usr/bin/env python3
"""Source-only audit of LocalGPT code-generation DXFunction wiring.

This deliberately does not compile or execute generated code. It checks the DI registration,
function descriptors/schemas, workflow calls, output-kind scaffolding, Ollama textual fallback,
and absence of the former arbitrary code-generation payload/file-count ceilings.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROGRAM = (ROOT / "src/LocalGPT/Program.cs").read_text(encoding="utf-8-sig")
REGISTRY = (ROOT / "src/LocalGPT/Services/DxAiFunctionRegistry.cs").read_text(encoding="utf-8-sig")
WORKFLOW = (ROOT / "src/LocalGPT/Services/CodeGenerationWorkflowService.cs").read_text(encoding="utf-8-sig")
PROJECT_MAINTENANCE = (ROOT / "src/LocalGPT/Services/ProjectMaintenanceService.cs").read_text(encoding="utf-8-sig")
PROJECT_MODELS = (ROOT / "src/LocalGPT/BusinessObjects/ProjectMaintenanceModels.cs").read_text(encoding="utf-8-sig")
OUTPUT_MODELS = (ROOT / "src/LocalGPT/BusinessObjects/CodeGenerationWorkflowModels.cs").read_text(encoding="utf-8-sig")
ARTIFACT_FUNCTIONS = (ROOT / "src/LocalGPT/Services/ArtifactWorkspaceDxAiFunctions.cs").read_text(encoding="utf-8-sig")
CAPABILITY_FUNCTION = (ROOT / "src/LocalGPT/Services/CodeGenerationCapabilitiesDxAiFunction.cs").read_text(encoding="utf-8-sig")
CODEGEN_CONTROLLER = (ROOT / "src/LocalGPT/Controller/CodeGenerationController.cs").read_text(encoding="utf-8-sig")
REMOTE_IMPORT = (ROOT / "src/LocalGPT/Services/RemoteKnowledgeImportService.cs").read_text(encoding="utf-8-sig")
CHAT_UPLOADS = (ROOT / "src/LocalGPT/Services/ChatUploadWorkspaceService.cs").read_text(encoding="utf-8-sig")
RUNTIME_SEEDS = (ROOT / "src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs").read_text(encoding="utf-8-sig")
OLLAMA = (ROOT / "src/LocalGPT/Services/OllamaThinkingChatClient.cs").read_text(encoding="utf-8-sig")

failures: list[str] = []

def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        failures.append(f"missing {label}: {needle}")

def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        failures.append(f"forbidden {label} remains: {needle}")

require(PROGRAM, "AddScoped<ICodeGenerationWorkflowService, CodeGenerationWorkflowService>()", "workflow DI registration")
require(PROGRAM, "AddScoped<IDxAiFunctionRegistry, DxAiFunctionRegistry>()", "DXFunction registry DI registration")
require(PROGRAM, "typeof(IDxAiFunctionHandler).IsAssignableFrom(type.AsType())", "DXFunction handler discovery")
require(PROGRAM, "AddScoped(typeof(IDxAiFunctionHandler), handlerType)", "DXFunction handler scoped registration")
require(CODEGEN_CONTROLLER, "using LocalGPT.Services;", "code-generation controller service namespace import")
require(CODEGEN_CONTROLLER, "LocalGptCatalogService catalog", "code-generation controller catalog dependency")

functions = {
    "codegen.review.list": "workflow.ListReviewsAsync",
    "codegen.review.get": "workflow.GetReviewAsync",
    "codegen.review.create": "workflow.CreateReviewAsync",
    "codegen.review.execute": "workflow.ExecuteReviewAsync",
    "codegen.review.reject": "workflow.RejectReviewAsync",
}
for name, workflow_call in functions.items():
    require(REGISTRY, f'"{name}"', f"DXFunction descriptor {name}")
    require(REGISTRY, workflow_call, f"workflow call for {name}")

require(REGISTRY, '"projectRevisionId"', "projectRevisionId create-review schema")
require(REGISTRY, '"required": [\n            "goal"\n          ]', "goal required create-review schema")

output_kinds = [
    "SourceFiles", "ClassLibrary", "ConsoleApplication", "Solution",
    "LocalGptAddon", "CSharpScript", "PowerShellScript", "JavaScriptModule",
]
for kind in output_kinds:
    require(REGISTRY, f'"{kind}"', f"create-review output kind {kind}")
    require(WORKFLOW, f"CodeGenerationOutputKinds.{kind}", f"workflow output handling {kind}")

for marker in [
    "case CodeGenerationOutputKinds.SourceFiles:",
    "case CodeGenerationOutputKinds.CSharpScript:",
    "case CodeGenerationOutputKinds.PowerShellScript:",
    "case CodeGenerationOutputKinds.JavaScriptModule:",
    "case CodeGenerationOutputKinds.ClassLibrary:",
    "case CodeGenerationOutputKinds.ConsoleApplication:",
    "case CodeGenerationOutputKinds.LocalGptAddon:",
    "case CodeGenerationOutputKinds.Solution:",
]:
    require(WORKFLOW, marker, f"scaffold switch {marker}")

require(WORKFLOW, "if (request.BuildAfterGeneration && !request.UserConfirmedBuild)", "separate build confirmation")
require(WORKFLOW, "result.WrittenFiles.Add(relativePath.Replace('\\\\', '/'));", "unbounded approved-file result recording")
require(WORKFLOW, ".Take(Math.Max(1, take))", "positive caller-controlled review listing")

for legacy in [
    "MaxPayloadCharacters", "MaxFileCount", "MaxReviewTake",
    "payloadJson.Length >", "recorded++ < 5000", "approved.Count > 5000",
    "private string Limit(string? value, int maxLength",
]:
    forbid(WORKFLOW, legacy, "arbitrary code-generation ceiling/truncation")


# Repository rescans performed after approved generation must scale with the database-backed
# MaxFiles policy instead of silently imposing the old 100k source-code ceiling. The default
# request value (0) delegates to policy; callers may still request a smaller explicit scan.
forbid(WORKFLOW, "MaximumFiles = 100000", "100k generated-workspace rescan ceiling")
forbid(PROJECT_MAINTENANCE, "Math.Clamp(request.MaximumFiles, 1, 100000)", "100k project scan clamp")
require(PROJECT_MAINTENANCE, "runtimePolicy.GetInt(LocalGptRuntimeValue.MaxFiles)", "database-backed project scan file policy")
require(PROJECT_MAINTENANCE, "request.MaximumFiles > 0", "optional caller-requested project scan cap")
require(PROJECT_MODELS, "public int MaximumFiles { get; set; }", "policy-backed zero-default project scan request")
forbid(WORKFLOW, "MaximumFileBytes = 4L * 1024 * 1024 * 1024", "generated-workspace file-size source literal")
forbid(PROJECT_MAINTENANCE, "Math.Clamp(request.MaximumFileBytes", "hard-coded project scan file-size clamp")
require(PROJECT_MAINTENANCE, "runtimePolicy.GetLong(LocalGptRuntimeValue.MaxSingleFileBytes)", "database-backed project scan file-size policy")
require(PROJECT_MAINTENANCE, "request.MaximumFileBytes > 0", "optional caller-requested project scan file-size cap")
require(PROJECT_MODELS, "public long MaximumFileBytes { get; set; }", "policy-backed zero-default project scan file-size request")
forbid(PROJECT_MAINTENANCE, "EnumerateRelativeEntries(root, 5000", "fixed workspace permission-assessment entry ceiling")
require(PROJECT_MAINTENANCE, "var maximumAssessmentEntries = Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.MaxFiles));", "database-backed workspace permission-assessment entry policy")
forbid(CHAT_UPLOADS, ".Take(Math.Clamp(take, 1, 1000))", "fixed 1k chat-upload workspace file listing ceiling")
forbid(CHAT_UPLOADS, ".Take(Math.Clamp(take, 1, 100))", "fixed chat-upload workspace history listing ceiling")
require(CHAT_UPLOADS, "Math.Max(1, catalog.MaxFiles)", "database-backed chat-upload listing policy")

# Project-maintenance limits that already have runtime-policy keys must not be duplicated as
# source constants. This protects the provisioning architecture from future drift.
forbid(PROJECT_MAINTENANCE, "private const int MaxCompilerCandidates", "duplicated compiler-candidate constant")
forbid(PROJECT_MAINTENANCE, "private const int MaxCapturedCharacters", "duplicated captured-output constant")
require(PROJECT_MAINTENANCE, "LocalGptRuntimeValue.ProjectMaintenanceMaximumCompilerCandidates", "compiler-candidate runtime policy")
require(PROJECT_MAINTENANCE, "LocalGptRuntimeValue.ProjectMaintenanceMaximumCapturedCharacters", "captured-output runtime policy")

# Compatibility keys may remain in persisted policy stores, but fresh installs must no longer
# seed the former joke-size code-generation ceilings.
for policy_name in [
    "CodeGenerationMaximumPayloadCharacters",
    "CodeGenerationMaximumFileCount",
    "CodeGenerationMaximumReviewTake",
]:
    require(
        RUNTIME_SEEDS,
        f'new(LocalGptRuntimeValue.{policy_name}, nameof(LocalGptRuntimeValue.{policy_name}), "2147483647", "System.Int32")',
        f"effectively-unbounded compatibility seed {policy_name}",
    )


# Generated workspace source iteration must be executable through DI-backed tools, not merely
# advertised as controller routes. This is the plain-file path used for PowerShell and for any
# reviewed source extension when CodeDOM is not wanted.
for name in [
    "council.artifact_workspaces",
    "council.artifact_workspace_files",
    "council.artifact_workspace_file.read",
    "council.artifact_workspace_file.write",
    "council.artifact_workspace_zip",
]:
    require(ARTIFACT_FUNCTIONS, f'"{name}"', f"DI-backed generated-workspace function {name}")
require(ARTIFACT_FUNCTIONS, "SupportsDeferredApprovalRequest: true", "deferred approval for generated workspace writes")
require(ARTIFACT_FUNCTIONS, "catalog.MaxSingleFileBytes", "database-backed generated-workspace file-size policy")
require(ARTIFACT_FUNCTIONS, "runtime.ResolveWorkspaceTextFile", "generated-workspace path containment")
require(RUNTIME_SEEDS, '".ps1"', "PowerShell artifact text extension")
require(CAPABILITY_FUNCTION, '"codegen.capabilities"', "AI-visible code-generation capability directory")
require(CAPABILITY_FUNCTION, "CodeGenerationOutputKinds.PowerShellScript", "PowerShell capability declaration")
require(WORKFLOW, "GeneratePlainCSharpFallbackSource", "plain C# fallback when CodeDOM generation fails")
require(OUTPUT_MODELS, 'public const string PowerShellScript = "PowerShellScript";', "PowerShell output-kind identifier")

# The remote-import log demonstrated the old fixed 60,000-entry ceiling. It must now come from
# the same data-provisioning mechanism as the rest of LocalGPT scale policy.
forbid(REMOTE_IMPORT, "60000", "fixed 60k remote archive entry ceiling")
require(REMOTE_IMPORT, "catalog.MaxZipEntries", "database-backed remote archive entry policy")
require(REMOTE_IMPORT, "catalog.MaxFiles", "database-backed remote import file policy")
require(REMOTE_IMPORT, "catalog.MaxExtractedBytes", "database-backed remote extracted-byte policy")
require(REMOTE_IMPORT, "request.MaxLinkedPages > 0", "optional caller-requested linked-page policy")
forbid(REMOTE_IMPORT, "Math.Clamp(request.MaxLinkedPages, 1, 50)", "fixed 50-page remote crawl ceiling")

require(OLLAMA, 'Content = $$$"""', "three-dollar raw interpolated fallback literal")
require(OLLAMA, '{{{marker}}}', "fallback marker interpolation")
require(OLLAMA, '{"functionName":"exact.registry.name","arguments":{}}', "literal textual DXFunction JSON example")
require(OLLAMA, '{{{functionDirectory}}}', "fallback function directory interpolation")

if failures:
    print("Code-generation/DXFunction source audit failed:")
    for failure in failures:
        print(f"  - {failure}")
    raise SystemExit(1)

print(
    "Code-generation/DXFunction source audit passed: DI discovery, five review functions, "
    "eight output kinds including PowerShell, projectRevisionId schema, Ollama textual fallback, "
    "approval-gated plain workspace file writes, CodeDOM fallback, removal of former arbitrary "
    "payload/file/report ceilings, policy-backed remote imports, project scanning/assessment, and upload-workspace listings are present."
)
