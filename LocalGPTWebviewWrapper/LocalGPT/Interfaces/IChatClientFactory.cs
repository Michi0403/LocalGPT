using LocalGPT.Services;

namespace LocalGPT.Interfaces
{
    public interface IChatClientFactory
    {
        CompositeChatClient Build();
    }
}
