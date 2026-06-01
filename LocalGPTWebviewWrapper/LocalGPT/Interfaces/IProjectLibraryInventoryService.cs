namespace LocalGPT.Interfaces
{
    public interface IProjectLibraryInventoryService
    {
        Task<string> BuildDevExpressBriefingAsync(CancellationToken cancellationToken = default);
    }
}
