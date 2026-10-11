namespace LocalGPT.BusinessObjects;

/// <summary>Editable, project-owned two-dimensional world description. No game's scenery, art or rules are compiled into the engine.</summary>
public sealed class GameSceneDocument
{
    public string SceneName { get; set; } = "New world";
    public int WorldWidth { get; set; } = 64;
    public int WorldHeight { get; set; } = 48;
    public int ViewWidth { get; set; } = 32;
    public int ViewHeight { get; set; } = 18;
    public int ChunkSize { get; set; } = 16;
    public int FixedTicksPerSecond { get; set; } = 60;
    public double WorldSecondsPerRealSecond { get; set; } = 1;
    public double SecondsPerTurn { get; set; } = 1;
    public List<GameSceneTile> Tiles { get; set; } = [];
    public List<GameSceneSprite> Sprites { get; set; } = [];
    public List<GameSceneActor> Actors { get; set; } = [];
    public List<GameSceneLayer> Layers { get; set; } = [];
    public List<GameScenePaletteKey> PaletteTimeline { get; set; } = [];
    public List<GameSceneTimedEvent> Timeline { get; set; } = [];
    public List<GameSceneAnimation> Animations { get; set; } = [];
    public List<GameSceneHudElement> Hud { get; set; } = [];
    public string ArtDirection { get; set; } = string.Empty;
}

/// <summary>One cell in an open-world chunk, with semantic exclusion flags for object placement.</summary>
public sealed class GameSceneTile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int X { get; set; }
    public int Y { get; set; }
    public string Glyph { get; set; } = ".";
    public string SpriteId { get; set; } = string.Empty;
    public string Color { get; set; } = "#87cefa";
    public bool Solid { get; set; }
    public bool BlocksDecoration { get; set; }
    public string Biome { get; set; } = string.Empty;
}

/// <summary>AI-authored or hand-edited sprite/creature represented independently of its movement rules.</summary>
public sealed class GameSceneActor
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Kind { get; set; } = "creature";
    public double X { get; set; }
    public double Y { get; set; }
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
    public string Glyph { get; set; } = "@";
    public string SpriteId { get; set; } = string.Empty;
    public string Color { get; set; } = "#ffffff";
    public int Layer { get; set; } = 10;
    public bool Collides { get; set; } = true;
    /// <summary>Marks the single authored actor moved by human/AI GameDirector controls; no fixed creature type is assumed.</summary>
    public bool PlayerControlled { get; set; }
    public string ArtistModel { get; set; } = string.Empty;
    public string CuratorModel { get; set; } = string.Empty;
}

/// <summary>Just-in-time pixel art; each character indexes an authored palette, and blank cells remain transparent.</summary>
public sealed class GameSceneSprite
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public List<string> PixelRows { get; set; } = [];
    public Dictionary<string, string> Palette { get; set; } = [];
    public string ArtistModel { get; set; } = string.Empty;
    public string CuratorModel { get; set; } = string.Empty;
}

/// <summary>Generic camera-relative parallax plane; row bands and both axes can move independently.</summary>
public sealed class GameSceneLayer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Plane";
    public int Depth { get; set; }
    public int FirstScreenRow { get; set; }
    public int LastScreenRow { get; set; } = 17;
    public double ScrollCellsPerWorldSecondX { get; set; }
    public double ScrollCellsPerWorldSecondY { get; set; }
    public double CameraFactorX { get; set; } = 1;
    public double CameraFactorY { get; set; } = 1;
    public string GlyphPattern { get; set; } = string.Empty;
    public string Color { get; set; } = "#4a6b8a";
    public bool Wrap { get; set; } = true;
}

/// <summary>Scene-wide palette phase synchronised to the same world clock as physics and line scrolling.</summary>
public sealed class GameScenePaletteKey
{
    public double AtWorldSecond { get; set; }
    public string Foreground { get; set; } = "#eeeeee";
    public string Background { get; set; } = "#101820";
    public string Name { get; set; } = string.Empty;
}

