using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;

namespace LocalGPT.Interfaces;

/// <summary>
/// Represents an initial variable application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Name">Name value supplied to the initial variable operation and used when producing its result.</param>
/// <param name="Value">Value value supplied to the initial variable operation and used when producing its result.</param>
/// <param name="DataType">Data type value supplied to the initial variable operation and used when producing its result.</param>
public sealed record InitialVariable(string Name, string Value, string DataType);

/// <summary>
/// Defines the contract for initial data behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IInitialDataCatalog
{
    /// <summary>
    /// Gets the regex patterns collection maintained or exposed by this initial data instance for downstream processing.
    /// </summary>
    /// <value>The regex patterns value exposed by <see cref="IInitialDataCatalog"/>.</value>
    IReadOnlyList<RegexPatternDto> RegexPatterns { get; }
    /// <summary>
    /// Gets the prompts collection maintained or exposed by this initial data instance for downstream processing.
    /// </summary>
    /// <value>The prompts value exposed by <see cref="IInitialDataCatalog"/>.</value>
    IReadOnlyList<PromptConfigDto> Prompts { get; }
    /// <summary>
    /// Gets the variables collection maintained or exposed by this initial data instance for downstream processing.
    /// </summary>
    /// <value>The variables value exposed by <see cref="IInitialDataCatalog"/>.</value>
    IReadOnlyList<InitialVariable> Variables { get; }
    /// <summary>
    /// Loads knowledge in the initial data directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<CouncilKnowledgeEntry>> LoadKnowledgeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for database initialization behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IDatabaseInitializationService
{
    /// <summary>
    /// Performs initialize as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
