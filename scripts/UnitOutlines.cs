using Godot;
using System.Collections.Generic;

// Draws one outline in the side's color around every unit during a battle. Outlines are drawn slightly
// inside the unit's cells, so units standing next to each other keep separate outlines.
public partial class UnitOutlines : Node2D
{
  private const float Inset = 1.5f;
  private const float Width = 2f;

  private List<Unit> _units = new();

  public void SetData(List<Unit> units)
  {
    _units = units;
  }

  public override void _Process(double delta)
  {
    if (Visible)
      QueueRedraw();
  }

  public override void _Draw()
  {
    foreach (Unit unit in _units)
    {
      if (!IsInstanceValid(unit) || unit.Health <= 0)
        continue;

      Vector2[] outline = GetOutline(unit);
      if (outline.Length == 0)
        continue;

      DrawSetTransform(unit.VisualOffset);
      DrawPolyline(outline, GamePalette.Side(unit.Side), Width);
    }
    DrawSetTransform(Vector2.Zero);
  }

  // Outline edges run clockwise, so chaining each edge's end to the next edge's start gives the border polygon
  private static Vector2[] GetOutline(Unit unit)
  {
    Vector2 tileSize = new Vector2(GlobalConstants.TileSize, GlobalConstants.TileSize);
    Dictionary<Vector2, Vector2> next = new();
    foreach (var edge in OverlayLayer.GetOutlineEdges(unit.GetOccupiedCells(), CombatFx.CellCenter, tileSize))
      next[edge.Item1] = edge.Item2;
    if (next.Count == 0)
      return [];

    List<Vector2> polygon = new();
    Vector2 start = CombatFx.CellCenter(unit.GetOccupiedCells()[0]);
    foreach (Vector2 point in next.Keys)
    {
      start = point;
      break;
    }
    Vector2 current = start;
    do
    {
      polygon.Add(current);
      current = next[current];
    }
    while (current != start && polygon.Count <= next.Count);

    var inset = Geometry2D.OffsetPolygon(polygon.ToArray(), -Inset);
    if (inset.Count == 0)
      return [];

    List<Vector2> closed = [.. inset[0]];
    closed.Add(closed[0]);
    return closed.ToArray();
  }
}
