namespace LocalGPT.BusinessObjects;

internal sealed class BrowserThemeState
{
    public string? ShellThemeName { get; set; }
    public string? ComponentThemeName { get; set; }
    public List<BrowserThemeFusionStep>? FusionRoute { get; set; }
}

internal sealed class BrowserThemeFusionStep
{
    public int Sequence { get; set; }
    public string? Target { get; set; }
    public string? ThemeName { get; set; }
}

internal sealed class LoggerNullScope : IDisposable
{
    public void Dispose()
    {
    }
}
