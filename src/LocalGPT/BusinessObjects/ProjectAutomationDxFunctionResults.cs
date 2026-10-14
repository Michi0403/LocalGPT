namespace LocalGPT.BusinessObjects;

/// <summary>Creates consistent invocation results for project/team automation handlers.</summary>
internal sealed class ProjectAutomationDxFunctionResults
{
    /// <summary>
    /// Performs success for <see cref="ProjectAutomationDxFunctionResults"/>, keeping the operation consistent with the state and invariants of the surrounding project automation DevExpress function results workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the project automation DevExpress function results operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public DxAiFunctionInvocationResult Success(object? value = null) => new() { Succeeded = true, Status = "Completed", Value = value };
    /// <summary>
    /// Performs failed for <see cref="ProjectAutomationDxFunctionResults"/>, keeping the operation consistent with the state and invariants of the surrounding project automation DevExpress function results workflow.
    /// </summary>
    /// <param name="ex">Ex value supplied to the project automation DevExpress function results operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public DxAiFunctionInvocationResult Failed(Exception ex) => new() { Succeeded = false, Status = "Failed", Error = ex.Message };
}
