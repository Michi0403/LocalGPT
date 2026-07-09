using System.Text.RegularExpressions;

namespace LocalGPT.Extensions.PlainStatics
{
    public class RegExStatics
    {
        public static RegexOptions? ParseFlags(string? flags, ILogger logger)
        {
            try
            {
                var validFlags = "i".ToCharArray().Select(f => (RegexOptions)Enum.Parse(typeof(RegexOptions), f.ToString()));

                if (!string.IsNullOrEmpty(flags))
                {
                    foreach (var flag in flags.Split('|'))
                    {
                        // Add validation for each regex option
                    }
                }

                return RegexOptions.None;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ParseFlags flags {flags.ToString()} ex {ex.ToString()}");
                return null;
            }
        
        }
    }
}
