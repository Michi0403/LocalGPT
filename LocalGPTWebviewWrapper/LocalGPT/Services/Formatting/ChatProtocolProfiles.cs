using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Formatting;

public sealed class ChatProtocolProfileCatalog
{
    private ChatProtocolProfileCatalog() { }
    public static IReadOnlyList<IChatProtocolProfile> CreateDefaults() =>
    [
        new HarmonyChatProtocolProfile(),
        new DeepSeekChatProtocolProfile(),
        new GemmaChatProtocolProfile(),
        new AppleChatProtocolProfile(),
        new ThinkTagsChatProtocolProfile(),
        new PlainTextChatProtocolProfile()
    ];

    public static IChatProtocolProfile ResolveExact(
        IEnumerable<IChatProtocolProfile> profiles,
        ChatResponseProtocol protocol) =>
        profiles.FirstOrDefault(profile => profile.Protocol == protocol)
        ?? new PlainTextChatProtocolProfile();
}

public sealed class HarmonyChatProtocolProfile : IChatProtocolProfile
{
    public ChatResponseProtocol Protocol => ChatResponseProtocol.Harmony;
    public int Priority => 100;
    public bool MatchesModel(string modelName) =>
        ContainsAny(modelName, "harmony", "gpt-oss");
    public string NormalizeThinking(string text) => text;
    public string NormalizeContent(string text) => text;

    internal static bool ContainsAny(string value, params string[] needles) =>
        needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
}

public sealed class DeepSeekChatProtocolProfile : IChatProtocolProfile
{
    private static readonly string[] ControlTokens =
    [
        "<｜begin▁of▁sentence｜>",
        "<｜end▁of▁sentence｜>",
        "<｜User｜>",
        "<｜Assistant｜>"
    ];

    public ChatResponseProtocol Protocol => ChatResponseProtocol.DeepSeek;
    public int Priority => 90;
    public bool MatchesModel(string modelName) =>
        HarmonyChatProtocolProfile.ContainsAny(modelName, "deepseek", "deep-seek", "r1-distill");
    public string NormalizeThinking(string text) => Strip(text);
    public string NormalizeContent(string text) => Strip(text);

    private static string Strip(string text) => ReplaceAll(text, ControlTokens);

    internal static string ReplaceAll(string text, IEnumerable<string> tokens)
    {
        var result = text;
        foreach (var token in tokens)
            result = result.Replace(token, string.Empty, StringComparison.OrdinalIgnoreCase);
        return result;
    }
}

public sealed class GemmaChatProtocolProfile : IChatProtocolProfile
{
    private static readonly string[] ControlTokens =
    [
        "<bos>",
        "<eos>",
        "<start_of_turn>model\n",
        "<start_of_turn>assistant\n",
        "<start_of_turn>model",
        "<start_of_turn>assistant",
        "<end_of_turn>"
    ];

    public ChatResponseProtocol Protocol => ChatResponseProtocol.Gemma;
    public int Priority => 80;
    public bool MatchesModel(string modelName) =>
        HarmonyChatProtocolProfile.ContainsAny(modelName, "gemma", "codegemma", "shieldgemma");
    public string NormalizeThinking(string text) => DeepSeekChatProtocolProfile.ReplaceAll(text, ControlTokens);
    public string NormalizeContent(string text) => DeepSeekChatProtocolProfile.ReplaceAll(text, ControlTokens);
}

public sealed class AppleChatProtocolProfile : IChatProtocolProfile
{
    private static readonly string[] ControlTokens =
    [
        "<|start_of_role|>assistant<|end_of_role|>",
        "<|start_of_role|>analysis<|end_of_role|>",
        "<|start_of_turn|>assistant",
        "<|end_of_turn|>",
        "<|end_of_text|>",
        "<|eot_id|>"
    ];

    public ChatResponseProtocol Protocol => ChatResponseProtocol.Apple;
    public int Priority => 70;
    public bool MatchesModel(string modelName) =>
        HarmonyChatProtocolProfile.ContainsAny(modelName, "apple", "openelm", "afm", "foundation-model", "mlx-");
    public string NormalizeThinking(string text) => DeepSeekChatProtocolProfile.ReplaceAll(text, ControlTokens);
    public string NormalizeContent(string text) => DeepSeekChatProtocolProfile.ReplaceAll(text, ControlTokens);
}

public sealed class ThinkTagsChatProtocolProfile : IChatProtocolProfile
{
    public ChatResponseProtocol Protocol => ChatResponseProtocol.ThinkTags;
    public int Priority => 50;
    public bool MatchesModel(string modelName) =>
        HarmonyChatProtocolProfile.ContainsAny(modelName, "qwq", "qwen3", "thinking");
    public string NormalizeThinking(string text) => text;
    public string NormalizeContent(string text) => text;
}

public sealed class PlainTextChatProtocolProfile : IChatProtocolProfile
{
    public ChatResponseProtocol Protocol => ChatResponseProtocol.PlainText;
    public int Priority => 0;
    public bool MatchesModel(string modelName) => false;
    public string NormalizeThinking(string text) => text;
    public string NormalizeContent(string text) => text;
}
