using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Requests compilation of the already-persisted Game-project design baseline into a runtime definition artifact.
/// Build requests deliberately carry no editable game design fields so compiling cannot silently mutate project authoring state.
/// </summary>
public sealed class BuildProjectGameRequest
{
    /// <summary>Records the explicit approval required before the Project system creates or replaces the durable GameBuild artifact.</summary>
    /// <value><see langword="true"/> only for an approved build invocation.</value>
    public bool UserConfirmed { get; set; }
}
