using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;

namespace LocalGPT.Interfaces;

/// <summary>
/// Represents an initial variable.
/// </summary>
public sealed record InitialVariable(string Name, string Value, string DataType);

/// <summary>
/// Defines the initial data catalog contract.
/// </summary>
public interface IInitialDataCatalog
{
    IReadOnlyList<RegexPatternDto> RegexPatterns { get; }
    IReadOnlyList<PromptConfigDto> Prompts { get; }
    IReadOnlyList<InitialVariable> Variables { get; }
    /// <summary>
    /// Loads knowledge async.
    /// </summary>
    Task<IReadOnlyList<CouncilKnowledgeEntry>> LoadKnowledgeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the database initialization service contract.
/// </summary>
public interface IDatabaseInitializationService
{
    /// <summary>
    /// Runs the initialize async operation.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
