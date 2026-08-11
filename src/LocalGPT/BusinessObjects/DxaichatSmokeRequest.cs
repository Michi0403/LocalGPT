namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a dxaichat smoke request.
    /// </summary>
    public sealed class DxaichatSmokeRequest
    {
        /// <summary>
        /// Gets or sets prompt.
        /// </summary>
        public string Prompt { get; set; } = """
            Review LocalGPT progress so far from the visible prompt only.
            Return Markdown with these sections: Structure feedback, Usability feedback, Council behavior feedback, Next test.
            Be honest about uncertainty and write "Needs verification" for anything you cannot know from this prompt.
            """;

        /// <summary>
        /// Gets or sets max output tokens.
        /// </summary>
        public int MaxOutputTokens { get; set; } = 1024;

        /// <summary>
        /// Gets or sets save to memory.
        /// </summary>
        public bool SaveToMemory { get; set; } = true;

        /// <summary>
        /// Gets or sets include diagnostic system prompt.
        /// </summary>
        public bool IncludeDiagnosticSystemPrompt { get; set; } = true;

        /// <summary>
        /// Gets or sets title.
        /// </summary>
        public string? Title { get; set; }
    }
}
