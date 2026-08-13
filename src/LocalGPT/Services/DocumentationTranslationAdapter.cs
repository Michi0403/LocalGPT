using System.Globalization;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.Localization;

namespace LocalGPT.Services;

/// <summary>
/// Bridges compiler-generated XML comments to the existing LocalGPT localization service without changing that service's contract.
/// </summary>
/// <param name="localization">Local gpt localization service dependency used by the documentation translation adapter workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[DocumentationUpdated("2.1.20")]
public sealed class DocumentationTranslationAdapter(
    ILocalGptLocalizationService localization,
    ILogger<DocumentationTranslationAdapter> logger) : IDocumentationTranslationAdapter
{
    /// <summary>
    /// Performs adapt for <see cref="DocumentationTranslationAdapter"/>, keeping the operation consistent with the state and invariants of the surrounding documentation translation adapter workflow.
    /// </summary>
    /// <inheritdoc />
    public LocalGptDocumentationComment Adapt(LocalGptDocumentationComment comment, string? culture = null)
    {
        var memberId = comment?.MemberId;
        try
        {
            ArgumentNullException.ThrowIfNull(comment);
            var normalizedCulture = NormalizeCulture(culture);
            var key = BuildLocalizationKey(comment.MemberId);
            return new LocalGptDocumentationComment
            {
                MemberId = comment.MemberId,
                DisplayName = localization.Get($"{key}.DisplayName", normalizedCulture, comment.DisplayName),
                Summary = localization.Get($"{key}.Summary", normalizedCulture, comment.Summary),
                Remarks = localization.Get($"{key}.Remarks", normalizedCulture, comment.Remarks),
                Culture = normalizedCulture,
                LastUpdatedVersion = comment.LastUpdatedVersion,
                CurrentVersion = comment.CurrentVersion,
                IsCurrent = comment.IsCurrent
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Adapting XML documentation member {DocumentationMemberId} failed.", memberId);
            throw;
        }
    }

    /// <summary>
    /// Builds localization key for <see cref="DocumentationTranslationAdapter"/>, keeping the operation consistent with the state and invariants of the surrounding documentation translation adapter workflow.
    /// </summary>
    /// <param name="memberId">Identifier of the member to use for this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildLocalizationKey(string memberId)
    {
    try
    {
            var builder = new StringBuilder("Documentation.");
            foreach (var character in memberId)
                builder.Append(char.IsLetterOrDigit(character) ? character : '_');
            return builder.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationTranslationAdapter)}.{nameof(BuildLocalizationKey)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationTranslationAdapter)}.{nameof(BuildLocalizationKey)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes culture for <see cref="DocumentationTranslationAdapter"/>, keeping the operation consistent with the state and invariants of the surrounding documentation translation adapter workflow.
    /// </summary>
    /// <param name="culture">Culture value supplied to the documentation translation adapter operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeCulture(string? culture)
    {
    try
    {
            var requested = string.IsNullOrWhiteSpace(culture) ? CultureInfo.CurrentUICulture.Name : culture.Trim();
            try
            {
                return CultureInfo.GetCultureInfo(requested).Name;
            }
            catch (CultureNotFoundException)
            {
                return "en-US";
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationTranslationAdapter)}.{nameof(NormalizeCulture)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationTranslationAdapter)}.{nameof(NormalizeCulture)} failed.");
        throw;
    }
}
}
