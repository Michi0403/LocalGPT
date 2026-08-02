using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;
using System.Text.RegularExpressions;

public interface IRegexPatternService
{
    Task AddOrUpdateAsync(RegexPatternDto dto);

    Task<Regex?> GetRegexAsync(string name);

    Regex Compile(string pattern, string? flags = null);

    Task<List<RegexPattern>> ListAllAsync();

    Task<List<RegexPattern>> ListAllAsync(int? take);

    Task DeleteAsync(string name);
}
public interface IRegexFunctionParameterService
{
    string GetRequiredString(System.Text.Json.JsonElement element, string propertyName);
}
