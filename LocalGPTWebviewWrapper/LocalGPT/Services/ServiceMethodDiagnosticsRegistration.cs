using System.Reflection;
using LocalGPT.Diagnostics;
using LocalGPT.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LocalGPT.Services;

/// <summary>
/// Decorates LocalGPT interface services with bounded method-level operational logging.
/// ThemeService remains deliberately untouched because its compatibility constructor is concrete and UI-owned.
/// </summary>
public sealed class ServiceMethodDiagnosticsRegistration(ILogger logger)
{
    public void Apply(IServiceCollection services, bool isDevelopment)
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
            "Enabled bounded method-level diagnostics for {ServiceDescriptorCount} LocalGPT interface service registration(s); ThemeService was excluded.",
            decorated);
    }

    private bool ShouldDecorate(ServiceDescriptor descriptor)
    {
        var serviceType = descriptor.ServiceType;
        if (descriptor.IsKeyedService || serviceType.ContainsGenericParameters)
            return false;
        if (!serviceType.IsInterface || serviceType.Namespace?.StartsWith("LocalGPT.Interfaces", StringComparison.Ordinal) != true)
            return false;
        if (serviceType.GetMethods().Any(method => method.ReturnType.IsByRef || method.GetParameters().Any(parameter => parameter.ParameterType.IsByRef)))
            return false;
        if (serviceType == typeof(IServiceActivityService) || serviceType == typeof(IComponentActivityService))
            return false;
        if (serviceType.Name.Contains("Theme", StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    private object CreateProxy(IServiceProvider provider, ServiceDescriptor descriptor, bool isDevelopment)
    {
        // Implementation-type targets are created exclusively for the proxy. Factory registrations in
        // this application are aliases to other DI-owned services, so the proxy must not dispose them.
        var ownsTarget = descriptor.ImplementationType is not null;
        var target = descriptor.ImplementationInstance
            ?? descriptor.ImplementationFactory?.Invoke(provider)
            ?? ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType
                ?? throw new InvalidOperationException($"Service descriptor {descriptor.ServiceType} has no implementation."));

        var proxy = DispatchProxy.Create(descriptor.ServiceType, typeof(ServiceMethodLoggingDispatchProxy));
        ((ServiceMethodLoggingDispatchProxy)proxy).Initialize(
            target,
            provider.GetRequiredService<ILoggerFactory>(),
            isDevelopment,
            ownsTarget);
        return proxy;
    }
}
