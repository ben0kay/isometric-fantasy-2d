// Shared bounded-area navigation rebuilt from active tree/rock collision footprints.
// World population can reuse one grid; agents request paths only when needed.
using Godot;
using System;

public interface IWorldNavigationAgent
{
    void ConfigureNavigation(CharacterBody2D target, StaticObstacleNavigation navigation);
}

public sealed class StaticObstacleNavigation
{
    private AStarGrid2D _grid;
    private Rect2 _bounds;
    private int _cellSize, _columns, _rows, _searchRadius;

    #region Build
    // Rasterize actual world-space polygons AFTER the scenery placement pass.
    // =========================================================
    public void Build(Node2D actors, Rect2 bounds, int cellSize = 24, float clearance = 17f, int searchRadius = 8)
    {
        _bounds = bounds;
        _cellSize = Mathf.Clamp(cellSize, 12, 48);
        _columns = Mathf.Max(3, Mathf.CeilToInt(bounds.Size.X / _cellSize));
        _rows = Mathf.Max(3, Mathf.CeilToInt(bounds.Size.Y / _cellSize));
        _searchRadius = Mathf.Clamp(searchRadius, 1, 16);
        _grid = new AStarGrid2D {
            Region = new Rect2I(0, 0, _columns, _rows),
            CellSize = new Vector2(_cellSize, _cellSize),
            Offset = bounds.Position + Vector2.One * (_cellSize * 0.5f),
            DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles
        };
        _grid.Update();

        for (int x = 0; x < _columns; x++) {
            _grid.SetPointSolid(new Vector2I(x, 0));
            _grid.SetPointSolid(new Vector2I(x, _rows - 1));
        }
        for (int y = 0; y < _rows; y++) {
            _grid.SetPointSolid(new Vector2I(0, y));
            _grid.SetPointSolid(new Vector2I(_columns - 1, y));
        }

        foreach (Node node in actors.FindChildren("*","",true,false))
        {
            if (node is not StaticBody2D body) continue;
            foreach (Node child in body.GetChildren())
            {
                if (child is CollisionPolygon2D polygon && !polygon.Disabled)
                {
                    Vector2[] source = polygon.Polygon;
                    Vector2[] world = new Vector2[source.Length];
                    for (int i = 0; i < source.Length; i++) world[i] = polygon.ToGlobal(source[i]);
                    BlockPolygon(world, clearance);
                }
                else if (child is CollisionShape2D shape && !shape.Disabled && shape.Shape is RectangleShape2D rect)
                {
                    Vector2 h = rect.Size * 0.5f;
                    Vector2[] vertices = { new(-h.X, -h.Y), new(h.X, -h.Y), new(h.X, h.Y), new(-h.X, h.Y) };
                    for (int i = 0; i < vertices.Length; i++) vertices[i] = shape.ToGlobal(vertices[i]);
                    BlockPolygon(vertices, clearance);
                }
            }
        }
    }

    // Include extra clearance around trunks and boulders; process only affected cells.
    // =========================================================
    private void BlockPolygon(Vector2[] polygon, float clearance)
    {
        if (polygon.Length < 3) return;
        float left = polygon[0].X, top = polygon[0].Y, right = left, bottom = top;
        foreach (Vector2 p in polygon)
        {
            left = Mathf.Min(left, p.X); right = Mathf.Max(right, p.X);
            top = Mathf.Min(top, p.Y); bottom = Mathf.Max(bottom, p.Y);
        }
        float c = Mathf.Max(0f, clearance);
        Vector2I start = ToCell(new Vector2(left - c, top - c));
        Vector2I end = ToCell(new Vector2(right + c, bottom + c));
        for (int y = start.Y; y <= end.Y; y++)
        for (int x = start.X; x <= end.X; x++)
        {
            Vector2I id = new(x, y);
            Vector2 center = _grid.GetPointPosition(id);
            bool solid = Geometry2D.IsPointInPolygon(center, polygon);
            if (!solid)
                for (int i = 0; i < polygon.Length; i++)
                {
                    Vector2 nearest = Geometry2D.GetClosestPointToSegment(center, polygon[i], polygon[(i + 1) % polygon.Length]);
                    if (center.DistanceSquaredTo(nearest) > c * c) continue;
                    solid = true; break;
                }
            if (solid) _grid.SetPointSolid(id);
        }
    }
    #endregion

    #region Lookup
    // Map world coordinates to bounded grid cells without relying on active scenes.
    // =========================================================
    private Vector2I ToCell(Vector2 world) =>
        new(Mathf.Clamp(Mathf.FloorToInt((world.X - _bounds.Position.X) / _cellSize), 0, _columns - 1),
            Mathf.Clamp(Mathf.FloorToInt((world.Y - _bounds.Position.Y) / _cellSize), 0, _rows - 1));

    // Find a reachable grid cell near a blocked spawn or destination.
    // =========================================================
    public bool TryGetWalkablePoint(Vector2 wanted, out Vector2 available)
    {
        available = wanted;
        if (_grid == null) return false;
        Vector2I original = ToCell(wanted);
        if (!_grid.IsPointSolid(original)) { available = _grid.GetPointPosition(original); return true; }

        float best = float.PositiveInfinity;
        bool found = false;
        for (int dy = -_searchRadius; dy <= _searchRadius; dy++)
        for (int dx = -_searchRadius; dx <= _searchRadius; dx++)
        {
            Vector2I id = original + new Vector2I(dx, dy);
            if (!_grid.IsInBoundsv(id) || _grid.IsPointSolid(id)) continue;
            Vector2 candidate = _grid.GetPointPosition(id);
            float distance = candidate.DistanceSquaredTo(wanted);
            if (distance >= best) continue;
            available = candidate; best = distance; found = true;
        }
        return found;
    }

    // If no path is available, stop rather than push through an obstacle.
    // =========================================================
    public Vector2[] FindPath(Vector2 from, Vector2 to)
    {
        if (_grid == null || !TryGetWalkablePoint(from, out Vector2 start) ||
            !TryGetWalkablePoint(to, out Vector2 end)) return Array.Empty<Vector2>();
        return _grid.GetPointPath(ToCell(start), ToCell(end));
    }
    #endregion
}
