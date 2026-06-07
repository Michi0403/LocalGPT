using LocalGPT.BusinessObjects;
using LocalGPT.Services;

namespace LocalGPT.Extensions.PlainStatics
{
    public static class CouncelChatStatics
    {
        public static int GetCouncilModelLoadPriority(string modelName)
        {
            if (modelName.Contains("gpt-oss", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (modelName.Contains("deepseek-r1:8b", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (modelName.Contains("gemma", StringComparison.OrdinalIgnoreCase))
                return 2;

            if (modelName.Contains("qwen", StringComparison.OrdinalIgnoreCase))
                return 3;

            return 10;
        }


        public static bool IsDynamicSession(ChatClientSession session) =>
            session.Name.StartsWith(GlobalVariableSlopCollectionToRemove.DetectedOllamaSessionPrefix, StringComparison.OrdinalIgnoreCase) ||
            session.Name.Equals(CouncelChatStatics.CouncilSessionName, StringComparison.OrdinalIgnoreCase);

        public static IEnumerable<string> OrderCouncilModelsForLoad(IEnumerable<string> modelNames)
        {
            return modelNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetCouncilModelLoadPriority)
                .ThenBy(name => name, StringComparer.OrdinalIgnoreCase);
        }
        public static string BuildDynamicSessionName(MultiModelCouncilModelCandidate candidate) =>
      $"{GlobalVariableSlopCollectionToRemove.DetectedOllamaSessionPrefix}{candidate.ModelName} @ {StringExtensions.TrimEndpoint(candidate.Endpoint)}";

        public static string BuildCandidateLabel(MultiModelCouncilModelCandidate candidate) =>
            $"{candidate.ModelName} @ {StringExtensions.TrimEndpoint(candidate.Endpoint)}";

        public static string BuildCandidateTitle(MultiModelCouncilModelCandidate candidate)
        {
            var details = string.IsNullOrWhiteSpace(candidate.Details)
                ? "No model details reported."
                : candidate.Details;
            return $"{candidate.Provider} at {candidate.Endpoint}. {details}";
        }
        public const int MinCouncilOutputTokens = 256;
        public const int DefaultCouncilOutputTokens = 262144;
        public const int MaxCouncilOutputTokens = 262144;
        public const int MinCouncilContextTokens = 2048;
        public const int DefaultCouncilContextTokens = 262144;
        public const int MaxCouncilContextTokens = 262144;
        public const string CouncilSessionName = "AI Council — selected Ollama models";
    }
}
