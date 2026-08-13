namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents the input contract for grounded process review, carrying the values a caller supplies to the corresponding application operation.
    /// </summary>
    public sealed class GroundedProcessReviewRequest
    {
        /// <summary>
        /// Gets or sets the facts collection maintained or exposed by this grounded process review instance for downstream processing.
        /// </summary>
        /// <value>The facts value exposed by <see cref="GroundedProcessReviewRequest"/>.</value>
        public List<string> Facts { get; set; } = [];

        /// <summary>
        /// Gets or sets the question value that forms part of the grounded process review state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The question value exposed by <see cref="GroundedProcessReviewRequest"/>.</value>
        public string? Question { get; set; }

        /// <summary>
        /// Gets or sets the max output tokens value that forms part of the grounded process review state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max output tokens value exposed by <see cref="GroundedProcessReviewRequest"/>.</value>
        public int MaxOutputTokens { get; set; } = 4096;
    }
}
