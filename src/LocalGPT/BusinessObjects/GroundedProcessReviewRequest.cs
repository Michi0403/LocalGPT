namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a grounded process review request.
    /// </summary>
    public sealed class GroundedProcessReviewRequest
    {
        /// <summary>
        /// Gets or sets facts.
        /// </summary>
        public List<string> Facts { get; set; } = [];

        /// <summary>
        /// Gets or sets question.
        /// </summary>
        public string? Question { get; set; }

        /// <summary>
        /// Gets or sets max output tokens.
        /// </summary>
        public int MaxOutputTokens { get; set; } = 4096;
    }
}
