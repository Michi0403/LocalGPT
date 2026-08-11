using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a custom version.
    /// </summary>
    public class CustomVersion : ICustomVersion
    {
        /// <summary>
        /// Gets or sets version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Runs the custom version operation.
        /// </summary>
        public CustomVersion(string version)
        {
            Version = version;
        }


    }
}
