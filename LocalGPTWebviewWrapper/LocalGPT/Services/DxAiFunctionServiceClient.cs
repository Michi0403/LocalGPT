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
public sealed class DxAiFunctionServiceClient(
    IDxAiFunctionRegistry registry,
    IChatSessionContext sessionContext,
    ILogger<DxAiFunctionServiceClient> logger) : IDxAiFunctionServiceClient, IDisposable
{
    private readonly SemaphoreSlim callGate = new(1, 1);
    private readonly object stateGate = new();
    private CancellationTokenSource? activeCall;
    private string? cancellationReason;
    private bool disposed;

    public Guid? CurrentOperationId { get; private set; }

    public IReadOnlyList<DxaichatFunctionInfo> GetFunctions() => registry.GetFunctions();

    public Task<DxAiFunctionInvocationResult> CallAsync(
        string functionName,
        object? parameters = null,
        bool userConfirmed = false,
        bool automaticInvocation = false,
        string requestedBy = "CurrentUser",
        CancellationToken cancellationToken = default)
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

    public void Cancel() => CancelWithReason("The current user cancelled the function call.");

    public void CancelWithReason(string reason)
    {
        lock (stateGate)
        {
            cancellationReason = string.IsNullOrWhiteSpace(reason)
                ? "The current user cancelled the function call."
                : reason.Trim();
            activeCall?.Cancel();
        }
    }

    public void Dispose()
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
}
