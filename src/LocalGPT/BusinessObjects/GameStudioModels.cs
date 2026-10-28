using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Chooses a presentation adapter, never a game-mechanics implementation or a team identity.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameSceneArtworkMode
{
    /// <summary>Existing single-character actor and tile display; compatible with legacy/1970s-style art.</summary>
    ClassicGlyph,
    /// <summary>Preserves multi-line authored character art and explicitly positioned text canvases.</summary>
    AuthoredAscii,
    /// <summary>Uses authored character art and color metadata; the renderer determines available ANSI capabilities.</summary>
    AuthoredAnsi,
    /// <summary>Uses authored palette-based pixel sprites with the same simulation state.</summary>
    AuthoredPixel
}

/// <summary>One authored text canvas. Multiple canvases form conditional, layered screens without code-generated scenes.</summary>
public sealed class GameSceneTextCanvas
{
    /// <summary>Stable project-local canvas name for revisions and proposals.</summary>
    /// <value>Canvas identity.</value>
    public string Id { get; set; } = string.Empty;
    /// <summary>Screen-space left position.</summary>
    /// <value>Horizontal offset in terminal cells.</value>
    public int X { get; set; }
    /// <summary>Screen-space top position.</summary>
    /// <value>Vertical offset in terminal cells.</value>
    public int Y { get; set; }
    /// <summary>Authored rows, preserved literally with spaces and character shading.</summary>
    /// <value>Canvas rows; never synthesized by the renderer.</value>
    public List<string> Rows { get; set; } = [];
    /// <summary>Optional scene-state variable gating this canvas.</summary>
    /// <value>Empty makes the canvas unconditional.</value>
    public string VisibleWhenVariable { get; set; } = string.Empty;
    /// <summary>Required value of the visibility variable.</summary>
    /// <value>Literal state value, compared ordinally.</value>
    public string VisibleWhenValue { get; set; } = string.Empty;
    /// <summary>Earliest world time at which the canvas can appear.</summary>
    /// <value>World time in seconds.</value>
    public double FromWorldSecond { get; set; }
    /// <summary>Optional expiration time; zero leaves the canvas active.</summary>
    /// <value>World time in seconds or zero.</value>
    public double UntilWorldSecond { get; set; }
}

/// <summary>Binds an existing database regex name to a bounded scene-state update, without defining a second regex catalog.</summary>
public sealed class GameSceneRegexTransition
{
    /// <summary>Existing name resolved through the application's regex catalog and compiler.</summary>
    /// <value>Catalog regex identity.</value>
    public string RegexPatternName { get; set; } = string.Empty;
    /// <summary>Presentation state variable to update after a successful match.</summary>
    /// <value>Project-owned state key used in authored placeholders and canvas visibility.</value>
    public string Variable { get; set; } = string.Empty;
    /// <summary>Optional named/numbered capture group; blank uses the configured literal value.</summary>
    /// <value>Regex capture group name.</value>
    public string CaptureGroup { get; set; } = string.Empty;
    /// <summary>Value used when no capture group is specified.</summary>
    /// <value>Bounded literal scene-state value.</value>
    public string ValueOnMatch { get; set; } = string.Empty;
}


