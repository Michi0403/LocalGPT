using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LocalGPT.Security;

/// <summary>
/// Represents a human approval required attribute application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class HumanApprovalRequiredAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Initializes a new <see cref="HumanApprovalRequiredAttribute"/> instance and captures the dependencies or initial state required by its human approval required attribute workflow.
    /// </summary>
    /// <param name="operationKey">Operation key value supplied to the human approval required attribute operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the human approval required attribute operation and used when producing its result.</param>
    /// <param name="description">Description value supplied to the human approval required attribute operation and used when producing its result.</param>
    /// <param name="riskLevel">Risk level value supplied to the human approval required attribute operation and used when producing its result.</param>
    /// <param name="requestedRole">Requested role value supplied to the human approval required attribute operation and used when producing its result.</param>
    /// <param name="requiredBeforeCompletion">Value indicating whether required before completion should apply to this operation.</param>
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
