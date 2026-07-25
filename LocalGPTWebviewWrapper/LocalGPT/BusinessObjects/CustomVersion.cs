using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects
{
    public class CustomVersion : ICustomVersion
    {
        public string Version { get; set; }

        public CustomVersion(string version)
        {
            Version = version;
        }


    }
}
