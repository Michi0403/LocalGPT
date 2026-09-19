namespace LocalGPT.BusinessObjects;

/// <summary>Defines persisted Council context-boundary capability keys shared by seeded teams and runtime prompt assembly.</summary>
public sealed class CouncilContextCapabilities
{
    /// <summary>Prevents unrelated memory, project/repository inventories and general capability briefings from being injected into a self-contained role workflow.</summary>
    public const string RoleIsolated = "localgpt.council.context.role-isolated";

    private CouncilContextCapabilities()
    {
    }
}
