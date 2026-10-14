namespace LocalGPT.BusinessObjects;

/// <summary>Defines persisted Council context-boundary capability keys shared by seeded teams and runtime prompt assembly.</summary>
public sealed class CouncilContextCapabilities
{
    /// <summary>Prevents unrelated memory, project/repository inventories and general capability briefings from being injected into a self-contained role workflow.</summary>
    public const string RoleIsolated = "localgpt.council.context.role-isolated";

    /// <summary>
    /// Initializes a new <see cref="CouncilContextCapabilities"/> instance and captures the dependencies or initial state required by its council context capabilities workflow.
    /// </summary>
    private CouncilContextCapabilities()
    {
    }
}
