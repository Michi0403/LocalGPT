using DevExpress.Blazor.Internal.TreeListData;

namespace LocalGPT.BusinessObjects
{
    public class ConfigurationRoot
    {
        public const string Configuration = "Configuration";
        // Top-level ASP.NET Core bits
        //public string? AllowedHosts { get; set; }                  // e.g. "*"
        //public MsLoggingOptions? Logging { get; set; }                  // standard "Logging" section

        public LoggingCoreOptions? LoggingCore { get; set; }
        public PythonCoreOptions? PythonCore { get; set; }
        public ConnectionStringsCoreOptions? ConnectionStringsCore { get; set; }
        public AICoreOptions? AICore { get; set; }
    }
    public class MsLoggingOptions
    {
        public Dictionary<string, string>? LogLevel { get; set; }
    }
}
