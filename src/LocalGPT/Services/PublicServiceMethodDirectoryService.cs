using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Reflection;

namespace LocalGPT.Services;

/// <summary>Projects the existing versioned, policy-backed service inventory into compact, purpose-based navigation.</summary>
/// <param name="catalog">Devexpress ai function catalog service dependency used by the public service method directory workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublicServiceMethodDirectoryService(IDxAiFunctionCatalogService catalog, ILogger<PublicServiceMethodDirectoryService> logger) : IPublicServiceMethodDirectoryService
{
    /// <summary>
    /// Performs search as part of the public service method directory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<PublicServiceControllerPage> SearchAsync(string? query, string? service, int skip, int take, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = (await catalog.GetEntriesAsync(cancellationToken).ConfigureAwait(false))
                .Where(entry => entry.CatalogKey.StartsWith("service:", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(service))
                rows = rows.Where(entry => entry.ImplementationTypeName.Contains(service, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(query))
                rows = rows.Where(entry => entry.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                           entry.Purpose.Contains(query, StringComparison.OrdinalIgnoreCase));
            var ordered = rows.OrderBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
            var offset = Math.Max(0, skip);
            var pageSize = Math.Clamp(take == 0 ? 50 : take, 1, 200);
            return new PublicServiceControllerPage
            {
                Total = ordered.Count, Skip = offset, Take = pageSize,
                Methods = ordered.Skip(offset).Take(pageSize).ToArray()
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Public service capability directory lookup failed; query omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs groups as part of the public service method directory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<PublicServiceGroupDescriptor>> GroupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = (await catalog.GetEntriesAsync(cancellationToken).ConfigureAwait(false))
                .Where(entry => entry.CatalogKey.StartsWith("service:", StringComparison.OrdinalIgnoreCase));
            var assembly = typeof(Program).Assembly;
            return entries.GroupBy(entry => entry.ImplementationTypeName, StringComparer.Ordinal)
                .OrderBy(group => group.First().DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var contractName = group.First().ServiceContractTypeName;
                    var contractType = assembly.GetType(contractName.Split(',')[0].Trim(), throwOnError: false);
                    return new PublicServiceGroupDescriptor
                    {
                        Service = group.First().DisplayName.Split('.')[0],
                        MethodCount = group.Count(),
                        Properties = contractType?.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(property => property.GetIndexParameters().Length == 0)
                            .Select(property => new PublicServicePropertyDescriptor
                            {
                                Name = property.Name,
                                TypeName = property.PropertyType.FullName ?? property.PropertyType.Name,
                                CanRead = property.GetMethod?.IsPublic == true,
                                CanWrite = property.SetMethod?.IsPublic == true
                            }).ToArray() ?? []
                    };
                }).ToArray();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Public service business-purpose grouping failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs find as part of the public service method directory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionCatalogEntry?> FindAsync(string catalogKey, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(catalogKey) || !catalogKey.StartsWith("service:", StringComparison.OrdinalIgnoreCase))
                return null;
            return await catalog.GetEntryAsync(catalogKey, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Public service capability lookup failed; key omitted.");
            throw;
        }
    }
    /// <summary>
    /// Sets exposure as part of the public service method directory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<PublicServiceExposureResult> SetExposureAsync(PublicServiceExposureRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Service);
            // Exact service identity, not a broad text contains query. Never alter another service family's policy.
            var entries = (await catalog.GetEntriesAsync(cancellationToken).ConfigureAwait(false))
                .Where(entry => entry.CatalogKey.StartsWith("service:", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(entry.DisplayName.Split('.')[0], request.Service, StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.CatalogKey, StringComparer.Ordinal)
                .ToArray();
            if (entries.Length == 0) throw new KeyNotFoundException("The specified service is not registered in the public method catalog.");
            var changed = 0;
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry.ExposeToAiChat == request.ExposeToAiChat &&
                    entry.ExposeToOneWire == request.ExposeToOneWire &&
                    entry.AllowRemoteInvocation == request.AllowRemoteInvocation)
                    continue;
                await catalog.SavePolicyAsync(new DxAiFunctionCatalogSaveRequest
                {
                    CatalogKey = entry.CatalogKey,
                    IsEnabled = entry.IsEnabled,
                    ExposeToAiChat = request.ExposeToAiChat,
                    ExposeToOneWire = request.ExposeToOneWire,
                    AllowRemoteInvocation = request.AllowRemoteInvocation,
                    RequiresFrontendConfirmation = entry.RequiresFrontendConfirmation,
                    InteractionEditor = entry.InteractionEditor,
                    AllowedPeerIdsJson = entry.AllowedPeerIdsJson,
                    UpdatedBy = "Local service operator"
                }, cancellationToken).ConfigureAwait(false);
                changed++;
            }
            logger.LogInformation("Changed exposure for {Updated}/{Matched} public service methods in the approved business-purpose group {Service}.", changed, entries.Length, request.Service);
            return new PublicServiceExposureResult { Service = request.Service, Matched = entries.Length, Updated = changed };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Changing approved public service exposure failed; no request payload logged.");
            throw;
        }
    }

}
