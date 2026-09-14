namespace LocalGPT.BusinessObjects;

/// <summary>Requests a full or cropped readback of the authoritative Council ASCII display.</summary>
public sealed class ReadCouncilGameDisplayRequest
{
    /// <summary>Gets or sets the game session identifier.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Gets or sets the left display-cell coordinate.</summary>
    public int X { get; set; }
    /// <summary>Gets or sets the top display-cell coordinate.</summary>
    public int Y { get; set; }
    /// <summary>Gets or sets the requested width; zero means through the right edge.</summary>
    public int Width { get; set; }
    /// <summary>Gets or sets the requested height; zero means through the bottom edge.</summary>
    public int Height { get; set; }
}

/// <summary>Requests bounded text output on the authoritative Council ASCII display.</summary>
public sealed class WriteCouncilGameTextRequest
{
    /// <summary>Gets or sets the game session identifier.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Gets or sets the authoritative turn the renderer expects.</summary>
    public long Turn { get; set; }
    /// <summary>Gets or sets the renderer that owns display mutations for this turn.</summary>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>Gets or sets the left display-cell coordinate.</summary>
    public int X { get; set; }
    /// <summary>Gets or sets the top display-cell coordinate.</summary>
    public int Y { get; set; }
    /// <summary>Gets or sets text to write; line breaks advance display rows.</summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>Requests one display-cell mutation.</summary>
public sealed class SetCouncilGameCellRequest
{
    /// <summary>Gets or sets the game session identifier.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Gets or sets the authoritative turn the renderer expects.</summary>
    public long Turn { get; set; }
    /// <summary>Gets or sets the renderer that owns display mutations for this turn.</summary>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>Gets or sets the horizontal cell coordinate.</summary>
    public int X { get; set; }
    /// <summary>Gets or sets the vertical cell coordinate.</summary>
    public int Y { get; set; }
    /// <summary>Gets or sets the glyph; the first Unicode character is used.</summary>
    public string Glyph { get; set; } = " ";
}

/// <summary>Requests a rectangular display region to be filled with one glyph.</summary>
public sealed class FillCouncilGameRegionRequest
{
    /// <summary>Gets or sets the game session identifier.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Gets or sets the authoritative turn the renderer expects.</summary>
    public long Turn { get; set; }
    /// <summary>Gets or sets the renderer that owns display mutations for this turn.</summary>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>Gets or sets the left display-cell coordinate.</summary>
    public int X { get; set; }
    /// <summary>Gets or sets the top display-cell coordinate.</summary>
    public int Y { get; set; }
    /// <summary>Gets or sets the region width.</summary>
    public int Width { get; set; }
    /// <summary>Gets or sets the region height.</summary>
    public int Height { get; set; }
    /// <summary>Gets or sets the fill glyph.</summary>
    public string Glyph { get; set; } = " ";
}

/// <summary>Requests a multiline ASCII block to be blitted into a display region.</summary>
public sealed class BlitCouncilGameRegionRequest
{
    /// <summary>Gets or sets the game session identifier.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Gets or sets the authoritative turn the renderer expects.</summary>
    public long Turn { get; set; }
    /// <summary>Gets or sets the renderer that owns display mutations for this turn.</summary>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>Gets or sets the left display-cell coordinate.</summary>
    public int X { get; set; }
    /// <summary>Gets or sets the top display-cell coordinate.</summary>
    public int Y { get; set; }
    /// <summary>Gets or sets multiline ASCII content. Content is clipped to the display.</summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>Requests a pregenerated local ASCII animation for one Council display turn.</summary>
public sealed class SubmitCouncilGameAnimationRequest
{
    /// <summary>Gets or sets the game session identifier.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Gets or sets the authoritative turn the renderer expects.</summary>
    public long Turn { get; set; }
    /// <summary>Gets or sets the renderer that owns the animation frames.</summary>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>Gets or sets two through twelve complete frames generated before playback.</summary>
    public IReadOnlyList<string> Frames { get; set; } = [];
    /// <summary>Gets or sets client-side frame delay in milliseconds.</summary>
    public int DelayMilliseconds { get; set; } = 650;
    /// <summary>Gets or sets an optional stable transcript/display caption.</summary>
    public string Caption { get; set; } = string.Empty;
}

/// <summary>Returns a full or cropped read-only snapshot of the Council ASCII display.</summary>
public sealed class CouncilGameDisplaySnapshot
{
    /// <summary>Gets or sets the game session identifier.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Gets or sets the authoritative turn.</summary>
    public long Turn { get; set; }
    /// <summary>Gets or sets the complete display width in terminal cells.</summary>
    public int FrameWidth { get; set; }
    /// <summary>Gets or sets the complete display height in terminal cells.</summary>
    public int FrameHeight { get; set; }
    /// <summary>Gets or sets the left coordinate represented by <see cref="Text"/>.</summary>
    public int X { get; set; }
    /// <summary>Gets or sets the top coordinate represented by <see cref="Text"/>.</summary>
    public int Y { get; set; }
    /// <summary>Gets or sets the returned region width.</summary>
    public int Width { get; set; }
    /// <summary>Gets or sets the returned region height.</summary>
    public int Height { get; set; }
    /// <summary>Gets or sets the fixed-cell display text.</summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>Gets or sets the current frame renderer.</summary>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>Gets or sets supported read/write display functions for AI discovery.</summary>
    public IReadOnlyList<string> SupportedMethods { get; set; } = [];
}
