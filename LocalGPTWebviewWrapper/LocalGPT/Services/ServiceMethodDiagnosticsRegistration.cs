using System.Reflection;
using LocalGPT.Diagnostics;
using LocalGPT.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LocalGPT.Services;

/// <summary>
/// Decorates scoped and transient LocalGPT interface services with bounded method-level operational logging.
/// Singleton services remain DI-owned and use their explicit service logging so the decorator can never
/// resolve a scoped dependency from the root provider.
/// </summary>
public sealed class ServiceMethodDiagnosticsRegistration(ILogger logger)
{
    public void Apply(IServiceCollection services, bool isDevelopment)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(services);
            var decorated = 0;
            var descriptors = services.ToArray();

            foreach (var descriptor in descriptors)
            {
                if (!ShouldDecorate(descriptor))
                    continue;

                var index = services.IndexOf(descriptor);
                if (index < 0)
                    continue;

                var replacement = ServiceDescriptor.Describe(
                    descriptor.ServiceType,
                    provider => CreateProxy(provider, descriptor, isDevelopment),
                    descriptor.Lifetime);
                services[index] = replacement;
                decorated++;
            }

            logger.LogInformation(
                "Enabled bounded method-level diagnostics for {ServiceDescriptorCount} scoped/transient LocalGPT interface service registration(s); singleton, disposable, high-frequency and ThemeService registrations were excluded.",
                decorated);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Registering bounded service-method diagnostics failed.");
            throw;
        }
    }

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
                serviceType == typeof(IDxAiFunctionHandler))
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

    private object CreateProxy(IServiceProvider provider, ServiceDescriptor descriptor, bool isDevelopment)
    {
        try
        {
            // Singleton and disposable registrations are excluded above. The provider passed here therefore
            // represents the active scoped/transient resolution boundary, never the root singleton provider.
            var target = descriptor.ImplementationInstance
                ?? descriptor.ImplementationFactory?.Invoke(provider)
                ?? ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType
                    ?? throw new InvalidOperationException($"Service descriptor {descriptor.ServiceType} has no implementation."));

            if (target is IDisposable or IAsyncDisposable)
            {
                logger.LogDebug(
                    "Skipped method-diagnostics proxy creation for disposable implementation {ServiceImplementationType}; DI retains disposal ownership.",
                    target.GetType().FullName);
                return target;
            }

            var proxy = DispatchProxy.Create(descriptor.ServiceType, typeof(ServiceMethodLoggingDispatchProxy));
            ((ServiceMethodLoggingDispatchProxy)proxy).Initialize(
                target,
                provider.GetRequiredService<ILoggerFactory>(),
                isDevelopment);
            return proxy;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Creating a bounded method-diagnostics proxy failed for {ServiceType}; no service arguments were logged.",
                descriptor.ServiceType.FullName);
            throw;
        }
    }
}
