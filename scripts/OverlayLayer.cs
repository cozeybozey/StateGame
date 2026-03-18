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
    _cells.AddRange(cells);
    QueueRedraw();
  }

  public void RemoveCells(List<Vector2I> cells)
  {
    foreach (Vector2I cell in cells)
      _cells.Remove(cell);
    QueueRedraw();
  }

  public override void _Draw()
  {
    if (_cells.Count == 0)
      return;

    HashSet<(Vector2, Vector2)> edges = new();

    Vector2 tileSize = TileSet.TileSize;

    foreach (var cell in _cells)
    {
      Vector2 center = MapToLocal(cell);
      Vector2 half = tileSize / 2;

      if (OutlineCells)
      {
        Vector2 tl = center + new Vector2(-half.X, -half.Y);
        Vector2 tr = center + new Vector2(half.X, -half.Y);
        Vector2 bl = center + new Vector2(-half.X, half.Y);
        Vector2 br = center + new Vector2(half.X, half.Y);

        AddEdge(edges, tl, tr);
        AddEdge(edges, tr, br);
        AddEdge(edges, br, bl);
        AddEdge(edges, bl, tl);
      }

      if (HighlightCells)
      {
        Rect2 rect = new Rect2(center - TileSet.TileSize / 2, TileSet.TileSize);
        DrawRect(rect, HighlightColor);
      }
    }

    if (OutlineCells)
    {
      foreach (var edge in edges)
      {
        DrawLine(edge.Item1, edge.Item2, OutlineColor, LineWidth);
      }
    }
  }

  private void AddEdge(HashSet<(Vector2, Vector2)> edges, Vector2 a, Vector2 b)
  {
    if (edges.Contains((b, a)))
      edges.Remove((b, a)); // internal edge
    else
      edges.Add((a, b));
  }
}