/// <summary>Editable, project-owned two-dimensional world description. No game's scenery, art or rules are compiled into the engine.</summary>
public sealed class GameSceneDocument
{
    /// <summary>
    /// Gets or sets the scene name value that forms part of the game scene state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scene name value exposed by <see cref="GameSceneDocument"/>.</value>
    public string SceneName { get; set; } = "New world";
    /// <summary>
    /// Gets or sets the world width value that forms part of the game scene state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The world width value exposed by <see cref="GameSceneDocument"/>.</value>
    public int WorldWidth { get; set; } = 64;
    /// <summary>
    /// Gets or sets the world height value that forms part of the game scene state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The world height value exposed by <see cref="GameSceneDocument"/>.</value>
    public int WorldHeight { get; set; } = 48;
    /// <summary>
    /// Gets or sets the view width value that forms part of the game scene state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The view width value exposed by <see cref="GameSceneDocument"/>.</value>
    public int ViewWidth { get; set; } = 32;
    /// <summary>
    /// Gets or sets the view height value that forms part of the game scene state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The view height value exposed by <see cref="GameSceneDocument"/>.</value>
    public int ViewHeight { get; set; } = 18;
    /// <summary>
    /// Gets or sets the chunk size that quantifies the associated game scene data.
    /// </summary>
    /// <value>The chunk size value exposed by <see cref="GameSceneDocument"/>.</value>
    public int ChunkSize { get; set; } = 16;
    /// <summary>
    /// Gets or sets the fixed ticks per second value that forms part of the game scene state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fixed ticks per second value exposed by <see cref="GameSceneDocument"/>.</value>
    public int FixedTicksPerSecond { get; set; } = 60;
    /// <summary>
    /// Gets or sets the world seconds per real second value that forms part of the game scene state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The world seconds per real second value exposed by <see cref="GameSceneDocument"/>.</value>
    public double WorldSecondsPerRealSecond { get; set; } = 1;
    /// <summary>
    /// Gets or sets the seconds per turn value that forms part of the game scene state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The seconds per turn value exposed by <see cref="GameSceneDocument"/>.</value>
    public double SecondsPerTurn { get; set; } = 1;
    /// <summary>
    /// Gets or sets the tiles collection maintained or exposed by this game scene instance for downstream processing.
    /// </summary>
    /// <value>The tiles value exposed by <see cref="GameSceneDocument"/>.</value>
    public List<GameSceneTile> Tiles { get; set; } = [];
    /// <summary>
    /// Gets or sets the sprites collection maintained or exposed by this game scene instance for downstream processing.
    /// </summary>
    /// <value>The sprites value exposed by <see cref="GameSceneDocument"/>.</value>
    public List<GameSceneSprite> Sprites { get; set; } = [];
    /// <summary>
    /// Gets or sets the actors collection maintained or exposed by this game scene instance for downstream processing.
    /// </summary>
    /// <value>The actors value exposed by <see cref="GameSceneDocument"/>.</value>
    public List<GameSceneActor> Actors { get; set; } = [];
    /// <summary>
    /// Gets or sets the layers collection maintained or exposed by this game scene instance for downstream processing.
    /// </summary>
    /// <value>The layers value exposed by <see cref="GameSceneDocument"/>.</value>
    public List<GameSceneLayer> Layers { get; set; } = [];
    /// <summary>
    /// Gets or sets the palette timeline collection maintained or exposed by this game scene instance for downstream processing.
    /// </summary>
    /// <value>The palette timeline value exposed by <see cref="GameSceneDocument"/>.</value>
    public List<GameScenePaletteKey> PaletteTimeline { get; set; } = [];
    /// <summary>
    /// Gets or sets the timeline collection maintained or exposed by this game scene instance for downstream processing.
    /// </summary>
    /// <value>The timeline value exposed by <see cref="GameSceneDocument"/>.</value>
    public List<GameSceneTimedEvent> Timeline { get; set; } = [];
    /// <summary>
    /// Gets or sets the animations collection maintained or exposed by this game scene instance for downstream processing.
    /// </summary>
    /// <value>The animations value exposed by <see cref="GameSceneDocument"/>.</value>
    public List<GameSceneAnimation> Animations { get; set; } = [];
    /// <summary>
    /// Gets or sets the hud collection maintained or exposed by this game scene instance for downstream processing.
    /// </summary>
    /// <value>The hud value exposed by <see cref="GameSceneDocument"/>.</value>
    public List<GameSceneHudElement> Hud { get; set; } = [];
    /// <summary>
    /// Gets or sets the art direction value that forms part of the game scene state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The art direction value exposed by <see cref="GameSceneDocument"/>.</value>
    public string ArtDirection { get; set; } = string.Empty;
    /// <summary>Selects whether the same game world is drawn as classic glyphs, author-created character art, ANSI, or pixel art.</summary>
    /// <value>Renderer-independent project-owned artwork preference.</value>
    public GameSceneArtworkMode ArtworkMode { get; set; } = GameSceneArtworkMode.ClassicGlyph;
    /// <summary>Authored multi-line backgrounds, decorative frames, and conditional complete screens.</summary>
    /// <value>Text canvases attached to this project revision.</value>
    public List<GameSceneTextCanvas> TextCanvases { get; set; } = [];
    /// <summary>Links approved regex catalog rules to declarative presentation-state transitions.</summary>
    /// <value>Ordered match-to-variable bindings used by both previews and approved game controls.</value>
    public List<GameSceneRegexTransition> RegexTransitions { get; set; } = [];
}

