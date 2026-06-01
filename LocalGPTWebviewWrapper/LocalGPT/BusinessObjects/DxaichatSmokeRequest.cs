namespace LocalGPT.BusinessObjects
{
    public sealed class DxaichatSmokeRequest
    {
        public string Prompt { get; set; } = """
            Review LocalGPT progress so far from the visible prompt only.
            Return Markdown with these sections: Structure feedback, Usability feedback, Council behavior feedback, Next test.
            Be honest about uncertainty and write "Needs verification" for anything you cannot know from this prompt.
            """;

        public int MaxOutputTokens { get; set; } = 1024;

        public bool SaveToMemory { get; set; } = true;

        public bool IncludeDiagnosticSystemPrompt { get; set; } = true;

        public string? Title { get; set; }
    }
}
