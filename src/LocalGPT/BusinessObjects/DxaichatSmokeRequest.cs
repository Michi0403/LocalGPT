namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents the input contract for dxaichat smoke, carrying the values a caller supplies to the corresponding application operation.
    /// </summary>
    public sealed class DxaichatSmokeRequest
    {
        /// <summary>
        /// Gets or sets the prompt value that forms part of the dxaichat smoke state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The prompt value exposed by <see cref="DxaichatSmokeRequest"/>.</value>
        public string Prompt { get; set; } = """
            Review LocalGPT progress so far from the visible prompt only.
            Return Markdown with these sections: Structure feedback, Usability feedback, Council behavior feedback, Next test.
            Be honest about uncertainty and write "Needs verification" for anything you cannot know from this prompt.
            """;

        /// <summary>
        /// Gets or sets the max output tokens value that forms part of the dxaichat smoke state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max output tokens value exposed by <see cref="DxaichatSmokeRequest"/>.</value>
        public int MaxOutputTokens { get; set; } = 1024;

        /// <summary>
        /// Gets or sets a value indicating whether save to memory applies to the dxaichat smoke state.
        /// </summary>
        /// <value>The save to memory value exposed by <see cref="DxaichatSmokeRequest"/>.</value>
        public bool SaveToMemory { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether diagnostic system prompt applies to the dxaichat smoke state.
        /// </summary>
        /// <value>The include diagnostic system prompt value exposed by <see cref="DxaichatSmokeRequest"/>.</value>
        public bool IncludeDiagnosticSystemPrompt { get; set; } = true;

        /// <summary>
        /// Gets or sets the title value that forms part of the dxaichat smoke state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The title value exposed by <see cref="DxaichatSmokeRequest"/>.</value>
        public string? Title { get; set; }
    }
}
