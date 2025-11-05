namespace LocalGPT.BusinessObjects
{
    public class ConnectionStringsCoreOptions()
    {
        public const string ConnectionStringsCore = "ConnectionStringsCore";

        public string? ConnectionString { get; set; }

        public string? EasyTestConnectionString { get; set; }

        public string? DefaultConnection { get; set; }
    }
}