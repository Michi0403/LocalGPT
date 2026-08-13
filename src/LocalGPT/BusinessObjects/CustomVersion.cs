using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a custom version application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public class CustomVersion : ICustomVersion
    {
        /// <summary>
        /// Gets or sets the version value that forms part of the custom version state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The version value exposed by <see cref="CustomVersion"/>.</value>
        public string Version { get; set; }

        /// <summary>
        /// Initializes a new <see cref="CustomVersion"/> instance and captures the dependencies or initial state required by its custom version workflow.
        /// </summary>
        /// <param name="version">Version value supplied to the custom version operation and used when producing its result.</param>
        public CustomVersion(string version)
        {
            Version = version;
        }


    }
}
