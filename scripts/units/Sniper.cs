using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Sniper : Unit
{
  public override int maxHealth { get; set; } = 8;
  public override int health { get; set; } = 8;
  public override int damage { get; set; } = 10;
  public override int armor { get; set; } = 0;
  public override int startingCooldown { get; set; } = 2;
  public override int cooldown { get; set; } = 2;
  public override int speed { get; set; } = 1;

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid)
  {
    List<Vector2I> result = new();

    Vector2I myPos = occupiedMainCell;

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

        int dx = otherPos.X - myPos.X;
        int dy = otherPos.Y - myPos.Y;

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
}
