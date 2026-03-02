using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Sniper : Unit
{
  public override int maxHealth { get; set; } = 8;
  public override int health { get; set; } = 8;
  public override int damage { get; set; } = 1;
  public override int armor { get; set; } = 0;
  public override int startingCooldown { get; set; } = 1;
  public override int cooldown { get; set; } = 1;
  public override int speed { get; set; } = 1;

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid)
  {
    List<Vector2I> result = new();

    float maxDistanceSq = -1f;
    Vector2I? farthestPos = null;

    for (int x = 0; x < unitsGrid.GetLength(0); x++)
    {
      for (int y = 0; y < unitsGrid.GetLength(1); y++)
      {
        Unit unit = unitsGrid[x, y];

        if (unit == null || unit == this || unit.side == side)
          continue;

        Vector2I otherPos = new(x, y);

        int dx = otherPos.X - occupiedMainCell.X;
        int dy = otherPos.Y - occupiedMainCell.Y;

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

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        // Scale damage with distance
        targetUnit.ChangeHealth(-damage * Mathf.FloorToInt(Mathf.Abs(target.Y - occupiedMainCell.Y) * 0.5f + Mathf.Abs(target.X - occupiedMainCell.X) * 0.25f));
      }
    }
  }
}
