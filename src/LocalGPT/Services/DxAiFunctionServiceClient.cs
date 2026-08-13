using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Scoped control-flow client for DI-backed DXAIFunctions. It deliberately
/// invokes the registry directly instead of making loopback HTTP calls.
/// One scoped client permits one active call, which gives the UI deterministic
/// cancellation and prevents overlapping mutation confirmations.
/// </summary>
/// <param name="registry">Devexpress ai function registry dependency used by the DevExpress AI function service workflow to provide the corresponding application capability.</param>
/// <param name="sessionContext">Chat session context dependency used by the DevExpress AI function service workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DxAiFunctionServiceClient(
    IDxAiFunctionRegistry registry,
    IChatSessionContext sessionContext,
    ILogger<DxAiFunctionServiceClient> logger) : IDxAiFunctionServiceClient, IDisposable
{
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to call gate state owned by <see cref="DxAiFunctionServiceClient"/>.
    /// </summary>
    private readonly SemaphoreSlim callGate = new(1, 1);
    /// <summary>
    /// Stores the internal state gate state used by <see cref="DxAiFunctionServiceClient"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object stateGate = new();
    /// <summary>
    /// Stores the cancellation source used by <see cref="DxAiFunctionServiceClient"/> to stop its current background or asynchronous operation.
    /// </summary>
    private CancellationTokenSource? activeCall;
    /// <summary>
    /// Stores the internal cancellation reason state used by <see cref="DxAiFunctionServiceClient"/> while executing its surrounding workflow.
    /// </summary>
    private string? cancellationReason;
    /// <summary>
    /// Stores the internal disposed state used by <see cref="DxAiFunctionServiceClient"/> while executing its surrounding workflow.
    /// </summary>
    private bool disposed;

    /// <summary>
    /// Gets or sets the stable current operation identifier used to identify or correlate this DevExpress AI function service instance with related application state.
    /// </summary>
    /// <value>The current operation identifier value exposed by <see cref="DxAiFunctionServiceClient"/>.</value>
    public Guid? CurrentOperationId { get; private set; }

    /// <summary>
    /// Retrieves functions for <see cref="DxAiFunctionServiceClient"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function service workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<DxaichatFunctionInfo> GetFunctions() {
    try
    {
        return registry.GetFunctions();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionServiceClient)}.{nameof(GetFunctions)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionServiceClient)}.{nameof(GetFunctions)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs call for <see cref="DxAiFunctionServiceClient"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function service workflow.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI function service operation and used when producing its result.</param>
    /// <param name="parameters">Parameters value supplied to the DevExpress AI function service operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="automaticInvocation">Value indicating whether automatic invocation should apply to this operation.</param>
    /// <param name="requestedBy">Requested by value supplied to the DevExpress AI function service operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> CallAsync(
        string functionName,
        object? parameters = null,
        bool userConfirmed = false,
        bool automaticInvocation = false,
        string requestedBy = "CurrentUser",
        CancellationToken cancellationToken = default)
    {
    try
    {
            var request = new DxAiFunctionInvocationRequest
            {
                Parameters = parameters is JsonElement element
                    ? element.Clone()
                    : JsonSerializer.SerializeToElement(parameters ?? new { }),
                UserConfirmed = userConfirmed,
                AutomaticInvocation = automaticInvocation,
                RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? "CurrentUser" : requestedBy.Trim()
            };
            return CallAsync(functionName, request, cancellationToken);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionServiceClient)}.{nameof(CallAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionServiceClient)}.{nameof(CallAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs call for <see cref="DxAiFunctionServiceClient"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function service workflow.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI function service operation and used when producing its result.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> CallAsync(
        string functionName,
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        ArgumentNullException.ThrowIfNull(request);
        await callGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        var operationId = request.OperationId ?? Guid.NewGuid();
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (stateGate)
        {
            activeCall = linkedCancellation;
            cancellationReason = null;
            CurrentOperationId = operationId;
        }

        try
        {
            var invocation = new DxAiFunctionInvocationRequest
            {
                OperationId = operationId,
                Parameters = request.Parameters.ValueKind == JsonValueKind.Undefined
                    ? JsonSerializer.SerializeToElement(new { })
                    : request.Parameters.Clone(),
                UserConfirmed = request.UserConfirmed,
                AutomaticInvocation = request.AutomaticInvocation,
                ConfirmationSummaryHash = request.ConfirmationSummaryHash,
                RequestedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "CurrentUser" : request.RequestedBy.Trim(),
                ConversationId = request.ConversationId ?? sessionContext.ConversationId,
                ProjectId = request.ProjectId ?? sessionContext.ProjectId,
                ProjectVersionId = request.ProjectVersionId ?? sessionContext.ProjectVersionId,
                ApplicationVersion = string.IsNullOrWhiteSpace(request.ApplicationVersion)
                    ? sessionContext.ApplicationVersion
                    : request.ApplicationVersion.Trim()
            };

            return await registry.InvokeAsync(functionName, invocation, linkedCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            string? reason;
            lock (stateGate)
                reason = cancellationReason;
            logger.LogInformation(exception, "DXAIFunction operation {OperationId} was cancelled by the current user.", operationId);
            return new DxAiFunctionInvocationResult
            {
                FunctionName = functionName,
                OperationId = operationId,
                Status = "Cancelled",
                Error = string.IsNullOrWhiteSpace(reason) ? "The current user cancelled the function call." : reason
            };
        }
        finally
        {
            lock (stateGate)
            {
                activeCall = null;
                cancellationReason = null;
                CurrentOperationId = null;
            }
            callGate.Release();
        }
    }

    /// <summary>
    /// Determines whether cel for <see cref="DxAiFunctionServiceClient"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function service workflow.
    /// </summary>
    public void Cancel() {
    try
    {
        CancelWithReason("The current user cancelled the function call.");
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionServiceClient)}.{nameof(Cancel)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionServiceClient)}.{nameof(Cancel)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether cel with reason for <see cref="DxAiFunctionServiceClient"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function service workflow.
    /// </summary>
    /// <param name="reason">Reason value supplied to the DevExpress AI function service operation and used when producing its result.</param>
    public void CancelWithReason(string reason)
    {
    try
    {
            lock (stateGate)
            {
                cancellationReason = string.IsNullOrWhiteSpace(reason)
                    ? "The current user cancelled the function call."
                    : reason.Trim();
                activeCall?.Cancel();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionServiceClient)}.{nameof(CancelWithReason)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionServiceClient)}.{nameof(CancelWithReason)} failed.");
        throw;
    }
}

    /// <summary>
    /// Releases resources owned by <see cref="DxAiFunctionServiceClient"/> and leaves the DevExpress AI function service workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
    try
    {
            lock (stateGate)
            {
                if (disposed)
                    return;
                disposed = true;
                activeCall?.Cancel();
                activeCall = null;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionServiceClient)}.{nameof(Dispose)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionServiceClient)}.{nameof(Dispose)} failed.");
        throw;
    }
}
}
