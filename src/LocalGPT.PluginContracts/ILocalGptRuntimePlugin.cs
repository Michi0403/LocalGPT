using System.Text.Json;

namespace LocalGPT.PluginContracts;

/// <summary>Stable host contract implemented by trusted LocalGPT runtime plugins.</summary>
public interface ILocalGptRuntimePlugin
{
    /// <summary>Executes the plugin against validated function parameters.</summary>
    /// <param name="parameters">Validated invocation parameters supplied by LocalGPT.</param>
    /// <param name="cancellationToken">Cancellation token for the invocation.</param>
    /// <returns>JSON or text returned to the LocalGPT function caller.</returns>
    Task<string> ExecuteAsync(JsonElement parameters, CancellationToken cancellationToken = default);
}