/// <summary>One cell in an open-world chunk, with semantic exclusion flags for object placement.</summary>
public sealed class GameSceneTile
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this game scene tile instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="GameSceneTile"/>.</value>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Gets or sets the x value that forms part of the game scene tile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="GameSceneTile"/>.</value>
    public int X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the game scene tile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="GameSceneTile"/>.</value>
    public int Y { get; set; }
    /// <summary>
    /// Gets or sets the glyph value that forms part of the game scene tile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The glyph value exposed by <see cref="GameSceneTile"/>.</value>
    public string Glyph { get; set; } = ".";
    /// <summary>
    /// Gets or sets the stable sprite identifier used to identify or correlate this game scene tile instance with related application state.
    /// </summary>
    /// <value>The sprite identifier value exposed by <see cref="GameSceneTile"/>.</value>
    public string SpriteId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the color value that forms part of the game scene tile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The color value exposed by <see cref="GameSceneTile"/>.</value>
    public string Color { get; set; } = "#87cefa";
    /// <summary>
    /// Gets or sets a value indicating whether solid applies to the game scene tile state.
    /// </summary>
    /// <value>The solid value exposed by <see cref="GameSceneTile"/>.</value>
    public bool Solid { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether blocks decoration applies to the game scene tile state.
    /// </summary>
    /// <value>The blocks decoration value exposed by <see cref="GameSceneTile"/>.</value>
    public bool BlocksDecoration { get; set; }
    /// <summary>
    /// Gets or sets the biome value that forms part of the game scene tile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The biome value exposed by <see cref="GameSceneTile"/>.</value>
    public string Biome { get; set; } = string.Empty;
}

