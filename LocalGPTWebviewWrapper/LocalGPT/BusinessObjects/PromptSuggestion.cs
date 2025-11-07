namespace LocalGPT.BusinessObjects
{
    public class PromptSuggestion
    {
        public string Title { get; set; }
        public string Text { get; set; }
        public string PromptMessage { get; set; }
        public PromptSuggestion(string title, string text, string promptMessage)
        {
            Title = title;
            Text = text;
            PromptMessage = promptMessage;
        }
    }
}
