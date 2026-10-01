using LocalGPT.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LocalGPT.Services;

/// <summary>
/// Reviews scoped and transient LocalGPT interface-service registrations for bounded diagnostics while
/// preserving their original DI descriptors. Application services own their method-level diagnostics; this
/// registration pass must not change scoped graph identity, factory ownership, lifetime, or disposal.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ServiceMethodDiagnosticsRegistration(ILogger logger)
{
    /// <summary>
    /// Performs apply for <see cref="ServiceMethodDiagnosticsRegistration"/>, keeping the operation consistent with the state and invariants of the surrounding service method diagnostics registration workflow.
    /// </summary>
    /// <param name="services">Service collection dependency used by the service method diagnostics registration workflow to provide the corresponding application capability.</param>
    /// <param name="isDevelopment">Value indicating whether is development should apply to this operation.</param>
    public void Apply(IServiceCollection services, bool isDevelopment)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(services);
            var eligible = 0;
            var descriptors = services.ToArray();

            foreach (var descriptor in descriptors)
            {
                if (ShouldDecorate(descriptor))
                    eligible++;
            }

            // LocalGPT services already own method-local diagnostics. Replacing their registrations with
            // DispatchProxy factories changes the DI call-site graph and can recursively re-enter
            // ActivatorUtilities while a scoped graph is being materialized. On .NET 10 this surfaced as
            // duplicate ServiceCacheKey failures during component injection and made routed UI unusable.
            // Keep the registrations exactly as authored; the review count remains useful diagnostics
            // without altering lifetime, identity, factory ownership, or disposal behavior.
            logger.LogInformation(
                "Reviewed {ServiceDescriptorCount} scoped/transient LocalGPT interface service registration(s) for bounded diagnostics in developmentMode={DevelopmentMode}; registrations were preserved unchanged because service-owned method diagnostics are authoritative.",
                eligible,
                isDevelopment);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Registering bounded service-method diagnostics failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs should decorate for <see cref="ServiceMethodDiagnosticsRegistration"/>, keeping the operation consistent with the state and invariants of the surrounding service method diagnostics registration workflow.
    /// </summary>
    /// <param name="descriptor">Descriptor value supplied to the service method diagnostics registration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool ShouldDecorate(ServiceDescriptor descriptor)
    {
        try
        {
            var serviceType = descriptor.ServiceType;
            if (descriptor.IsKeyedService || serviceType.ContainsGenericParameters)
                return false;
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
                return false;
            if (typeof(IDisposable).IsAssignableFrom(serviceType) || typeof(IAsyncDisposable).IsAssignableFrom(serviceType))
                return false;
            if (!serviceType.IsInterface || serviceType.Namespace?.StartsWith("LocalGPT.Interfaces", StringComparison.Ordinal) != true)
                return false;
            if (serviceType.GetMethods().Any(method => method.ReturnType.IsByRef || method.GetParameters().Any(parameter => parameter.ParameterType.IsByRef)))
                return false;
            if (serviceType == typeof(IServiceActivityService) ||
                serviceType == typeof(IComponentActivityService) ||
                serviceType == typeof(IDxAiFunctionHandler) ||
                serviceType == typeof(IDxAiFunctionRegistry))
            {
                return false;
            }
            if (IsHighFrequencyReadService(serviceType))
                return false;
            if (serviceType.Name.Contains("Theme", StringComparison.OrdinalIgnoreCase))
                return false;
            if (descriptor.ImplementationType is not null &&
                (typeof(IDisposable).IsAssignableFrom(descriptor.ImplementationType) ||
                 typeof(IAsyncDisposable).IsAssignableFrom(descriptor.ImplementationType)))
            {
                return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Evaluating a service registration for method diagnostics failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether high frequency read service for <see cref="ServiceMethodDiagnosticsRegistration"/>, keeping the operation consistent with the state and invariants of the surrounding service method diagnostics registration workflow.
    /// </summary>
    /// <param name="serviceType">Service type value supplied to the service method diagnostics registration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsHighFrequencyReadService(Type serviceType)
    {
        try
        {
            return serviceType == typeof(ILocalGptRuntimePolicyDataService) ||
                serviceType == typeof(IStructuredTextTranslationService) ||
                serviceType == typeof(ICouncilLiveSessionService) ||
                serviceType == typeof(IDxAiFunctionJsonService) ||
                serviceType == typeof(IChatContentRenderer) ||
                serviceType == typeof(IChatResponseFormatter) ||
                serviceType == typeof(IChatResponseFormatterFactory) ||
                serviceType == typeof(IChatProtocolResolver) ||
                serviceType == typeof(IChatProtocolProfileCatalog) ||
                serviceType == typeof(IChatProtocolTextService) ||
                serviceType == typeof(IChatProtocolProfile);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Evaluating a high-frequency service exclusion failed for {ServiceType}.", serviceType.FullName);
            throw;
        }
    }

}
