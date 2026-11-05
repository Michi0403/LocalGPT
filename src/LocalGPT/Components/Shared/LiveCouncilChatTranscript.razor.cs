namespace LocalGPT.Components.Shared;

/// <summary>
/// Renders the ordered transcript of a running server-owned Council inside the chat message. The component refreshes itself at most once per second,
/// renders Markdown on the thread pool and keeps completed provider-stream segments as stable render entries, so only new or changed segments are sent to the browser.
/// </summary>
public partial class LiveCouncilChatTranscript
{
}
