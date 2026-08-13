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
            /// <summary>
            /// Runs the throw if null operation.
            /// </summary>
            ArgumentNullException.ThrowIfNull(services);
            var decorated = 0;
            /// <summary>
            /// Runs the to array operation.
            /// </summary>
            var descriptors = services.ToArray();

            foreach (var descriptor in descriptors)
            {
                if (!ShouldDecorate(descriptor))
                    continue;

                /// <summary>
                /// Runs the index of operation.
                /// </summary>
                var index = services.IndexOf(descriptor);
                if (index < 0)
                    continue;

                /// <summary>
                /// Runs the describe operation.
                /// </summary>
                var replacement = ServiceDescriptor.Describe(
                    descriptor.ServiceType,
                    provider => CreateProxy(provider, descriptor, isDevelopment),
                    descriptor.Lifetime);
                services[index] = replacement;
                decorated++;
            }

            /// <summary>
            /// Runs the log information operation.
            /// </summary>
            logger.LogInformation(
                /// <summary>
                /// Runs the registration operation.
                /// </summary>
                "Enabled bounded method-level diagnostics for {ServiceDescriptorCount} scoped/transient LocalGPT interface service registration(s); singleton, disposable, high-frequency and ThemeService registrations were excluded.",
                decorated);
        }
        catch (Exception exception)
        {
            /// <summary>
            /// Runs the log error operation.
            /// </summary>
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
            /// <summary>
            /// Runs the log error operation.
            /// </summary>
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
                /// <summary>
                /// Runs the typeof operation.
                /// </summary>
                serviceType == typeof(IChatProtocolProfile);
        }
        catch (Exception exception)
        {
            /// <summary>
            /// Runs the log error operation.
            /// </summary>
            logger.LogError(exception, "Evaluating a high-frequency service exclusion failed for {ServiceType}.", serviceType.FullName);
            throw;
        }
    }

    /// <summary>
    /// Creates proxy for <see cref="ServiceMethodDiagnosticsRegistration"/>, keeping the operation consistent with the state and invariants of the surrounding service method diagnostics registration workflow.
    /// </summary>
    /// <param name="provider">Service provider dependency used by the service method diagnostics registration workflow to provide the corresponding application capability.</param>
    /// <param name="descriptor">Descriptor value supplied to the service method diagnostics registration operation and used when producing its result.</param>
    /// <param name="isDevelopment">Value indicating whether is development should apply to this operation.</param>
    /// <returns>The object produced by the operation.</returns>
    private object CreateProxy(IServiceProvider provider, ServiceDescriptor descriptor, bool isDevelopment)
    {
        try
        {
            // Singleton and disposable registrations are excluded above. The provider passed here therefore
            // represents the active scoped/transient resolution boundary, never the root singleton provider.
            var target = descriptor.ImplementationInstance
                ?? descriptor.ImplementationFactory?.Invoke(provider)
                ?? ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType
                    /// <summary>
                    /// Runs the invalid operation exception operation.
                    /// </summary>
                    ?? throw new InvalidOperationException($"Service descriptor {descriptor.ServiceType} has no implementation."));

            if (target is IDisposable or IAsyncDisposable)
            {
                /// <summary>
                /// Runs the log debug operation.
                /// </summary>
                logger.LogDebug(
                    "Skipped method-diagnostics proxy creation for disposable implementation {ServiceImplementationType}; DI retains disposal ownership.",
                    /// <summary>
                    /// Gets type.
                    /// </summary>
                    target.GetType().FullName);
                return target;
            }

            /// <summary>
            /// Runs the create operation.
            /// </summary>
            var proxy = DispatchProxy.Create(descriptor.ServiceType, typeof(ServiceMethodLoggingDispatchProxy));
            /// <summary>
            /// Runs the initialize operation.
            /// </summary>
            ((ServiceMethodLoggingDispatchProxy)proxy).Initialize(
                target,
                provider.GetRequiredService<ILoggerFactory>(),
                isDevelopment);
            return proxy;
        }
        catch (Exception exception)
        {
            /// <summary>
            /// Runs the log error operation.
            /// </summary>
            logger.LogError(
                exception,
                "Creating a bounded method-diagnostics proxy failed for {ServiceType}; no service arguments were logged.",
                descriptor.ServiceType.FullName);
            throw;
        }
    }
}
