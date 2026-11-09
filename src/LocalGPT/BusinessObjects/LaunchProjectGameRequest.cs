using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Supplies chat/Council ownership metadata when an already-built Game project is handed to the runtime.
/// </summary>
public sealed class LaunchProjectGameRequest
{
    /// <summary>Associates the new runtime session with a saved/current chat when available.</summary>
    /// <value>The owning conversation identifier, or <c>null</c> for an unbound launch.</value>
    public Guid? ConversationId { get; set; }
    /// <summary>Associates the new runtime session with the active Council run when applicable.</summary>
    /// <value>The owning Council run identifier, or <c>null</c> for a standalone launch.</value>
    public Guid? CouncilRunId { get; set; }
    /// <summary>Provides the bounded human-readable actor label recorded as the launch initiator.</summary>
    /// <value>The launch initiator label.</value>
    public string StartedBy { get; set; } = "Human User";
}
