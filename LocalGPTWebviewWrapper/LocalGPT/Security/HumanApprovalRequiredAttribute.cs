using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LocalGPT.Security;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class HumanApprovalRequiredAttribute : TypeFilterAttribute
{
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
