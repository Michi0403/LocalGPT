using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LocalGPT.Security;

/// <summary>
/// Represents a human approval required attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class HumanApprovalRequiredAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Runs the human approval required attribute operation.
    /// </summary>
    public HumanApprovalRequiredAttribute(
        string operationKey,
        string title,
        string description,
        string riskLevel = "Medium",
        string requestedRole = "Security reviewer",
        bool requiredBeforeCompletion = false)
        : base(typeof(HumanApprovalActionFilter))
    {
        Arguments =
        [
            operationKey,
            title,
            description,
            riskLevel,
            requestedRole,
            requiredBeforeCompletion
        ];
    }
}
