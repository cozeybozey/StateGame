using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Booster : Unit
{
  public override int maxHealth { get; set; } = 4;
  public override int health { get; set; } = 4;
  public override int damage { get; set; } = 0;
  public override int armor { get; set; } = 0;
  public override int startingCooldown { get; set; } = 2;
  public override int cooldown { get; set; } = 2;
  public override int speed { get; set; } = 1;

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid)
  {
     return [new Vector2I(occupiedMainCell.X + 1, occupiedMainCell.Y)];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeDamage(targetUnit.damage);
      }
    }
  }
}
