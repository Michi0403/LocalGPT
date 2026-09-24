using LocalGPT.PluginContracts;
using System.Reflection;
using System.Runtime.Loader;

namespace LocalGPT.Runtime.Plugins;

/// <summary>Isolated load context for one trusted compiled runtime plugin.</summary>
internal sealed class RuntimePluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;
    private readonly string ContractAssemblyName = typeof(ILocalGptRuntimePlugin).Assembly.GetName().Name ?? "LocalGPT.PluginContracts";

    /// <summary>Creates a collectible plugin load context for the supplied entry assembly.</summary>
    /// <param name="pluginPath">Absolute plugin entry assembly path.</param>
    public RuntimePluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        resolver = new AssemblyDependencyResolver(pluginPath);
    }

    /// <summary>Resolves managed plugin dependencies while keeping the shared contract in the host context.</summary>
    /// <param name="assemblyName">Assembly requested by the plugin.</param>
    /// <returns>The resolved assembly, or <see langword="null"/> to fall back to the default load context.</returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (string.Equals(assemblyName.Name, ContractAssemblyName, StringComparison.OrdinalIgnoreCase))
            return null;
        var path = resolver.ResolveAssemblyToPath(assemblyName);
        return string.IsNullOrWhiteSpace(path) ? null : LoadFromAssemblyPath(path);
    }

    /// <summary>Resolves unmanaged plugin dependencies relative to the plugin package.</summary>
    /// <param name="unmanagedDllName">Native library name requested by the plugin.</param>
    /// <returns>A native library handle, or zero when the default resolver should continue.</returns>
    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return string.IsNullOrWhiteSpace(path) ? nint.Zero : LoadUnmanagedDllFromPath(path);
    }
}
