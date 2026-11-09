using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an ambient LocalGPT context pop scope application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="restore">Restore value supplied to the ambient LocalGPT context pop scope operation and used when producing its result.</param>
/// <param name="prior">Prior value supplied to the ambient LocalGPT context pop scope operation and used when producing its result.</param>
/// <param name="loggingScope">Disposable dependency used by the ambient LocalGPT context pop scope workflow to provide the corresponding application capability.</param>
internal sealed class AmbientLocalGptContextPopScope(
    Action<AmbientLocalGptContextHolder?> restore,
    AmbientLocalGptContextHolder? prior,
    IDisposable? loggingScope) : IDisposable
{
    /// <summary>
    /// Stores the internal restore action state used by <see cref="AmbientLocalGptContextPopScope"/> while executing its surrounding workflow.
    /// </summary>
    private Action<AmbientLocalGptContextHolder?>? restoreAction = restore;
    /// <summary>
    /// Stores the internal prior holder state used by <see cref="AmbientLocalGptContextPopScope"/> while executing its surrounding workflow.
    /// </summary>
    private AmbientLocalGptContextHolder? priorHolder = prior;
    /// <summary>
    /// Stores the disposable dependency used by <see cref="AmbientLocalGptContextPopScope"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private IDisposable? activeLoggingScope = loggingScope;

    /// <summary>
    /// Releases resources owned by <see cref="AmbientLocalGptContextPopScope"/> and leaves the ambient LocalGPT context pop scope workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
        var currentRestore = Interlocked.Exchange(ref restoreAction, null);
        if (currentRestore is null)
            return;

        activeLoggingScope?.Dispose();
        activeLoggingScope = null;
        currentRestore(priorHolder);
        priorHolder = null;
    }
}