/// <summary>AI-authored or hand-edited sprite/creature represented independently of its movement rules.</summary>
public sealed class GameSceneActor
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this game scene actor instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="GameSceneActor"/>.</value>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Gets or sets the kind value that forms part of the game scene actor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="GameSceneActor"/>.</value>
    public string Kind { get; set; } = "creature";
    /// <summary>
    /// Gets or sets the x value that forms part of the game scene actor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="GameSceneActor"/>.</value>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the game scene actor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="GameSceneActor"/>.</value>
    public double Y { get; set; }
    /// <summary>
    /// Gets or sets the velocity x value that forms part of the game scene actor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The velocity x value exposed by <see cref="GameSceneActor"/>.</value>
    public double VelocityX { get; set; }
    /// <summary>
    /// Gets or sets the velocity y value that forms part of the game scene actor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The velocity y value exposed by <see cref="GameSceneActor"/>.</value>
    public double VelocityY { get; set; }
    /// <summary>
    /// Gets or sets the glyph value that forms part of the game scene actor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The glyph value exposed by <see cref="GameSceneActor"/>.</value>
    public string Glyph { get; set; } = "@";
    /// <summary>
    /// Gets or sets the stable sprite identifier used to identify or correlate this game scene actor instance with related application state.
    /// </summary>
    /// <value>The sprite identifier value exposed by <see cref="GameSceneActor"/>.</value>
    public string SpriteId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the color value that forms part of the game scene actor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The color value exposed by <see cref="GameSceneActor"/>.</value>
    public string Color { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets the layer value that forms part of the game scene actor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The layer value exposed by <see cref="GameSceneActor"/>.</value>
    public int Layer { get; set; } = 10;
    /// <summary>
    /// Gets or sets a value indicating whether collides applies to the game scene actor state.
    /// </summary>
    /// <value>The collides value exposed by <see cref="GameSceneActor"/>.</value>
    public bool Collides { get; set; } = true;
    /// <summary>Marks the single authored actor moved by human/AI GameDirector controls; no fixed creature type is assumed.</summary>
    /// <value>The player controlled value exposed by <see cref="GameSceneActor"/>.</value>
    public bool PlayerControlled { get; set; }
    /// <summary>
    /// Gets or sets the artist model value that forms part of the game scene actor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The artist model value exposed by <see cref="GameSceneActor"/>.</value>
    public string ArtistModel { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the curator model value that forms part of the game scene actor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The curator model value exposed by <see cref="GameSceneActor"/>.</value>
    public string CuratorModel { get; set; } = string.Empty;
}

/// <summary>Just-in-time pixel art; each character indexes an authored palette, and blank cells remain transparent.</summary>
public sealed class GameSceneSprite
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this game scene sprite instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="GameSceneSprite"/>.</value>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Gets or sets the pixel rows collection maintained or exposed by this game scene sprite instance for downstream processing.
    /// </summary>
    /// <value>The pixel rows value exposed by <see cref="GameSceneSprite"/>.</value>
    public List<string> PixelRows { get; set; } = [];
    /// <summary>Optional multi-line glyph sprite for authored ASCII/ANSI presentation, independent of pixel palette tokens.</summary>
    /// <value>Literal character-art rows with preserved whitespace.</value>
    public List<string> AsciiRows { get; set; } = [];
    /// <summary>
    /// Gets or sets the palette collection maintained or exposed by this game scene sprite instance for downstream processing.
    /// </summary>
    /// <value>The palette value exposed by <see cref="GameSceneSprite"/>.</value>
    public Dictionary<string, string> Palette { get; set; } = [];
    /// <summary>
    /// Gets or sets the artist model value that forms part of the game scene sprite state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The artist model value exposed by <see cref="GameSceneSprite"/>.</value>
    public string ArtistModel { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the curator model value that forms part of the game scene sprite state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The curator model value exposed by <see cref="GameSceneSprite"/>.</value>
    public string CuratorModel { get; set; } = string.Empty;
}

/// <summary>Generic camera-relative parallax plane; row bands and both axes can move independently.</summary>
public sealed class GameSceneLayer
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this game scene layer instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="GameSceneLayer"/>.</value>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Gets or sets the name value that forms part of the game scene layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="GameSceneLayer"/>.</value>
    public string Name { get; set; } = "Plane";
    /// <summary>
    /// Gets or sets the depth value that forms part of the game scene layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The depth value exposed by <see cref="GameSceneLayer"/>.</value>
    public int Depth { get; set; }
    /// <summary>
    /// Gets or sets the first screen row value that forms part of the game scene layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The first screen row value exposed by <see cref="GameSceneLayer"/>.</value>
    public int FirstScreenRow { get; set; }
    /// <summary>
    /// Gets or sets the last screen row value that forms part of the game scene layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last screen row value exposed by <see cref="GameSceneLayer"/>.</value>
    public int LastScreenRow { get; set; } = 17;
    /// <summary>
    /// Gets or sets the scroll cells per world second x value that forms part of the game scene layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scroll cells per world second x value exposed by <see cref="GameSceneLayer"/>.</value>
    public double ScrollCellsPerWorldSecondX { get; set; }
    /// <summary>
    /// Gets or sets the scroll cells per world second y value that forms part of the game scene layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scroll cells per world second y value exposed by <see cref="GameSceneLayer"/>.</value>
    public double ScrollCellsPerWorldSecondY { get; set; }
    /// <summary>
    /// Gets or sets the camera factor x value that forms part of the game scene layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The camera factor x value exposed by <see cref="GameSceneLayer"/>.</value>
    public double CameraFactorX { get; set; } = 1;
    /// <summary>
    /// Gets or sets the camera factor y value that forms part of the game scene layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The camera factor y value exposed by <see cref="GameSceneLayer"/>.</value>
    public double CameraFactorY { get; set; } = 1;
    /// <summary>
    /// Gets or sets the glyph pattern value that forms part of the game scene layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The glyph pattern value exposed by <see cref="GameSceneLayer"/>.</value>
    public string GlyphPattern { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the color value that forms part of the game scene layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The color value exposed by <see cref="GameSceneLayer"/>.</value>
    public string Color { get; set; } = "#4a6b8a";
    /// <summary>
    /// Gets or sets a value indicating whether wrap applies to the game scene layer state.
    /// </summary>
    /// <value>The wrap value exposed by <see cref="GameSceneLayer"/>.</value>
    public bool Wrap { get; set; } = true;
}

/// <summary>Scene-wide palette phase synchronised to the same world clock as physics and line scrolling.</summary>
public sealed class GameScenePaletteKey
{
    /// <summary>
    /// Gets or sets the at world second value that forms part of the game scene palette key state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The at world second value exposed by <see cref="GameScenePaletteKey"/>.</value>
    public double AtWorldSecond { get; set; }
    /// <summary>
    /// Gets or sets the foreground value that forms part of the game scene palette key state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The foreground value exposed by <see cref="GameScenePaletteKey"/>.</value>
    public string Foreground { get; set; } = "#eeeeee";
    /// <summary>
    /// Gets or sets the background value that forms part of the game scene palette key state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background value exposed by <see cref="GameScenePaletteKey"/>.</value>
    public string Background { get; set; } = "#101820";
    /// <summary>
    /// Gets or sets the name value that forms part of the game scene palette key state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="GameScenePaletteKey"/>.</value>
    public string Name { get; set; } = string.Empty;
}

/// <summary>Timeline event on the world clock, including cinematics and presentation-only transitions.</summary>
public sealed class GameSceneTimedEvent
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this game scene timed event instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="GameSceneTimedEvent"/>.</value>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Gets or sets the at world second value that forms part of the game scene timed event state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The at world second value exposed by <see cref="GameSceneTimedEvent"/>.</value>
    public double AtWorldSecond { get; set; }
    /// <summary>
    /// Gets or sets the kind value that forms part of the game scene timed event state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="GameSceneTimedEvent"/>.</value>
    public string Kind { get; set; } = "scene";
    /// <summary>
    /// Gets or sets the payload JSON value that forms part of the game scene timed event state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The payload JSON value exposed by <see cref="GameSceneTimedEvent"/>.</value>
    public string PayloadJson { get; set; } = "{}";
}

