using DevExpress.Blazor.Internal.TreeListData;

namespace LocalGPT.BusinessObjects
{
    public class ConfigurationRoot
    {
        public const string Configuration = "Configuration";

        public LoggingCoreOptions? LoggingCore { get; set; }
        public PythonCoreOptions? PythonCore { get; set; }
        public ConnectionStringsCoreOptions? ConnectionStringsCore { get; set; }
        public AICoreOptions? AIOptionsCore { get; set; }
    }
}
