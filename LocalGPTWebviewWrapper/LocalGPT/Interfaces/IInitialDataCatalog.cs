using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;

namespace LocalGPT.Interfaces;

public sealed record InitialVariable(string Name, string Value, string DataType);

public interface IInitialDataCatalog
{
    IReadOnlyList<RegexPatternDto> RegexPatterns { get; }
    IReadOnlyList<PromptConfigDto> Prompts { get; }
    IReadOnlyList<InitialVariable> Variables { get; }
    Task<IReadOnlyList<CouncilKnowledgeEntry>> LoadKnowledgeAsync(CancellationToken cancellationToken = default);
}

public interface IDatabaseInitializationService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
