using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects
{
    public class PromptSuggestion
    {
        public string Title { get; set; }
        [Column(TypeName = "TEXT")]
        public string Text { get; set; }
        [Column(TypeName = "TEXT")]
        public string PromptMessage { get; set; }
        public PromptSuggestion(string title, string text, string promptMessage)
        {
            Title = title;
            Text = text;
            PromptMessage = promptMessage;
        }
    }
}
