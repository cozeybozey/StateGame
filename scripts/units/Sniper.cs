using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Sniper : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = new();

    float maxDistanceSq = -1f;
    Vector2I? farthestPos = null;

    for (int x = 0; x < unitsGrid.GetLength(0); x++)
    {
      for (int y = 0; y < unitsGrid.GetLength(1); y++)
      {
        Unit unit = unitsGrid[x, y];

        if (unit == null || unit == this || unit.Side == Side)
          continue;

        Vector2I otherPos = new(x, y);

        int dx = otherPos.X - OccupiedMainCell.X;
        int dy = otherPos.Y - OccupiedMainCell.Y;

        float distanceSq = dx * dx + dy * dy;

        if (distanceSq > maxDistanceSq)
        {
          maxDistanceSq = distanceSq;
          farthestPos = otherPos;
        }
      }
    }

    if (farthestPos.HasValue)
      result.Add(farthestPos.Value);

    return result;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        // Scale damage with distance
        targetUnit.ChangeHealth(-Damage * Mathf.FloorToInt(Mathf.Abs(target.Y - OccupiedMainCell.Y) * 0.5f + Mathf.Abs(target.X - OccupiedMainCell.X) * 0.25f), this);
      }
    }
  }
}
