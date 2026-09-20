namespace LocalGPT.BusinessObjects;

/// <summary>Creates consistent invocation results for project/team automation handlers.</summary>
internal sealed class ProjectAutomationDxFunctionResults
{
    public DxAiFunctionInvocationResult Success(object? value = null) => new() { Succeeded = true, Status = "Completed", Value = value };
    public DxAiFunctionInvocationResult Failed(Exception ex) => new() { Succeeded = false, Status = "Failed", Error = ex.Message };
}
