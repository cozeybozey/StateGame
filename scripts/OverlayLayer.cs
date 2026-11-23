using Godot;
using System;
using System.Collections.Generic;
using System.Threading;

public partial class OverlayLayer : TileMapLayer
{
  [Export] public Color OutlineColor = Colors.Gold;
  [Export] public Color HighlightColor = new Color(0, 0, 0, 0.15f);
  [Export] public float LineWidth = 2f;
  [Export] public bool OutlineCells = true;
  [Export] public bool HighlightCells = true;

  private List<Vector2I> _cells = new();

  public void ShowCells(List<Vector2I> cells)
  {
    _cells = cells;
    QueueRedraw();
  }

  public new void Clear()
  {
    _cells.Clear();
    QueueRedraw();
  }

  public void AddCells(List<Vector2I> cells)
  {
    foreach (Vector2I cell in cells)
    { 
      if (!_cells.Contains(cell))
        _cells.Add(cell);
    }
    QueueRedraw();
  }

  public void RemoveCells(List<Vector2I> cells)
  {
    foreach (Vector2I cell in cells)
    {
      if (_cells.Contains(cell))
        _cells.Remove(cell);
    }
    QueueRedraw();
  }

  public override void _Draw()
  {
    if (_cells.Count == 0)
      return;

    Vector2 tileSize = TileSet.TileSize;

    if (HighlightCells)
    {
      foreach (var cell in _cells)
        DrawRect(new Rect2(MapToLocal(cell) - tileSize / 2, tileSize), HighlightColor);
    }

    if (OutlineCells)
    {
      foreach (var edge in GetOutlineEdges(_cells, cell => MapToLocal(cell), tileSize))
        DrawLine(edge.Item1, edge.Item2, OutlineColor, LineWidth);
    }
  }

  // Edges of the outer border of a group of cells. Each cell adds its four edges; an edge shared by
  // two cells in the group is added once in each direction, so it cancels out and only the outline remains.
  public static IEnumerable<(Vector2, Vector2)> GetOutlineEdges(IEnumerable<Vector2I> cells, Func<Vector2I, Vector2> cellCenter, Vector2 tileSize)
  {
    HashSet<(Vector2, Vector2)> edges = new();
    Vector2 half = tileSize / 2;

    foreach (Vector2I cell in cells)
    {
      Vector2 center = cellCenter(cell);
      Vector2 tl = center + new Vector2(-half.X, -half.Y);
      Vector2 tr = center + new Vector2(half.X, -half.Y);
      Vector2 bl = center + new Vector2(-half.X, half.Y);
      Vector2 br = center + new Vector2(half.X, half.Y);

      AddEdge(edges, tl, tr);
      AddEdge(edges, tr, br);
      AddEdge(edges, br, bl);
      AddEdge(edges, bl, tl);
    }

    return edges;
  }

  private static void AddEdge(HashSet<(Vector2, Vector2)> edges, Vector2 a, Vector2 b)
  {
    if (edges.Contains((b, a)))
      edges.Remove((b, a)); // internal edge
    else
      edges.Add((a, b));
  }
}
