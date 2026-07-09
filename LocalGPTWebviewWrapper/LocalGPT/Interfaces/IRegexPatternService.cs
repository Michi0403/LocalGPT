using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;
using System.Text.RegularExpressions;

public interface IRegexPatternService
{
    Task AddOrUpdateAsync(RegexPatternDto dto);

    Task<Regex> GetRegexAsync(string name);

    Task<List<RegexPattern>> ListAllAsync();

    Task DeleteAsync(string name);
}