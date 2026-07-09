using LocalGPT.BusinessObjects;
using System.Text.RegularExpressions;

public interface IRegexPatternService
{
    Task AddOrUpdateAsync(string name, string pattern, string? flags = null);

    Task<Regex> GetRegexAsync(string name);

    Task<List<RegexPattern>> ListAllAsync();

    Task DeleteAsync(string name);
}