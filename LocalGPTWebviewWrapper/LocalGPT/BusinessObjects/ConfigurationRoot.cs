namespace LocalGPT.BusinessObjects
{
    public class ConfigurationRoot
    {
        public const string Configuration = "Configuration";

        public LoggingCoreOptions? LoggingCore { get; set; }
        public PythonCoreOptions? PythonCore { get; set; }
        public ConnectionStringsCoreOptions? ConnectionStringsCore { get; set; }
    }
}
