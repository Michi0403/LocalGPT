using System.Text.Json;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Monitor/cancel Python tasks regardless of their interpreter transport. Executable source enters only through approved runtime plugins.</summary>
public interface IPythonJobOrchestrationService
{
    /// <summary>
    /// Lists jobs as part of the python job orchestration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="maximum">Maximum value supplied to the python job orchestration operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<PythonJobSnapshot> ListJobs(int maximum = 100);
    /// <summary>
    /// Determines whether cel job as part of the python job orchestration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="jobId">Identifier of the job to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool CancelJob(Guid jobId);
    /// <summary>
    /// Performs run approved python plugin as part of the python job orchestration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="compiler">Compiler value supplied to the python job orchestration operation and used when producing its result.</param>
    /// <param name="scriptPath">Script path value supplied to the python job orchestration operation and used when producing its result.</param>
    /// <param name="parameters">Parameters value supplied to the python job orchestration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The python plugin process result produced by the operation.</returns>
    Task<PythonPluginProcessResult> RunApprovedPythonPluginAsync(ProjectCompilerInstallation compiler, string scriptPath, JsonElement parameters, CancellationToken cancellationToken = default);
}
