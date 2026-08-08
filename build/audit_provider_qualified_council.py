#!/usr/bin/env python3
"""Static release gate for LocalGPT provider-qualified Council and Benchmark Council wiring."""
from __future__ import annotations

import argparse
import re
from pathlib import Path


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    args = parser.parse_args()
    root = args.root.resolve()

    checks: list[tuple[str, bool]] = []

    def text(relative: str) -> str:
        path = root / relative
        checks.append((f"file exists: {relative}", path.is_file()))
        return path.read_text(encoding="utf-8") if path.is_file() else ""

    models = text("src/LocalGPT/BusinessObjects/ProviderModelModels.cs")
    benchmark_models = text("src/LocalGPT/BusinessObjects/ProviderModelBenchmarkModels.cs")
    runtime = text("src/LocalGPT/Services/ProviderModelRuntimeService.cs")
    runtime_interface = text("src/LocalGPT/Interfaces/IProviderModelRuntimeService.cs")
    benchmark = text("src/LocalGPT/Services/ProviderModelBenchmarkService.cs")
    chat_factory = text("src/LocalGPT/Services/ChatClientFactory.cs")
    council = text("src/LocalGPT/Services/MultiModelCouncilService.cs")
    one_wire_execution = text("src/LocalGPT/Services/OneWire/OneWireExecutionServices.cs")
    panel = text("src/LocalGPT/Components/Shared/ProviderModelPanel.razor")
    batch_panel = text("src/LocalGPT/Components/Shared/ProviderModelBenchmarkCouncilPanel.razor")
    chat = text("src/LocalGPT/Components/Pages/Chat.razor")
    model_council = text("src/LocalGPT/Components/Pages/ModelCouncil.razor")
    install = text("src/LocalGPT/Components/Pages/Install.razor")
    wire = text("src/LocalGPT.WireProtocolVersion/OneWireProtocolContracts.cs")
    presets = text("src/LocalGPT/Services/ModelPresetService.cs")
    planner = text("src/LocalGPT/Services/Council/Scheduling/CouncilHardwareRoadPlanner.cs")
    program = text("src/LocalGPT/Program.cs")
    adaptive_benchmark = text("src/LocalGPT/Services/AdaptiveOllamaBenchmarkWiring.cs")
    provider_text = text("src/LocalGPT/Services/CouncilTextService.ProviderModels.cs")
    project = text("src/LocalGPT/LocalGPT.csproj")

    def contains(name: str, haystack: str, needle: str) -> None:
        checks.append((name, needle in haystack))

    def excludes(name: str, haystack: str, needle: str) -> None:
        checks.append((name, needle not in haystack))

    contains("selection key retains endpoint", models, " @ {normalizedEndpoint}")
    contains("stable identity hashes provider endpoint and model", models, "SHA256.HashData")
    contains("ambiguous bare names rejected", runtime, "is exposed by multiple providers")
    contains("stale qualified identities rejected", runtime, "is no longer available")
    contains("explicit run identities remembered", runtime, "public void Remember(ProviderModelReference model)")
    contains("cloud credentials endpoint-bound", runtime, "EnsureCredentialEndpointMatch")
    contains("OpenAI-compatible credentials remain endpoint-owned", runtime, "Credentials are endpoint-owned; never forward one configured host's key to another host")
    contains("runtime discovery credentials endpoint-bound", runtime, "local.ApiKey,")
    contains("chat discovery credentials endpoint-bound", chat_factory, "ResolveOpenAiCompatibleModel(configuredEndpoint, loc.ModelName, loc.ApiKey")
    contains("chat runtime credentials endpoint-bound", chat_factory, 'var runtimeApiKey = !string.IsNullOrWhiteSpace(loc.ApiKey) ? loc.ApiKey : "local-no-key";')
    contains("Ollama native client supported", runtime, "OllamaThinkingChatClient")
    contains("OpenAI compatible client supported", runtime, "global::OpenAI.OpenAIClient")
    contains("Azure OpenAI client supported", runtime, "AzureOpenAIClient")
    contains("multiple Ollama cores discovered", runtime, "options.OllamaCores")
    contains("multiple OpenAI-compatible cores discovered", runtime, "options.ChatGPTLocalCores")
    contains("configured remote OpenAI-compatible endpoints accepted", runtime, "neither configured nor a loopback provider")
    contains("LM Studio fallback discovered", runtime, "127.0.0.1:1234/v1")
    contains("benchmark target count bounded", benchmark, ".Take(24)")
    contains("benchmark profiles bounded", benchmark, "Math.Clamp(request.MaxProfilesPerModel, 1, 6)")
    contains("benchmark calls cancellable", benchmark, "CreateLinkedTokenSource")
    contains("benchmark request carries stable run id", benchmark_models, "public Guid RunId")
    contains("benchmark registers detachable live session", benchmark, "liveSessions.Begin(")
    contains("benchmark live stop token is linked", benchmark, "CreateLinkedTokenSource(cancellationToken, liveCancellation)")
    contains("benchmark publishes visible progress", benchmark, "liveSessions.Append(report.RunId")
    contains("benchmark always completes live session", benchmark, "liveSessions.Complete(report.RunId)")
    contains("benchmark calls disable automatic tools", benchmark, "enableAutomaticTools: false")
    contains("provider runtime exposes automatic-tool toggle", runtime_interface, "bool enableAutomaticTools = true")
    contains("provider runtime exposes benchmark failure propagation", runtime_interface, "bool throwOnFailure = false")
    contains("Ollama client receives automatic-tool toggle", runtime, "enableAutomaticTools,")
    contains("Ollama client receives benchmark failure propagation", runtime, "throwOnFailure)")
    contains("review evidence marked untrusted", benchmark, "untrusted model output previews")
    contains("apply requires confirmation", benchmark, "Fresh human confirmation is required")
    contains("recommendations retain provider endpoint", benchmark, "ProviderEndpoint = target.Model.Endpoint")
    checks.append(("non-Ollama recommendation clears num_gpu", bool(re.search(r"\?\s*target\.Recommendation\.OllamaNumGpu\s*:\s*null", benchmark))))
    contains("Council request carries provider selections", council, "request.ModelSelections = references")
    contains("authoritative selections suppress duplicate bare names", council, "Provider-qualified selections are authoritative")
    contains("Council runtime uses provider client factory", council, "providerModels.CreateChatClient")
    contains("provider step diagnostics retained", council, "ProviderEndpoint = providerModel.Endpoint")
    contains("Ollama unload restricted to Ollama", council, "providerModel.ProviderKind.Equals(ProviderModelKinds.Ollama")
    contains("legacy BaseUri retained", council, "Legacy bare model name bound to the explicitly requested Ollama BaseUri")
    contains("same-name routes not guessed", council, "matches.Count > 1")
    contains("same-name Council leader not guessed", council, "bareLeaderMatches.Count > 1")
    contains("wire route provider kind", wire, "public string ProviderKind")
    contains("wire route provider endpoint", wire, "public string ProviderEndpoint")
    contains("wire route provider model", wire, "public string ProviderModelName")
    contains("OneWire routes hydrate provider selections", one_wire_execution, "ModelSelections = wireRequest.ModelRoutes")
    contains("single-model panel benchmark action", panel, ">Benchmark<")
    contains("single-model panel properties action", panel, ">Properties<")
    contains("single-model panel apply action", panel, ">Apply recommendation<")
    contains("single-model panel cancellation", panel, ">Cancel<")
    contains("batch council runs all selected", batch_panel, "Benchmark all selected models")
    contains("batch council exposes live Chat transcript", batch_panel, "Open benchmark transcript in Chat")
    contains("single-model benchmark exposes live Chat transcript", panel, "Open live transcript")
    contains("batch council applies all", batch_panel, "Apply all recommendations")
    contains("batch results invalidated on selection change", batch_panel, "Selection changed. Run Benchmark Council again")
    excludes("shared model panel has no nested render mode", panel, "@rendermode")
    excludes("batch panel has no nested render mode", batch_panel, "@rendermode")
    contains("Chat uses reusable model panel", chat, "<ProviderModelPanel")
    contains("Chat uses Benchmark Council", chat, "<ProviderModelBenchmarkCouncilPanel")
    contains("Chat refreshes live-session list for detached runs", chat, "ScheduleLiveCouncilListRefresh")
    contains("Chat saves provider-qualified routes", chat, "CreateProviderQualifiedCouncilRoutes()")
    contains("ModelCouncil uses reusable model panel", model_council, "<ProviderModelPanel")
    contains("ModelCouncil uses Benchmark Council", model_council, "<ProviderModelBenchmarkCouncilPanel")
    contains("Install uses reusable model panel", install, "<ProviderModelPanel")
    contains("legacy preset migration avoids qualified misclassification", presets, "!new ProviderModelIdentity().LooksProviderQualified(route.ModelName)")
    contains("hardware planner strips non-Ollama num_gpu", planner, "var isOllamaRoute")
    contains("runtime service registered", program, "AddScoped<IProviderModelRuntimeService, ProviderModelRuntimeService>")
    contains("benchmark service registered", program, "AddScoped<IProviderModelBenchmarkService, ProviderModelBenchmarkService>")
    contains("benchmark applied event exists", benchmark_models, "ProviderModelBenchmarkAppliedEvent")
    contains("batch applied event exists", benchmark_models, "ProviderModelBenchmarkBatchAppliedEvent")
    contains("provider runtime aliases LocalGPT configuration root", runtime, "using ConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot;")
    contains("configured Ollama enumeration is materialized", runtime, "private IReadOnlyList<OllamaCoreOptions> EnumerateOllama")
    excludes("configured Ollama enumeration does not yield", runtime, "yield return primary")
    contains("single-model panel text is service-owned", panel, "CouncilText.ProviderModelReviewerSummary")
    contains("batch panel signature is service-owned", batch_panel, "CouncilText.ProviderModelBenchmarkCouncilSignature")
    contains("provider text service owns reviewer composition", provider_text, 'string.Join(" + ", reviewers)')
    contains("adaptive benchmark reuses provider identity", adaptive_benchmark, "providerIdentity.CreateSelectionKey(providerName")
    excludes("Council no longer injects obsolete prompt service", council, "IPromptConfigService promptConfigService")
    contains("failed final recovery marks the Council step", council, "Error = finalAnswerError")
    contains("failed verifier is not presented as peer verification", council, "did not produce a substantive peer-verification answer")
    version_match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", project)
    checks.append((
        "application patch version advanced",
        bool(version_match and tuple(map(int, version_match.groups())) >= (2, 4, 5))))

    failures = [name for name, passed in checks if not passed]
    if failures:
        for name in failures:
            print(f"FAIL: {name}")
        print(f"Provider-qualified Council feature audit failed: {len(failures)}/{len(checks)} checks failed.")
        return 1

    print(f"Provider-qualified Council feature audit passed: {len(checks)} checks.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
