// One reusable A* grid for the current static tree, rock and torch footprints.
// Built once after DEBUG scenery scatters; no physics queries or grid rebuilds every frame.
using Godot;

public sealed class DebugEnemyNavigation
{
    private AStarGrid2D _grid;
    private Rect2 _world;
    private int _cellSize, _columns, _rows, _targetSearch;

    #region Build
    // Rasterize actual world-transformed static collision polygons, padded for an enemy body.
    // =========================================================
    public void Build(Node2D actors, Rect2 worldBounds, EnemyMasterConfig config)
    {
        _world = worldBounds;
        _cellSize = Mathf.Clamp(config.GridCellSize, 12, 48);
        _columns = Mathf.CeilToInt(_world.Size.X / _cellSize);
        _rows = Mathf.CeilToInt(_world.Size.Y / _cellSize);
        _targetSearch = Mathf.Clamp(config.TargetSearchCells, 1, 12);

        _grid = new AStarGrid2D
        {
            Region = new Rect2I(0, 0, _columns, _rows),
            CellSize = new Vector2(_cellSize, _cellSize),
            Offset = _world.Position + Vector2.One * (_cellSize * 0.5f),
            DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles
        };
        _grid.Update();

        // Keep the enemy inside map bounds, with space for its collision footprint.
        for (int x = 0; x < _columns; x++)
        {
            _grid.SetPointSolid(new Vector2I(x, 0));
            _grid.SetPointSolid(new Vector2I(x, _rows - 1));
        }
        for (int y = 0; y < _rows; y++)
        {
            _grid.SetPointSolid(new Vector2I(0, y));
            _grid.SetPointSolid(new Vector2I(_columns - 1, y));
        }

        foreach (Node child in actors.GetChildren())
        {
            if (child is not StaticBody2D body) continue;
            foreach (Node collider in body.GetChildren())
            {
                if (collider is CollisionPolygon2D polygon && !polygon.Disabled)
                {
                    Vector2[] local = polygon.Polygon;
                    var world = new Vector2[local.Length];
                    for (int i = 0; i < local.Length; i++) world[i] = polygon.ToGlobal(local[i]);
                    BlockPolygon(world, config.CollisionClearance);
                }
                else if (collider is CollisionShape2D shape && !shape.Disabled && shape.Shape is RectangleShape2D rectangle)
                {
                    Vector2 half = rectangle.Size * 0.5f;
                    Vector2[] corners = {
                        new(-half.X, -half.Y), new(half.X, -half.Y),
                        new(half.X, half.Y), new(-half.X, half.Y)
                    };
                    for (int i = 0; i < corners.Length; i++) corners[i] = shape.ToGlobal(corners[i]);
                    BlockPolygon(corners, config.CollisionClearance);
                }
            }
        }
    }

    // Only visit cells near this object's collider, not the entire world grid.
    // =========================================================
    private void BlockPolygon(Vector2[] polygon, float clearance)
    {
        if (polygon.Length < 3) return;
        float minX = polygon[0].X, minY = polygon[0].Y, maxX = minX, maxY = minY;
        foreach (Vector2 p in polygon)
        {
            minX = Mathf.Min(minX, p.X); minY = Mathf.Min(minY, p.Y);
            maxX = Mathf.Max(maxX, p.X); maxY = Mathf.Max(maxY, p.Y);
        }
        Vector2I start = ToCell(new Vector2(minX - clearance, minY - clearance));
        Vector2I end = ToCell(new Vector2(maxX + clearance, maxY + clearance));
        float clearanceSquared = clearance * clearance;
        for (int y = start.Y; y <= end.Y; y++)
        for (int x = start.X; x <= end.X; x++)
        {
            Vector2I id = new(x, y);
            Vector2 center = _grid.GetPointPosition(id);
            bool blocked = Geometry2D.IsPointInPolygon(center, polygon);
            if (!blocked)
                for (int i = 0; i < polygon.Length; i++)
                {
                    Vector2 closest = Geometry2D.GetClosestPointToSegment(center, polygon[i], polygon[(i + 1) % polygon.Length]);
                    if (center.DistanceSquaredTo(closest) > clearanceSquared) continue;
                    blocked = true; break;
                }
            if (blocked) _grid.SetPointSolid(id);
        }
    }
    #endregion

    #region Queries
    // Convert global world coordinates to stable, bounded grid IDs.
    // =========================================================
    private Vector2I ToCell(Vector2 position) =>
        new(Mathf.Clamp(Mathf.FloorToInt((position.X - _world.Position.X) / _cellSize), 0, _columns - 1),
            Mathf.Clamp(Mathf.FloorToInt((position.Y - _world.Position.Y) / _cellSize), 0, _rows - 1));

    // Find a nearby free destination if the target is standing beside a collider.
    // =========================================================
    private bool FindFree(Vector2 position, out Vector2I nearest)
    {
        Vector2I original = ToCell(position);
        nearest = original;
        if (!_grid.IsPointSolid(original)) return true;
        float best = float.PositiveInfinity;
        bool found = false;
        for (int dy = -_targetSearch; dy <= _targetSearch; dy++)
        for (int dx = -_targetSearch; dx <= _targetSearch; dx++)
        {
            Vector2I candidate = original + new Vector2I(dx, dy);
            if (!_grid.IsInBoundsv(candidate) || _grid.IsPointSolid(candidate)) continue;
            float cost = _grid.GetPointPosition(candidate).DistanceSquaredTo(position);
            if (cost >= best) continue;
            nearest = candidate; best = cost; found = true;
        }
        return found;
    }

    // Empty result means no viable route: do not fall back to walking through trees.
    // =========================================================
    public Vector2[] FindPath(Vector2 from, Vector2 to)
    {
        if (_grid == null || !FindFree(from, out Vector2I start) || !FindFree(to, out Vector2I end))
            return System.Array.Empty<Vector2>();
        return _grid.GetPointPath(start, end);
    }

    // Make the authored starting point safe even if scenery settings change.
    // =========================================================
    public Vector2 FindSpawnPosition(Vector2 wanted) =>
        _grid != null && FindFree(wanted, out Vector2I id) ? _grid.GetPointPosition(id) : wanted;
    #endregion
}
