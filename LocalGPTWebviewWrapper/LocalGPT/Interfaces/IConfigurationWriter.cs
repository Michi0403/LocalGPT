namespace LocalGPT.Interfaces
{
    public interface IConfigurationWriter
    {
        Task SaveAsync(BusinessObjects.ConfigurationRoot root, CancellationToken ct = default);
    }
}