/// <summary>Timeline event on the world clock, including cinematics and presentation-only transitions.</summary>
public sealed class GameSceneTimedEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public double AtWorldSecond { get; set; }
    public string Kind { get; set; } = "scene";
    public string PayloadJson { get; set; } = "{}";
}

/// <summary>AI-authored frame sequence synchronised to world time and applied to an actor or tile identity.</summary>
public sealed class GameSceneAnimation
{
    public string TargetId { get; set; } = string.Empty;
    public List<GameSceneAnimationFrame> Frames { get; set; } = [];
    public bool Loop { get; set; } = true;
    public double StartWorldSecond { get; set; }
}

/// <summary>One authored sprite and its duration, measured in game-world seconds.</summary>
public sealed class GameSceneAnimationFrame
{
    public string SpriteId { get; set; } = string.Empty;
    public double DurationWorldSeconds { get; set; } = .15;
}

/// <summary>Data-defined presentation/HUD text with optional world-time placeholders; no game-specific interface text is compiled.</summary>
public sealed class GameSceneHudElement
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Color { get; set; } = "#ffffff";
}

/// <summary>An open mutable worktree, backed by updatable project artifacts rather than a stream of completed revisions.</summary>
public sealed class GameStudioDraft
{
    public Guid ProjectId { get; set; }
    public Guid DraftId { get; set; } = Guid.NewGuid();
    public Guid? ParentRevisionId { get; set; }
    public Guid? ClosedRevisionId { get; set; }
    public string BranchName { get; set; } = "main";
    public GameSceneDocument Scene { get; set; } = new();
    public List<string> FilePaths { get; set; } = [];
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>A proposed visual/scene revision remains isolated until a human accepts its curated geometry.</summary>
public sealed class GameSceneProposal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DraftId { get; set; }
    public string Artist { get; set; } = string.Empty;
    public string Curator { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GameSceneDocument Scene { get; set; } = new();
}

/// <summary>Small upsert-only visual contribution, allowing low-parameter models to change one creature, sprite or scanline band at a time.</summary>
public sealed class GameScenePatch
{
    public List<GameSceneTile> Tiles { get; set; } = [];
    public List<GameSceneActor> Actors { get; set; } = [];
    public List<GameSceneSprite> Sprites { get; set; } = [];
    public List<GameSceneLayer> Layers { get; set; } = [];
    public List<GameSceneAnimation> Animations { get; set; } = [];
    public List<GameSceneHudElement> Hud { get; set; } = [];
    public List<GameScenePaletteKey> PaletteTimeline { get; set; } = [];
}

/// <summary>Independent Council reviewer response; geometry validation is always repeated by the service.</summary>
public sealed class GameSceneReview
{
    public Guid ProposalId { get; set; }
    public Guid DraftId { get; set; }
    public string CuratorModel { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public string Critique { get; set; } = string.Empty;
    public List<GameSceneFinding> Findings { get; set; } = [];
}

/// <summary>Diagnostics by which independent AI/human curators can inspect a generated scene before it is adopted.</summary>
public sealed record GameSceneFinding(string Code, string Message, bool BlocksAcceptance);

/// <summary>Pure fixed-step state: elapsed real time never sets the speed of the game.</summary>
public sealed class GameSceneClock
{
    public long Tick { get; set; }
    public double FractionalRealSeconds { get; set; }
    public double WorldSeconds { get; set; }
    public int CurrentTurn { get; set; }
    public double CameraX { get; set; }
    public double CameraY { get; set; }
    public Dictionary<string, GameScenePosition> ActorPositions { get; set; } = [];
}

/// <summary>Simulation-space position maintained separately from the authored scene.</summary>
public sealed record GameScenePosition(double X, double Y);

/// <summary>A bounded console preview produced by the same semantic scene data as the Godot project.</summary>
public sealed record GameScenePreview(string Text, string Foreground, string Background, double WorldSeconds, long Tick);
