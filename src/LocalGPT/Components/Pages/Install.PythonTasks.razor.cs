using LocalGPT.BusinessObjects;

namespace LocalGPT.Components.Pages;

/// <summary>
/// Renders the install Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding LocalGPT interface.
/// </summary>
public partial class Install
{
    /// <summary>
    /// Stores the in-memory python task rows collection maintained internally by <see cref="Install"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<PythonJobSnapshot> PythonTaskRows = [];

    /// <summary>
    /// Refreshes python task rows for <see cref="Install"/>, keeping the operation consistent with the state and invariants of the surrounding install workflow.
    /// </summary>
    private void RefreshPythonTaskRows()
    {
        try
        {
            PythonTaskRows = PythonJobs.ListJobs(100);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Refreshing Python job monitoring failed.");
            Notifier.ShowError("PythonTasks", "Python task status could not be loaded.", "Python tasks");
        }
    }

    /// <summary>
    /// Determines whether cel python task for <see cref="Install"/>, keeping the operation consistent with the state and invariants of the surrounding install workflow.
    /// </summary>
    /// <param name="jobId">Identifier of the job to use for this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task CancelPythonTaskAsync(Guid jobId)
    {
        try
        {
            // A named UI command requests cancellation of one displayed, concrete job.
            // The API route and AI function retain their own approval boundaries.
            PythonJobs.CancelJob(jobId);
            PythonTaskRows = PythonJobs.ListJobs(100);
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Cancelling Python task {JobId} failed.", jobId);
            Notifier.ShowError("PythonTasks", "Python task cancellation failed.", "Python tasks");
        }
    }
}