/// <summary>AI-authored frame sequence synchronised to world time and applied to an actor or tile identity.</summary>
public sealed class GameSceneAnimation
{
    /// <summary>
    /// Gets or sets the stable target identifier used to identify or correlate this game scene animation instance with related application state.
    /// </summary>
    /// <value>The target identifier value exposed by <see cref="GameSceneAnimation"/>.</value>
    public string TargetId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the frames collection maintained or exposed by this game scene animation instance for downstream processing.
    /// </summary>
    /// <value>The frames value exposed by <see cref="GameSceneAnimation"/>.</value>
    public List<GameSceneAnimationFrame> Frames { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether loop applies to the game scene animation state.
    /// </summary>
    /// <value>The loop value exposed by <see cref="GameSceneAnimation"/>.</value>
    public bool Loop { get; set; } = true;
    /// <summary>
    /// Gets or sets the start world second value that forms part of the game scene animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The start world second value exposed by <see cref="GameSceneAnimation"/>.</value>
    public double StartWorldSecond { get; set; }
}

/// <summary>One authored sprite and its duration, measured in game-world seconds.</summary>
public sealed class GameSceneAnimationFrame
{
    /// <summary>
    /// Gets or sets the stable sprite identifier used to identify or correlate this game scene animation frame instance with related application state.
    /// </summary>
    /// <value>The sprite identifier value exposed by <see cref="GameSceneAnimationFrame"/>.</value>
    public string SpriteId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the duration world seconds value that forms part of the game scene animation frame state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The duration world seconds value exposed by <see cref="GameSceneAnimationFrame"/>.</value>
    public double DurationWorldSeconds { get; set; } = .15;
}

/// <summary>Data-defined presentation/HUD text with optional world-time placeholders; no game-specific interface text is compiled.</summary>
public sealed class GameSceneHudElement
{
    /// <summary>
    /// Gets or sets the x value that forms part of the game scene hud element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="GameSceneHudElement"/>.</value>
    public int X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the game scene hud element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="GameSceneHudElement"/>.</value>
    public int Y { get; set; }
    /// <summary>
    /// Gets or sets the format value that forms part of the game scene hud element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The format value exposed by <see cref="GameSceneHudElement"/>.</value>
    public string Format { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the color value that forms part of the game scene hud element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The color value exposed by <see cref="GameSceneHudElement"/>.</value>
    public string Color { get; set; } = "#ffffff";
}

/// <summary>An open mutable worktree, backed by updatable project artifacts rather than a stream of completed revisions.</summary>
public sealed class GameStudioDraft
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this game studio draft instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="GameStudioDraft"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable draft identifier used to identify or correlate this game studio draft instance with related application state.
    /// </summary>
    /// <value>The draft identifier value exposed by <see cref="GameStudioDraft"/>.</value>
    public Guid DraftId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable parent revision identifier used to identify or correlate this game studio draft instance with related application state.
    /// </summary>
    /// <value>The parent revision identifier value exposed by <see cref="GameStudioDraft"/>.</value>
    public Guid? ParentRevisionId { get; set; }
    /// <summary>
    /// Gets or sets the stable closed revision identifier used to identify or correlate this game studio draft instance with related application state.
    /// </summary>
    /// <value>The closed revision identifier value exposed by <see cref="GameStudioDraft"/>.</value>
    public Guid? ClosedRevisionId { get; set; }
    /// <summary>
    /// Gets or sets the branch name value that forms part of the game studio draft state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The branch name value exposed by <see cref="GameStudioDraft"/>.</value>
    public string BranchName { get; set; } = "main";
    /// <summary>
    /// Gets or sets the scene value that forms part of the game studio draft state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scene value exposed by <see cref="GameStudioDraft"/>.</value>
    public GameSceneDocument Scene { get; set; } = new();
    /// <summary>
    /// Gets or sets the file paths used by this game studio draft instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The file paths value exposed by <see cref="GameStudioDraft"/>.</value>
    public List<string> FilePaths { get; set; } = [];
    /// <summary>
    /// Gets or sets the updated at UTC associated with this game studio draft state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="GameStudioDraft"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>A proposed visual/scene revision remains isolated until a human accepts its curated geometry.</summary>
public sealed class GameSceneProposal
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this game scene proposal instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="GameSceneProposal"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable draft identifier used to identify or correlate this game scene proposal instance with related application state.
    /// </summary>
    /// <value>The draft identifier value exposed by <see cref="GameSceneProposal"/>.</value>
    public Guid DraftId { get; set; }
    /// <summary>
    /// Gets or sets the artist value that forms part of the game scene proposal state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The artist value exposed by <see cref="GameSceneProposal"/>.</value>
    public string Artist { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the curator value that forms part of the game scene proposal state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The curator value exposed by <see cref="GameSceneProposal"/>.</value>
    public string Curator { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the game scene proposal state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="GameSceneProposal"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the scene value that forms part of the game scene proposal state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scene value exposed by <see cref="GameSceneProposal"/>.</value>
    public GameSceneDocument Scene { get; set; } = new();
}

/// <summary>Small upsert-only visual contribution, allowing low-parameter models to change one creature, sprite or scanline band at a time.</summary>
public sealed class GameScenePatch
{
    /// <summary>
    /// Gets or sets the tiles collection maintained or exposed by this game scene patch instance for downstream processing.
    /// </summary>
    /// <value>The tiles value exposed by <see cref="GameScenePatch"/>.</value>
    public List<GameSceneTile> Tiles { get; set; } = [];
    /// <summary>
    /// Gets or sets the actors collection maintained or exposed by this game scene patch instance for downstream processing.
    /// </summary>
    /// <value>The actors value exposed by <see cref="GameScenePatch"/>.</value>
    public List<GameSceneActor> Actors { get; set; } = [];
    /// <summary>
    /// Gets or sets the sprites collection maintained or exposed by this game scene patch instance for downstream processing.
    /// </summary>
    /// <value>The sprites value exposed by <see cref="GameScenePatch"/>.</value>
    public List<GameSceneSprite> Sprites { get; set; } = [];
    /// <summary>
    /// Gets or sets the layers collection maintained or exposed by this game scene patch instance for downstream processing.
    /// </summary>
    /// <value>The layers value exposed by <see cref="GameScenePatch"/>.</value>
    public List<GameSceneLayer> Layers { get; set; } = [];
    /// <summary>
    /// Gets or sets the animations collection maintained or exposed by this game scene patch instance for downstream processing.
    /// </summary>
    /// <value>The animations value exposed by <see cref="GameScenePatch"/>.</value>
    public List<GameSceneAnimation> Animations { get; set; } = [];
    /// <summary>
    /// Gets or sets the hud collection maintained or exposed by this game scene patch instance for downstream processing.
    /// </summary>
    /// <value>The hud value exposed by <see cref="GameScenePatch"/>.</value>
    public List<GameSceneHudElement> Hud { get; set; } = [];
    /// <summary>
    /// Gets or sets the palette timeline collection maintained or exposed by this game scene patch instance for downstream processing.
    /// </summary>
    /// <value>The palette timeline value exposed by <see cref="GameScenePatch"/>.</value>
    public List<GameScenePaletteKey> PaletteTimeline { get; set; } = [];
    /// <summary>Optional presentation choice for this patch; null preserves the existing project's mode.</summary>
    /// <value>Scene artwork mode or null.</value>
    public GameSceneArtworkMode? ArtworkMode { get; set; }
    /// <summary>Individual authored screen canvases added or replaced by stable identity.</summary>
    /// <value>Proposed canvas changes.</value>
    public List<GameSceneTextCanvas> TextCanvases { get; set; } = [];
    /// <summary>Named regex catalog bindings added or replaced by pattern and variable identity.</summary>
    /// <value>Proposed declarative state transitions.</value>
    public List<GameSceneRegexTransition> RegexTransitions { get; set; } = [];
}

/// <summary>Independent Council reviewer response; geometry validation is always repeated by the service.</summary>
public sealed class GameSceneReview
{
    /// <summary>
    /// Gets or sets the stable proposal identifier used to identify or correlate this game scene review instance with related application state.
    /// </summary>
    /// <value>The proposal identifier value exposed by <see cref="GameSceneReview"/>.</value>
    public Guid ProposalId { get; set; }
    /// <summary>
    /// Gets or sets the stable draft identifier used to identify or correlate this game scene review instance with related application state.
    /// </summary>
    /// <value>The draft identifier value exposed by <see cref="GameSceneReview"/>.</value>
    public Guid DraftId { get; set; }
    /// <summary>
    /// Gets or sets the curator model value that forms part of the game scene review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The curator model value exposed by <see cref="GameSceneReview"/>.</value>
    public string CuratorModel { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether approved applies to the game scene review state.
    /// </summary>
    /// <value>The approved value exposed by <see cref="GameSceneReview"/>.</value>
    public bool Approved { get; set; }
    /// <summary>
    /// Gets or sets the critique value that forms part of the game scene review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The critique value exposed by <see cref="GameSceneReview"/>.</value>
    public string Critique { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the findings collection maintained or exposed by this game scene review instance for downstream processing.
    /// </summary>
    /// <value>The findings value exposed by <see cref="GameSceneReview"/>.</value>
    public List<GameSceneFinding> Findings { get; set; } = [];
}

/// <summary>Diagnostics by which independent AI/human curators can inspect a generated scene before it is adopted.</summary>
/// <param name="Code">Code value supplied to the game scene finding operation and used when producing its result.</param>
/// <param name="Message">Message value supplied to the game scene finding operation and used when producing its result.</param>
/// <param name="BlocksAcceptance">Value indicating whether blocks acceptance should apply to this operation.</param>
public sealed record GameSceneFinding(string Code, string Message, bool BlocksAcceptance);

/// <summary>Pure fixed-step state: elapsed real time never sets the speed of the game.</summary>
public sealed class GameSceneClock
{
    /// <summary>
    /// Gets or sets the tick value that forms part of the game scene clock state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The tick value exposed by <see cref="GameSceneClock"/>.</value>
    public long Tick { get; set; }
    /// <summary>
    /// Gets or sets the fractional real seconds value that forms part of the game scene clock state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fractional real seconds value exposed by <see cref="GameSceneClock"/>.</value>
    public double FractionalRealSeconds { get; set; }
    /// <summary>
    /// Gets or sets the world seconds value that forms part of the game scene clock state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The world seconds value exposed by <see cref="GameSceneClock"/>.</value>
    public double WorldSeconds { get; set; }
    /// <summary>
    /// Gets or sets the current turn value that forms part of the game scene clock state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current turn value exposed by <see cref="GameSceneClock"/>.</value>
    public int CurrentTurn { get; set; }
    /// <summary>
    /// Gets or sets the camera x value that forms part of the game scene clock state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The camera x value exposed by <see cref="GameSceneClock"/>.</value>
    public double CameraX { get; set; }
    /// <summary>
    /// Gets or sets the camera y value that forms part of the game scene clock state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The camera y value exposed by <see cref="GameSceneClock"/>.</value>
    public double CameraY { get; set; }
    /// <summary>
    /// Gets or sets the actor positions collection maintained or exposed by this game scene clock instance for downstream processing.
    /// </summary>
    /// <value>The actor positions value exposed by <see cref="GameSceneClock"/>.</value>
    public Dictionary<string, GameScenePosition> ActorPositions { get; set; } = [];
    /// <summary>Runtime-only scene presentation variables produced by approved regex transitions.</summary>
    /// <value>Named values; the authored GameSceneDocument remains unchanged.</value>
    public Dictionary<string, string> SceneVariables { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>Simulation-space position maintained separately from the authored scene.</summary>
/// <param name="X">X value supplied to the game scene position operation and used when producing its result.</param>
/// <param name="Y">Y value supplied to the game scene position operation and used when producing its result.</param>
public sealed record GameScenePosition(double X, double Y);

/// <summary>A bounded console preview produced by the same semantic scene data as the Godot project.</summary>
/// <param name="Text">Text value supplied to the game scene preview operation and used when producing its result.</param>
/// <param name="Foreground">Foreground value supplied to the game scene preview operation and used when producing its result.</param>
/// <param name="Background">Background value supplied to the game scene preview operation and used when producing its result.</param>
/// <param name="WorldSeconds">World seconds value supplied to the game scene preview operation and used when producing its result.</param>
/// <param name="Tick">Tick value supplied to the game scene preview operation and used when producing its result.</param>
public sealed record GameScenePreview(string Text, string Foreground, string Background, double WorldSeconds, long Tick);
