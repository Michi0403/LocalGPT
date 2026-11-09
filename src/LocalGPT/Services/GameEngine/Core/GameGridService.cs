using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Grid geometry of map-based games: walkability, line of sight, breadth-first paths, ray marching and carving.
/// </summary>
/// <param name="logger">Receives geometry diagnostics.</param>
public sealed class GameGridService(ILogger<GameGridService> logger) : IGameGridService
{
    /// <summary>
    /// The four path-finding neighbour offsets in search order.
    /// </summary>
    private readonly (int X, int Y)[] neighbourOffsets = [(1, 0), (0, 1), (-1, 0), (0, -1)];

    /// <inheritdoc />
    public bool IsWalkable(IReadOnlyList<string> map, int x, int y, char wall)
    {
        try
        {
            return y >= 0 && y < map.Count && x >= 0 && x < map[y].Length && map[y][x] != wall;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Testing grid cell {X},{Y} failed.", x, y);
            throw;
        }
    }

    /// <inheritdoc />
    public bool HasLineOfSight(IReadOnlyList<string> map, int fromX, int fromY, int toX, int toY, char wall)
    {
        try
        {
            var dx = toX - fromX;
            var dy = toY - fromY;
            var steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (steps <= 1)
                return true;
            for (var step = 1; step < steps; step++)
            {
                var x = (int)Math.Round(fromX + dx * (step / (double)steps));
                var y = (int)Math.Round(fromY + dy * (step / (double)steps));
                if (!IsWalkable(map, x, y, wall))
                    return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Testing line of sight from {FromX},{FromY} to {ToX},{ToY} failed.", fromX, fromY, toX, toY);
            throw;
        }
    }

    /// <inheritdoc />
    public List<(int X, int Y)> FindShortestPath(IReadOnlyList<string> map, int startX, int startY, int targetX, int targetY, char wall, HashSet<(int X, int Y)>? blocked, bool allowTargetOccupied)
    {
        try
        {
            if (!IsWalkable(map, startX, startY, wall) || !IsWalkable(map, targetX, targetY, wall))
                return [];
            var start = (X: startX, Y: startY);
            var target = (X: targetX, Y: targetY);
            var queue = new Queue<(int X, int Y)>();
            var previous = new Dictionary<(int X, int Y), (int X, int Y)>();
            var visited = new HashSet<(int X, int Y)> { start };
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target)
                    break;
                foreach (var offset in neighbourOffsets)
                {
                    var next = (X: current.X + offset.X, Y: current.Y + offset.Y);
                    if (!IsWalkable(map, next.X, next.Y, wall) || visited.Contains(next))
                        continue;
                    if (blocked?.Contains(next) == true && !(allowTargetOccupied && next == target))
                        continue;
                    visited.Add(next);
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
            if (!visited.Contains(target))
                return [];
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
            logger.LogError(exception, "Finding a grid path from {StartX},{StartY} to {TargetX},{TargetY} failed.", startX, startY, targetX, targetY);
            throw;
        }
    }

    /// <inheritdoc />
    public double CastRay(IReadOnlyList<string> map, double x, double y, double angle, char wall, double step, double maximumDistance)
    {
        try
        {
            for (var distance = step; distance < maximumDistance; distance += step)
            {
                var sampleX = (int)(x + Math.Cos(angle) * distance);
                var sampleY = (int)(y + Math.Sin(angle) * distance);
                if (sampleY < 0 || sampleY >= map.Count || sampleX < 0 || sampleX >= map[sampleY].Length)
                    return distance;
                if (map[sampleY][sampleX] == wall)
                    return distance;
            }
            return maximumDistance;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Casting a grid ray failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public void CarveRoom(char[][] cells, int x, int y, int width, int height, char floor)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(cells);
            for (var row = Math.Max(1, y); row < Math.Min(cells.Length - 1, y + height); row++)
                for (var column = Math.Max(1, x); column < Math.Min(cells[row].Length - 1, x + width); column++)
                    cells[row][column] = floor;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Carving a grid room at {X},{Y} failed.", x, y);
            throw;
        }
    }

    /// <inheritdoc />
    public void CarveCorridor(char[][] cells, int fromX, int fromY, int toX, int toY, bool horizontalFirst, char floor)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(cells);
            var x = fromX;
            var y = fromY;
            CarveInterior(cells, x, y, floor);
            if (horizontalFirst)
            {
                while (x != toX) { x += Math.Sign(toX - x); CarveInterior(cells, x, y, floor); }
                while (y != toY) { y += Math.Sign(toY - y); CarveInterior(cells, x, y, floor); }
            }
            else
            {
                while (y != toY) { y += Math.Sign(toY - y); CarveInterior(cells, x, y, floor); }
                while (x != toX) { x += Math.Sign(toX - x); CarveInterior(cells, x, y, floor); }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Carving a grid corridor from {FromX},{FromY} to {ToX},{ToY} failed.", fromX, fromY, toX, toY);
            throw;
        }
    }

    /// <summary>
    /// Carves one cell when it is not on the outer border.
    /// </summary>
    /// <param name="cells">The mutable map cells.</param>
    /// <param name="x">The column.</param>
    /// <param name="y">The row.</param>
    /// <param name="floor">The floor glyph.</param>
    private void CarveInterior(char[][] cells, int x, int y, char floor)
    {
        try
        {
            if (y > 0 && y < cells.Length - 1 && x > 0 && x < cells[y].Length - 1)
                cells[y][x] = floor;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Carving grid cell {X},{Y} failed.", x, y);
            throw;
        }
    }
}
