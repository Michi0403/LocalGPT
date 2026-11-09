namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Grid geometry shared by map, movement, combat, behaviour and view primitives.
/// </summary>
public interface IGameGridService
{
    /// <summary>
    /// Tests whether a cell is inside the map and not a wall.
    /// </summary>
    /// <param name="map">The map rows.</param>
    /// <param name="x">The column.</param>
    /// <param name="y">The row.</param>
    /// <param name="wall">The wall glyph.</param>
    /// <returns><c>true</c> when the cell can be entered.</returns>
    bool IsWalkable(IReadOnlyList<string> map, int x, int y, char wall);

    /// <summary>
    /// Tests whether every intermediate cell on the straight line between two cells is walkable.
    /// </summary>
    /// <param name="map">The map rows.</param>
    /// <param name="fromX">The start column.</param>
    /// <param name="fromY">The start row.</param>
    /// <param name="toX">The target column.</param>
    /// <param name="toY">The target row.</param>
    /// <param name="wall">The wall glyph.</param>
    /// <returns><c>true</c> when the line is unobstructed.</returns>
    bool HasLineOfSight(IReadOnlyList<string> map, int fromX, int fromY, int toX, int toY, char wall);

    /// <summary>
    /// Finds a shortest four-neighbour path with breadth-first search.
    /// </summary>
    /// <param name="map">The map rows.</param>
    /// <param name="startX">The start column.</param>
    /// <param name="startY">The start row.</param>
    /// <param name="targetX">The target column.</param>
    /// <param name="targetY">The target row.</param>
    /// <param name="wall">The wall glyph.</param>
    /// <param name="blocked">Occupied cells that may not be entered.</param>
    /// <param name="allowTargetOccupied">Whether the target cell may be occupied.</param>
    /// <returns>The path including start and target, or an empty list.</returns>
    List<(int X, int Y)> FindShortestPath(IReadOnlyList<string> map, int startX, int startY, int targetX, int targetY, char wall, HashSet<(int X, int Y)>? blocked, bool allowTargetOccupied);

    /// <summary>
    /// Marches a ray until it leaves the map or hits a wall.
    /// </summary>
    /// <param name="map">The map rows.</param>
    /// <param name="x">The ray origin column.</param>
    /// <param name="y">The ray origin row.</param>
    /// <param name="angle">The ray angle in radians.</param>
    /// <param name="wall">The wall glyph.</param>
    /// <param name="step">The march step.</param>
    /// <param name="maximumDistance">The maximum distance.</param>
    /// <returns>The hit distance or <paramref name="maximumDistance"/>.</returns>
    double CastRay(IReadOnlyList<string> map, double x, double y, double angle, char wall, double step, double maximumDistance);

    /// <summary>
    /// Carves a rectangular room, never touching the outer border.
    /// </summary>
    /// <param name="cells">The mutable map cells.</param>
    /// <param name="x">The room column.</param>
    /// <param name="y">The room row.</param>
    /// <param name="width">The room width.</param>
    /// <param name="height">The room height.</param>
    /// <param name="floor">The floor glyph.</param>
    void CarveRoom(char[][] cells, int x, int y, int width, int height, char floor);

    /// <summary>
    /// Carves an L-shaped corridor, never touching the outer border.
    /// </summary>
    /// <param name="cells">The mutable map cells.</param>
    /// <param name="fromX">The start column.</param>
    /// <param name="fromY">The start row.</param>
    /// <param name="toX">The target column.</param>
    /// <param name="toY">The target row.</param>
    /// <param name="horizontalFirst">Whether the horizontal leg is carved first.</param>
    /// <param name="floor">The floor glyph.</param>
    void CarveCorridor(char[][] cells, int fromX, int fromY, int toX, int toY, bool horizontalFirst, char floor);
}
