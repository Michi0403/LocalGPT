using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Owns the same .NET GUID generation and parsing primitive used by persisted project identities.</summary>
/// <param name="logger">Diagnostic logger; supplied values are not written to logs.</param>
public sealed class GuidUtilityService(ILogger<GuidUtilityService> logger) : IGuidUtilityService
{
    /// <inheritdoc />
    public GuidUtilityResult Execute(GuidUtilityRequest request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.Operation == "new") return new GuidUtilityResult(true, Guid.NewGuid().ToString("D"), true);
            if (request.Operation == "validate")
                return Guid.TryParse(request.Value, out var value) && value != Guid.Empty
                    ? new GuidUtilityResult(true, value.ToString("D"), false)
                    : new GuidUtilityResult(false, string.Empty, false);
            throw new ArgumentException("Operation must be new or validate.", nameof(request));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "GUID utility operation failed.");
            throw;
        }
    }
}
