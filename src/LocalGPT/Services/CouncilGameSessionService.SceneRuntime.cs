using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Shared GameDirector execution of an authored, revision-owned two-dimensional scene.</summary>
public sealed partial class CouncilGameSessionService
{
    /// <summary>Applies one reviewed semantic control and advances the same fixed-tick scene engine used for authoring.</summary>
    /// <param name="session">Authoritative session whose scene and clock are owned by GameDirector.</param>
    /// <param name="action">GameDirector-approved semantic action.</param>
    private void ApplySceneControl(CouncilGameSessionState session, string action)
    {
        try
        {
            var scene = session.Scene ?? throw new InvalidOperationException("The game session has no authored scene.");
            var clock = session.SceneClock ?? throw new InvalidOperationException("The game session has no scene clock.");
            var heading = ((int)Math.Round(session.FacingRadians / (Math.PI / 2d)) % 4 + 4) % 4;
            if (action == "turn-left")
                heading = (heading + 1) % 4;
            else if (action == "turn-right")
                heading = (heading + 3) % 4;
            session.FacingRadians = heading * Math.PI / 2d;

            // Authored actor control is generic; scenes without a controlled actor remain explorable camera/cinematic worlds.
            var directions = new (int X, int Y)[] { (0, 1), (-1, 0), (0, -1), (1, 0) };
            var direction = action switch
            {
                "move-forward" => heading,
                "move-backward" => (heading + 2) % 4,
                "strafe-left" => (heading + 1) % 4,
                "strafe-right" => (heading + 3) % 4,
                _ => -1
            };
            if (direction >= 0)
            {
                var maxX = Math.Max(0, scene.WorldWidth - scene.ViewWidth);
                var maxY = Math.Max(0, scene.WorldHeight - scene.ViewHeight);
                var controlled = scene.Actors.FirstOrDefault(actor => actor.PlayerControlled);
                if (controlled is not null)
                {
                    var current = clock.ActorPositions.TryGetValue(controlled.Id, out var position)
                        ? position : new GameScenePosition(controlled.X, controlled.Y);
                    var nextX = Math.Clamp(current.X + directions[direction].X, 0, scene.WorldWidth - 1);
                    var nextY = Math.Clamp(current.Y + directions[direction].Y, 0, scene.WorldHeight - 1);
                    if (!controlled.Collides || !scene.Tiles.Any(tile => tile.Solid && tile.X == (int)nextX && tile.Y == (int)nextY))
                        clock.ActorPositions[controlled.Id] = new GameScenePosition(nextX, nextY);
                }
                else
                {
                    var x = Math.Clamp((int)Math.Floor(clock.CameraX) + directions[direction].X, 0, maxX);
                    var y = Math.Clamp((int)Math.Floor(clock.CameraY) + directions[direction].Y, 0, maxY);
                    if (!scene.Tiles.Any(tile => tile.Solid && tile.X == x && tile.Y == y))
                    {
                        clock.CameraX = x;
                        clock.CameraY = y;
                    }
                }
            }
            gameSceneEngine.Advance(scene, clock, 0, takeTurn: true);
            var followedActor = scene.Actors.FirstOrDefault(actor => actor.PlayerControlled);
            if (followedActor is not null)
            {
                var visible = clock.ActorPositions.TryGetValue(followedActor.Id, out var playerPosition)
                    ? playerPosition : new GameScenePosition(followedActor.X, followedActor.Y);
                clock.CameraX = Math.Clamp(Math.Floor(visible.X - scene.ViewWidth / 2d),
                    0, Math.Max(0, scene.WorldWidth - scene.ViewWidth));
                clock.CameraY = Math.Clamp(Math.Floor(visible.Y - scene.ViewHeight / 2d),
                    0, Math.Max(0, scene.WorldHeight - scene.ViewHeight));
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying a Game Studio revision's authoritative scene control failed for session {SessionId}.", session.Id);
            throw;
        }
    }
}
