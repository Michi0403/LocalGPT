using System.Security.Cryptography;
using System.Text;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Owns the deterministic ASCII corridor map, hostile actors, combat and pathfinding.</summary>
public sealed partial class CouncilGameSessionService
{
    private void InitializeDoomWorld(CouncilGameSessionState session)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(session);
            const int width = 32;
            const int height = 20;
            var seed = session.MapSeed > 0
                ? session.MapSeed
                : !string.IsNullOrWhiteSpace(session.ScenarioPrompt)
                    ? BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(session.ScenarioPrompt)), 0) & int.MaxValue
                    : BitConverter.ToInt32(session.Id.ToByteArray(), 0) & int.MaxValue;
            seed = Math.Max(1, seed);
            var random = new Random(seed);
            var cells = Enumerable.Range(0, height)
                .Select(_ => Enumerable.Repeat('#', width).ToArray())
                .ToArray();
            var centers = new List<(int X, int Y)>();

            CarveRoom(cells, 1, 1, 8, 6);
            centers.Add((6, 3));
            var previous = centers[0];
            for (var index = 0; index < 5; index++)
            {
                var roomWidth = random.Next(5, 9);
                var roomHeight = random.Next(4, 7);
                var roomX = random.Next(10, Math.Max(11, width - roomWidth - 1));
                var roomY = random.Next(1, Math.Max(2, height - roomHeight - 1));
                CarveRoom(cells, roomX, roomY, roomWidth, roomHeight);
                var center = (X: roomX + roomWidth / 2, Y: roomY + roomHeight / 2);
                CarveCorridor(cells, previous.X, previous.Y, center.X, center.Y, random.Next(0, 2) == 0);
                centers.Add(center);
                previous = center;
            }

            session.MapSeed = seed;
            session.WorldMap = cells.Select(row => new string(row)).ToList();
            session.PlayerX = 3;
            session.PlayerY = 3;
            session.FacingRadians = 0d;
            session.ExtractionX = centers[^1].X;
            session.ExtractionY = centers[^1].Y;
            session.Enemies =
            [
                BuildEnemyState("grunt-alpha", "Grunt Alpha", "M", 6, 3, 75, 7),
                BuildEnemyState("grunt-beta", "Grunt Beta", "G", centers[1].X, centers[1].Y, 90, 6),
                BuildEnemyState("grunt-gamma", "Grunt Gamma", "G", centers[2].X, centers[2].Y, 90, 6),
                BuildEnemyState("brute-delta", "Brute Delta", "B", centers[3].X, centers[3].Y, 140, 9)
            ];
            session.CombatMessage = string.IsNullOrWhiteSpace(session.ScenarioPrompt)
                ? "CONTACT: Grunt Alpha is directly ahead. Clear hostiles, then reach X extraction."
                : $"SCENARIO: {session.ScenarioPrompt} · Clear hostiles, then reach X extraction.";
            session.BlockedMoveStreak = 0;
            session.LastMoveBlocked = false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Initializing the deterministic ASCII corridor world failed.");
            throw;
        }
    }

    private void CarveRoom(char[][] cells, int x, int y, int width, int height)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(cells);
            for (var row = Math.Max(1, y); row < Math.Min(cells.Length - 1, y + height); row++)
                for (var column = Math.Max(1, x); column < Math.Min(cells[row].Length - 1, x + width); column++)
                    cells[row][column] = '.';
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Carving an ASCII corridor room failed at {X},{Y}.", x, y);
            throw;
        }
    }

    private void CarveCorridor(char[][] cells, int fromX, int fromY, int toX, int toY, bool horizontalFirst)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(cells);
            var x = fromX;
            var y = fromY;
            void CarveCurrent()
            {
                if (y > 0 && y < cells.Length - 1 && x > 0 && x < cells[y].Length - 1)
                    cells[y][x] = '.';
            }
            CarveCurrent();
            if (horizontalFirst)
            {
                while (x != toX) { x += Math.Sign(toX - x); CarveCurrent(); }
                while (y != toY) { y += Math.Sign(toY - y); CarveCurrent(); }
            }
            else
            {
                while (y != toY) { y += Math.Sign(toY - y); CarveCurrent(); }
                while (x != toX) { x += Math.Sign(toX - x); CarveCurrent(); }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Carving an ASCII corridor connection failed.");
            throw;
        }
    }

    private CouncilGameEnemyState BuildEnemyState(string key, string name, string glyph, int x, int y, int health, int contactDamage)
    {
        try
        {
            return new CouncilGameEnemyState
            {
                Key = key,
                Name = name,
                Glyph = glyph,
                X = x,
                Y = y,
                Health = health,
                MaximumHealth = health,
                ContactDamage = contactDamage
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating deterministic hostile state {EnemyKey} failed.", key);
            throw;
        }
    }

    private IReadOnlyList<string> GetWorldMap(CouncilGameSessionState session)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(session);
            return session.WorldMap.Count > 0 ? session.WorldMap : doomMap;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the authoritative ASCII corridor map failed.");
            throw;
        }
    }

    private IReadOnlyList<string> GetWorldMap(CouncilGameSessionSnapshot snapshot)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            return snapshot.WorldMap.Count > 0 ? snapshot.WorldMap : doomMap;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the snapshot ASCII corridor map failed.");
            throw;
        }
    }

    private bool IsWalkable(IReadOnlyList<string> map, int x, int y)
    {
        try
        {
            return y >= 0 && y < map.Count && x >= 0 && x < map[y].Length && map[y][x] != '#';
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Checking ASCII corridor walkability failed at {X},{Y}.", x, y);
            throw;
        }
    }

    private bool IsLivingEnemyAt(CouncilGameSessionState session, int x, int y, string? exceptKey = null)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(session);
            return session.Enemies.Any(enemy => enemy.IsAlive
                && enemy.X == x
                && enemy.Y == y
                && !string.Equals(enemy.Key, exceptKey, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Checking hostile occupancy failed at {X},{Y}.", x, y);
            throw;
        }
    }

    private bool IsLivingEnemyAt(CouncilGameSessionSnapshot snapshot, int x, int y)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            return snapshot.Enemies.Any(enemy => enemy.IsAlive && enemy.X == x && enemy.Y == y);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Checking hostile snapshot occupancy failed at {X},{Y}.", x, y);
            throw;
        }
    }

    private CouncilGameEnemySnapshot CloneEnemy(CouncilGameEnemyState enemy)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(enemy);
            return new CouncilGameEnemySnapshot
            {
                Key = enemy.Key,
                Name = enemy.Name,
                Glyph = enemy.Glyph,
                X = enemy.X,
                Y = enemy.Y,
                Health = enemy.Health,
                MaximumHealth = enemy.MaximumHealth,
                ContactDamage = enemy.ContactDamage
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cloning deterministic hostile state failed.");
            throw;
        }
    }

    private void ResolveShot(CouncilGameSessionState session)
    {
        try
        {
            var map = GetWorldMap(session);
            CouncilGameEnemyState? target = null;
            var targetDistance = double.MaxValue;
            foreach (var enemy in session.Enemies.Where(enemy => enemy.IsAlive))
            {
                var dx = enemy.X + .5d - (session.PlayerX + .5d);
                var dy = enemy.Y + .5d - (session.PlayerY + .5d);
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance <= 0.01d || distance > 14d) continue;
                var angle = Math.Atan2(dy, dx);
                var delta = Math.Abs(SignedAngleDelta(session.FacingRadians, angle));
                var hitCone = Math.Max(.045d, Math.Atan2(.55d, distance));
                if (delta > hitCone || !HasLineOfSight(map, session.PlayerX, session.PlayerY, enemy.X, enemy.Y)) continue;
                if (distance < targetDistance)
                {
                    target = enemy;
                    targetDistance = distance;
                }
            }

            if (target is null)
            {
                session.CombatMessage = "SHOT: no hostile intersected the crosshair ray.";
                return;
            }

            const int damage = 50;
            target.Health = Math.Max(0, target.Health - damage);
            session.CombatMessage = target.IsAlive
                ? $"HIT: {target.Name} took {damage}; {target.Health}/{target.MaximumHealth} HP remains."
                : $"DOWN: {target.Name} eliminated.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving a deterministic ASCII corridor shot failed.");
            throw;
        }
    }

    private void AdvanceDoomWorld(CouncilGameSessionState session)
    {
        try
        {
            if (session.RuntimeProfile != CouncilGameRuntimeProfile.Corridor || session.Status != "Running") return;
            var alive = session.Enemies.Where(enemy => enemy.IsAlive).OrderBy(enemy => enemy.Key, StringComparer.Ordinal).ToList();
            if (alive.Count == 0)
            {
                ResolveExtraction(session);
                if (session.Status == "Running" && !session.CombatMessage.Contains("EXTRACTION", StringComparison.OrdinalIgnoreCase))
                    session.CombatMessage = $"AREA CLEAR: reach extraction X at {session.ExtractionX:00},{session.ExtractionY:00}.";
                return;
            }

            var attackMessages = new List<string>();
            for (var index = 0; index < alive.Count; index++)
            {
                var enemy = alive[index];
                var distance = Math.Abs(enemy.X - session.PlayerX) + Math.Abs(enemy.Y - session.PlayerY);
                if (distance == 1)
                {
                    session.Health = Math.Max(0, session.Health - enemy.ContactDamage);
                    attackMessages.Add($"{enemy.Name} hit -{enemy.ContactDamage} HP");
                    continue;
                }

                if ((session.Turn + index) % 2 != 0) continue;
                var blocked = session.Enemies
                    .Where(other => other.IsAlive && !string.Equals(other.Key, enemy.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(other => (other.X, other.Y))
                    .ToHashSet();
                var path = FindShortestPath(GetWorldMap(session), enemy.X, enemy.Y, session.PlayerX, session.PlayerY, blocked, true);
                if (path.Count < 2) continue;
                var next = path[1];
                if (next.X == session.PlayerX && next.Y == session.PlayerY)
                {
                    session.Health = Math.Max(0, session.Health - enemy.ContactDamage);
                    attackMessages.Add($"{enemy.Name} hit -{enemy.ContactDamage} HP");
                }
                else if (!IsLivingEnemyAt(session, next.X, next.Y, enemy.Key))
                {
                    enemy.X = next.X;
                    enemy.Y = next.Y;
                }
            }

            if (attackMessages.Count > 0)
                session.CombatMessage = $"{session.CombatMessage}  INCOMING: {string.Join("; ", attackMessages)}.".Trim();
            if (session.Health <= 0)
            {
                session.Status = "Completed";
                session.AutoplayEnabled = false;
                session.HumanInputRequired = false;
                session.CurrentTurnOwner = "Game Over";
                session.InputReason = "The player was defeated. Start a new corridor session to play again.";
                session.CombatMessage = "DEFEAT: hostile contact reduced HP to zero.";
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Advancing deterministic ASCII corridor hostiles failed.");
            throw;
        }
    }

    private void ResolveExtraction(CouncilGameSessionState session)
    {
        try
        {
            if (session.Enemies.Any(enemy => enemy.IsAlive)) return;
            if (session.PlayerX != session.ExtractionX || session.PlayerY != session.ExtractionY) return;
            session.Status = "Completed";
            session.AutoplayEnabled = false;
            session.HumanInputRequired = false;
            session.CurrentTurnOwner = "Mission Complete";
            session.InputReason = "Extraction reached after all hostiles were cleared.";
            session.CombatMessage = "EXTRACTION COMPLETE: arena clear.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving ASCII corridor extraction failed.");
            throw;
        }
    }

    private bool HasLineOfSight(IReadOnlyList<string> map, int fromX, int fromY, int toX, int toY)
    {
        try
        {
            var dx = toX - fromX;
            var dy = toY - fromY;
            var steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (steps <= 1) return true;
            for (var step = 1; step < steps; step++)
            {
                var x = (int)Math.Round(fromX + dx * (step / (double)steps));
                var y = (int)Math.Round(fromY + dy * (step / (double)steps));
                if (!IsWalkable(map, x, y)) return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Checking deterministic ASCII corridor line of sight failed.");
            throw;
        }
    }

    private List<(int X, int Y)> FindShortestPath(
        IReadOnlyList<string> map,
        int startX,
        int startY,
        int targetX,
        int targetY,
        HashSet<(int X, int Y)>? blocked = null,
        bool allowTargetOccupied = false)
    {
        try
        {
            if (!IsWalkable(map, startX, startY) || !IsWalkable(map, targetX, targetY)) return [];
            var start = (X: startX, Y: startY);
            var target = (X: targetX, Y: targetY);
            var queue = new Queue<(int X, int Y)>();
            var previous = new Dictionary<(int X, int Y), (int X, int Y)>();
            var visited = new HashSet<(int X, int Y)> { start };
            queue.Enqueue(start);
            var directions = new[] { (X: 1, Y: 0), (X: 0, Y: 1), (X: -1, Y: 0), (X: 0, Y: -1) };
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target) break;
                foreach (var direction in directions)
                {
                    var next = (X: current.X + direction.X, Y: current.Y + direction.Y);
                    if (!IsWalkable(map, next.X, next.Y) || visited.Contains(next)) continue;
                    if (blocked?.Contains(next) == true && !(allowTargetOccupied && next == target)) continue;
                    visited.Add(next);
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
            if (!visited.Contains(target)) return [];
            var path = new List<(int X, int Y)> { target };
            var cursor = target;
            while (cursor != start)
            {
                cursor = previous[cursor];
                path.Add(cursor);
            }
            path.Reverse();
            return path;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finding a deterministic ASCII corridor path failed from {StartX},{StartY} to {TargetX},{TargetY}.", startX, startY, targetX, targetY);
            throw;
        }
    }

    private double SignedAngleDelta(double from, double to)
    {
        try
        {
            var delta = NormalizeRadians(to) - NormalizeRadians(from);
            while (delta > Math.PI) delta -= Math.PI * 2d;
            while (delta < -Math.PI) delta += Math.PI * 2d;
            return delta;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Calculating an ASCII corridor angle delta failed.");
            throw;
        }
    }
}
