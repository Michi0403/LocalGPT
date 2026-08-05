namespace LocalGPT.BusinessObjects
{
    public sealed class GroundedProcessReviewRequest
    {
        public List<string> Facts { get; set; } = [];

        public string? Question { get; set; }

        public int MaxOutputTokens { get; set; } = 4096;
    }
}
