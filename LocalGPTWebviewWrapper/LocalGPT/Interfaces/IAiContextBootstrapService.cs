namespace LocalGPT.Interfaces
{
    public interface IAiContextBootstrapService
    {
        Task<string> BuildBootstrapPromptAsync(CancellationToken cancellationToken = default);
    }
}
